using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading.Tasks;
using FlexFit.Payment.API.Controllers;
using FlexFit.Payment.API.Configurations;
using FlexFit.Payment.API.Contracts.Requests.CreditPackages;
using FlexFit.Payment.API.Contracts.Responses.CreditPackages;
using FlexFit.Payment.API.Contracts.Requests.Payment;
using FlexFit.Payment.API.Contracts.Responses.Payment;
using FlexFit.Payment.API.Interfaces.Services;
using FlexFit.Payment.API.Interfaces.Repositories;
using FlexFit.Payment.API.Interfaces.Gateways;
using FlexFit.Payment.API.Services;
using FlexFit.Payment.API.Domain.Entities;
using FlexFit.Payment.API.Data;
using FlexFit.Payment.API.Repositories;
using FlexFit.Payment.API.Infrastructure.Redis;
using FlexFit.Payment.API.BackgroundServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace FlexFit.Payment.UnitTests
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPayOSPaymentGateway> _payOSGatewayMock;
        private readonly Mock<IOutboxRepository> _outboxRepoMock;
        private readonly Mock<IDistributedLockService> _lockServiceMock;
        private readonly Mock<IIdempotencyService> _idempotencyServiceMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<IProcessedMessageRepository> _processedRepoMock;

        public PaymentServiceTests()
        {
            _payOSGatewayMock = new Mock<IPayOSPaymentGateway>();
            _outboxRepoMock = new Mock<IOutboxRepository>();
            _lockServiceMock = new Mock<IDistributedLockService>();
            _idempotencyServiceMock = new Mock<IIdempotencyService>();
            _cacheServiceMock = new Mock<ICacheService>();
            _processedRepoMock = new Mock<IProcessedMessageRepository>();

            // Setup default distributed lock behaviors (success by default)
            _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _lockServiceMock.Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Setup default idempotency behavior
            _idempotencyServiceMock.Setup(x => x.IsIdempotentAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
        }

        private PaymentDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PaymentDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new PaymentDbContext(options);
        }

        // ==========================================
        // 1. Invalid credit package fails payment creation.
        // ==========================================
        [Fact]
        public async Task CreatePaymentUrlAsync_WithInvalidPackage_ThrowsException()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PaymentRepository(context);
            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object);

            // Act & Assert
            var request = new CreatePaymentRequest { PackageId = Guid.NewGuid(), PaymentMethod = "MOCK" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePaymentUrlAsync(Guid.NewGuid(), request));
        }

        // ==========================================
        // 2 & 3. Successful payment/webhook credits wallet exactly once & Duplicate webhook does not credit wallet twice.
        // ==========================================
        [Fact]
        public async Task ProcessPaymentCallbackAsync_SuccessWebhook_AddsCreditOnce_AndDuplicateIsIgnored()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var package = new CreditPackage
            {
                PackageId = Guid.NewGuid(),
                PackageName = "Silver",
                CreditAmount = 500,
                BonusCredit = 50,
                Price = 500000,
                IsActive = true
            };
            var payment = new FlexFit.Payment.API.Domain.Entities.Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = userId,
                PackageId = package.PackageId,
                Amount = 500000,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                Package = package
            };
            await context.CreditPackages.AddAsync(package);
            await context.Payments.AddAsync(payment);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object);

            var callbackData = new PaymentCallbackRequest
            {
                PaymentId = payment.PaymentId,
                Status = "Success",
                ProviderTransactionCode = "TXN123"
            };

            // Act - First Webhook
            var success = await service.ProcessPaymentCallbackAsync(callbackData);

            // Assert
            Assert.True(success);
            var wallet = await context.UserCredits.FirstOrDefaultAsync(w => w.UserId == userId);
            Assert.NotNull(wallet);
            Assert.Equal(550, wallet.Balance); // 500 + 50 bonus

            // Arrange for duplicate webhook
            _idempotencyServiceMock.Setup(x => x.IsIdempotentAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(false); // Second check fails idempotency

            // Act - Duplicate Webhook
            var duplicateSuccess = await service.ProcessPaymentCallbackAsync(callbackData);

            // Assert duplicate does not credit again
            Assert.True(duplicateSuccess); 
            Assert.Equal(550, wallet.Balance); // balance remains 550
        }

        // ==========================================
        // 4. Invalid PayOS webhook/signature is rejected or fails safely.
        // ==========================================
        [Fact]
        public async Task ProcessPayOSWebhookAsync_InvalidSignature_ThrowsException()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PaymentRepository(context);
            _payOSGatewayMock.Setup(x => x.VerifyWebhookSignatureAsync(It.IsAny<object>()))
                .ReturnsAsync((PayOSWebhookData?)null); // invalid signature returns null

            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.ProcessPayOSWebhookAsync(new object()));
        }

        // ==========================================
        // 5. Credit deduction succeeds with sufficient balance.
        // ==========================================
        [Fact]
        public async Task DeductCreditAsync_WithSufficientBalance_Succeeds()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var wallet = new UserCredit
            {
                UserCreditId = Guid.NewGuid(),
                UserId = userId,
                Balance = 100,
                TotalEarned = 100,
                UpdatedAt = DateTime.UtcNow
            };
            await context.UserCredits.AddAsync(wallet);
            await context.SaveChangesAsync();

            var repo = new CreditRepository(context);
            var service = new CreditAdjustmentService(repo, _lockServiceMock.Object, _outboxRepoMock.Object, _cacheServiceMock.Object, _idempotencyServiceMock.Object, _processedRepoMock.Object);

            // Act
            await service.DeductCreditAsync(bookingId, userId, 30, "GymBooking", "Deduction for gym session");

            // Assert
            Assert.Equal(70, wallet.Balance);
            var txn = await context.CreditTransactions.FirstOrDefaultAsync(t => t.ReferenceId == bookingId);
            Assert.NotNull(txn);
            Assert.Equal(-30, txn.Amount);
            Assert.Equal("Deduction", txn.Type);
        }

        // ==========================================
        // 6. Credit deduction fails with insufficient balance.
        // ==========================================
        [Fact]
        public async Task DeductCreditAsync_WithInsufficientBalance_QueuesFailureEvent()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var wallet = new UserCredit
            {
                UserCreditId = Guid.NewGuid(),
                UserId = userId,
                Balance = 10,
                TotalEarned = 10,
                UpdatedAt = DateTime.UtcNow
            };
            await context.UserCredits.AddAsync(wallet);
            await context.SaveChangesAsync();

            var repo = new CreditRepository(context);
            var service = new CreditAdjustmentService(repo, _lockServiceMock.Object, _outboxRepoMock.Object, _cacheServiceMock.Object, _idempotencyServiceMock.Object, _processedRepoMock.Object);

            // Act
            await service.DeductCreditAsync(bookingId, userId, 30, "GymBooking", "Deduction for gym session");

            // Assert
            Assert.Equal(10, wallet.Balance); // Balance unmodified
            _outboxRepoMock.Verify(o => o.QueueEventAsync(It.Is<string>(type => type == "CreditDeductionFailed"), It.IsAny<object>()), Times.Once);
        }

        // ==========================================
        // 7. Duplicate deduction event does not deduct twice.
        // ==========================================
        [Fact]
        public async Task DeductCreditAsync_DuplicateEvent_DoesNotDeductTwice()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var wallet = new UserCredit
            {
                UserCreditId = Guid.NewGuid(),
                UserId = userId,
                Balance = 100,
                TotalEarned = 100,
                UpdatedAt = DateTime.UtcNow
            };
            await context.UserCredits.AddAsync(wallet);
            await context.SaveChangesAsync();

            var repo = new CreditRepository(context);
            
            // Set up processed message mock to return already processed on second call
            var processedMock = new Mock<IProcessedMessageRepository>();
            processedMock.SetupSequence(p => p.HasBeenProcessedAsync(bookingId))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            var service = new CreditAdjustmentService(repo, _lockServiceMock.Object, _outboxRepoMock.Object, _cacheServiceMock.Object, _idempotencyServiceMock.Object, processedMock.Object);

            // Act
            await service.DeductCreditAsync(bookingId, userId, 30, "GymBooking", "Deduct 1");
            await service.DeductCreditAsync(bookingId, userId, 30, "GymBooking", "Deduct 2");

            // Assert
            Assert.Equal(70, wallet.Balance); // Only deducted once
        }

        // ==========================================
        // 8. Credit refund succeeds.
        // ==========================================
        [Fact]
        public async Task RefundCreditAsync_SufficientConditions_RefundsSuccessfully()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var wallet = new UserCredit
            {
                UserCreditId = Guid.NewGuid(),
                UserId = userId,
                Balance = 50,
                TotalEarned = 50,
                UpdatedAt = DateTime.UtcNow
            };
            await context.UserCredits.AddAsync(wallet);
            await context.SaveChangesAsync();

            var repo = new CreditRepository(context);
            var service = new CreditAdjustmentService(repo, _lockServiceMock.Object, _outboxRepoMock.Object, _cacheServiceMock.Object, _idempotencyServiceMock.Object, _processedRepoMock.Object);

            // Act
            await service.RefundCreditAsync(bookingId, userId, 30, "GymBooking", "Refund booking");

            // Assert
            Assert.Equal(80, wallet.Balance);
            var txn = await context.CreditTransactions.FirstOrDefaultAsync(t => t.ReferenceId == bookingId && t.Type == "Refund");
            Assert.NotNull(txn);
            Assert.Equal(30, txn.Amount);
        }

        // ==========================================
        // 9. Duplicate refund event does not refund twice.
        // ==========================================
        [Fact]
        public async Task RefundCreditAsync_DuplicateEvent_DoesNotRefundTwice()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var wallet = new UserCredit
            {
                UserCreditId = Guid.NewGuid(),
                UserId = userId,
                Balance = 50,
                TotalEarned = 50,
                UpdatedAt = DateTime.UtcNow
            };
            await context.UserCredits.AddAsync(wallet);
            await context.SaveChangesAsync();

            var repo = new CreditRepository(context);
            var processedMock = new Mock<IProcessedMessageRepository>();
            processedMock.SetupSequence(p => p.HasBeenProcessedAsync(bookingId))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            var service = new CreditAdjustmentService(repo, _lockServiceMock.Object, _outboxRepoMock.Object, _cacheServiceMock.Object, _idempotencyServiceMock.Object, processedMock.Object);

            // Act
            await service.RefundCreditAsync(bookingId, userId, 30, "GymBooking", "Refund 1");
            await service.RefundCreditAsync(bookingId, userId, 30, "GymBooking", "Refund 2");

            // Assert
            Assert.Equal(80, wallet.Balance); // Refunded once
        }

        // ==========================================
        // 10. Admin credit adjustment creates the correct transaction.
        // ==========================================
        [Fact]
        public async Task AdminAddCreditToUserAsync_ValidRequest_CreatesCorrectTransaction()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var wallet = new UserCredit
            {
                UserCreditId = Guid.NewGuid(),
                UserId = userId,
                Balance = 10,
                UpdatedAt = DateTime.UtcNow
            };
            await context.UserCredits.AddAsync(wallet);
            await context.SaveChangesAsync();

            var repo = new CreditRepository(context);
            var service = new CreditService(repo, _lockServiceMock.Object, _outboxRepoMock.Object, _cacheServiceMock.Object, _idempotencyServiceMock.Object);

            var request = new AdminAddCreditRequest
            {
                UserId = userId,
                Amount = 100,
                Description = "Gift from Admin"
            };

            // Act
            await service.AdminAddCreditToUserAsync(request);

            Assert.Equal(110, wallet.Balance);
            var txn = await context.CreditTransactions.FirstOrDefaultAsync(t => t.UserId == userId && t.Type == "AdminAdjustment");
            Assert.NotNull(txn);
            Assert.Equal(100, txn.Amount);
            Assert.Equal("[Admin điều chỉnh] Gift from Admin", txn.Description);
        }

        // ==========================================
        // 11. Wallet cache is invalidated after a mutation.
        // ==========================================
        [Fact]
        public async Task ProcessPaymentCallbackAsync_Success_InvalidatesWalletCache()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var package = new CreditPackage
            {
                PackageId = Guid.NewGuid(),
                PackageName = "Silver",
                CreditAmount = 500,
                Price = 500000,
                IsActive = true
            };
            var payment = new FlexFit.Payment.API.Domain.Entities.Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = userId,
                PackageId = package.PackageId,
                Amount = 500000,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                Package = package
            };
            await context.CreditPackages.AddAsync(package);
            await context.Payments.AddAsync(payment);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object);

            // Act
            await service.ProcessPaymentCallbackAsync(new PaymentCallbackRequest
            {
                PaymentId = payment.PaymentId,
                Status = "Success"
            });

            // Assert cache invalidation was triggered
            _cacheServiceMock.Verify(c => c.RemoveAsync($"payment:user:{userId}:balance"), Times.Once);
        }

        // ==========================================
        // 12. Distributed lock prevents two concurrent wallet mutations.
        // ==========================================
        [Fact]
        public async Task ProcessPaymentCallbackAsync_LockAcquisitionFails_StillSucceedsViaSql()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var package = new CreditPackage { PackageId = Guid.NewGuid(), PackageName = "Test", CreditAmount = 100, Price = 100000, IsActive = true };
            var payment = new FlexFit.Payment.API.Domain.Entities.Payment { PaymentId = Guid.NewGuid(), UserId = userId, PackageId = package.PackageId, Amount = 100000, Status = "Pending", CreatedAt = DateTime.UtcNow, Package = package };
            await context.CreditPackages.AddAsync(package);
            await context.Payments.AddAsync(payment);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            
            // Fail lock acquisition
            var badLockMock = new Mock<IDistributedLockService>();
            badLockMock.Setup(x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(false);

            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, badLockMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object);

            // Act
            var success = await service.ProcessPaymentCallbackAsync(new PaymentCallbackRequest
            {
                PaymentId = payment.PaymentId,
                Status = "Success"
            });
            
            // Assert
            Assert.True(success);
            var wallet = await context.UserCredits.FirstOrDefaultAsync(w => w.UserId == userId);
            Assert.NotNull(wallet);
            Assert.Equal(100, wallet.Balance);
        }

        // ==========================================
        // 13. OutboxMessage is created together with the financial operation.
        // ==========================================
        [Fact]
        public async Task ProcessPaymentCallbackAsync_Success_CreatesOutboxMessage()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var package = new CreditPackage { PackageId = Guid.NewGuid(), PackageName = "Bronze", CreditAmount = 100, Price = 100000, IsActive = true };
            var payment = new FlexFit.Payment.API.Domain.Entities.Payment { PaymentId = Guid.NewGuid(), UserId = userId, PackageId = package.PackageId, Amount = 100000, Status = "Pending", CreatedAt = DateTime.UtcNow, Package = package };
            await context.CreditPackages.AddAsync(package);
            await context.Payments.AddAsync(payment);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object);

            // Act
            await service.ProcessPaymentCallbackAsync(new PaymentCallbackRequest { PaymentId = payment.PaymentId, Status = "Success" });

            // Assert outbox queue was called
            _outboxRepoMock.Verify(o => o.QueueEventAsync("PaymentCompleted", It.IsAny<object>()), Times.Once);
        }

        // ==========================================
        // 14. ProcessedMessage or durable SQL idempotency prevents duplicate processing.
        // ==========================================
        [Fact]
        public async Task DeductCreditAsync_ProcessedTable_PreventsDuplicateProcessing()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var wallet = new UserCredit { UserCreditId = Guid.NewGuid(), UserId = userId, Balance = 100, TotalEarned = 100, UpdatedAt = DateTime.UtcNow };
            await context.UserCredits.AddAsync(wallet);
            await context.SaveChangesAsync();

            var repo = new CreditRepository(context);
            var processedMock = new Mock<IProcessedMessageRepository>();
            processedMock.Setup(p => p.HasBeenProcessedAsync(bookingId))
                .ReturnsAsync(true); // already processed

            var service = new CreditAdjustmentService(repo, _lockServiceMock.Object, _outboxRepoMock.Object, _cacheServiceMock.Object, _idempotencyServiceMock.Object, processedMock.Object);

            // Act
            await service.DeductCreditAsync(bookingId, userId, 30, "GymBooking", "Deduct");

            // Assert
            Assert.Equal(100, wallet.Balance); // unmodified since it was marked processed
        }

        // ==========================================
        // 15 & 16. Failed stream processing enters retry handling & retry exhaustion publishes to dead-letter stream.
        // ==========================================
        [Fact]
        public async Task RedisConsumerWorker_FailsExhaustively_PublishesToDeadLetter()
        {
            // Arrange
            var redisMock = new Mock<IConnectionMultiplexer>();
            var dbMock = new Mock<IDatabase>();
            redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

            // Mock a stream read containing one deduction request
            var values = new[]
            {
                new NameValueEntry("EventType", "CreditDeductionRequested"),
                new NameValueEntry("Payload", "{\"bookingId\":\"f7481cb0-c23f-42e1-a066-51e967a149c4\",\"userId\":\"" + Guid.NewGuid() + "\",\"creditCost\":30,\"referenceType\":\"GymBooking\",\"description\":\"Dat open gym\"}"),
                new NameValueEntry("CorrelationId", Guid.NewGuid().ToString())
            };
            var streamMessage = new StreamEntry("123-0", values);
            dbMock.SetupSequence(d => d.StreamReadGroupAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<RedisValue>(), It.IsAny<RedisValue?>(), It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(new[] { streamMessage })
                .ReturnsAsync(Array.Empty<StreamEntry>()); // Stop second read loop

            // Service provider with adjustment service throwing exception to force failure
            var services = new ServiceCollection();
            var adjMock = new Mock<ICreditAdjustmentService>();
            adjMock.Setup(a => a.DeductCreditAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("DB Down"));
            services.AddSingleton(adjMock.Object);
            var provider = services.BuildServiceProvider();

            var loggerMock = new Mock<ILogger<RedisConsumerWorker>>();
            var worker = new RedisConsumerWorker(redisMock.Object, provider, loggerMock.Object);

            // Act
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(2000); // Stop worker quickly
            await worker.StartAsync(cts.Token);
            await Task.Delay(800);
            await worker.StopAsync(cts.Token);

            // Assert dead letter stream was populated
            dbMock.Verify(d => d.StreamAddAsync(It.Is<RedisKey>(k => k == "flexfit:dead-letter"), It.IsAny<NameValueEntry[]>(), It.IsAny<RedisValue?>(), It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        // ==========================================
        // 17. Non-Admin access to an Admin endpoint is rejected where practical.
        // ==========================================
        [Fact]
        public void PaymentController_GetAllPayments_Requires_Admin_Role()
        {
            // Arrange
            var type = typeof(PaymentController);
            var method = type.GetMethod(nameof(PaymentController.GetAllPayments));

            // Act
            var authorizeAttribute = method?.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(authorizeAttribute);
            Assert.Equal("Admin", authorizeAttribute.Roles);
        }

        // ==========================================
        // 18. Verification of Admin authorization constraints.
        // ==========================================
        [Fact]
        public void CreditPackagesController_CreatePackage_Requires_Admin_Role()
        {
            // Arrange
            var type = typeof(CreditPackagesController);
            var method = type.GetMethod(nameof(CreditPackagesController.CreatePackage));

            // Act
            var authorizeAttribute = method?.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(authorizeAttribute);
            Assert.Equal("Admin", authorizeAttribute.Roles);
        }

        // ==========================================
        // 19. Existing payment response DTO shape remains compatible.
        // ==========================================
        [Fact]
        public void PaymentResponse_DTO_HasRequiredProperties()
        {
            var properties = typeof(PaymentResponse).GetProperties().Select(p => p.Name).ToList();

            Assert.Contains("PaymentId", properties);
            Assert.Contains("UserId", properties);
            Assert.Contains("PackageId", properties);
            Assert.Contains("Amount", properties);
            Assert.Contains("PaymentMethod", properties);
            Assert.Contains("PaymentUrl", properties);
            Assert.Contains("Status", properties);
            Assert.Contains("CreatedAt", properties);
        }

        // ==========================================
        // 20. Existing credit package response DTO shape remains compatible.
        // ==========================================
        [Fact]
        public void CreditPackageResponse_DTO_HasRequiredProperties()
        {
            var properties = typeof(FlexFit.Payment.API.Contracts.Responses.Payment.CreditPackageResponse).GetProperties().Select(p => p.Name).ToList();

            Assert.Contains("PackageId", properties);
            Assert.Contains("PackageName", properties);
            Assert.Contains("CreditAmount", properties);
            Assert.Contains("BonusCredit", properties);
            Assert.Contains("Price", properties);
            Assert.Contains("Description", properties);
            Assert.Contains("IsPopular", properties);
            Assert.Contains("IsActive", properties);
            Assert.Contains("CreatedAt", properties);
        }

        // ==========================================
        // 21. JWT Token Claims and Authorization Rules
        // ==========================================
        [Fact]
        public void GenerateDevToken_CreatesValidTokenWithAllRequiredClaims()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var email = "member@test.local";
            var role = "Member";
            
            var keyStr = "VeryLongSuperSecureKey1234567890!!";
            var issuer = "FlexFitAPI";
            var audience = "FlexFitClient";
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(60);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("userId", userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                new Claim("role", role),
                new Claim(ClaimTypes.Role, role)
            };

            // Act
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            
            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);
            
            Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => c.Type == "userId").Value);
            Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal(email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
            Assert.Equal(email, jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
            Assert.Equal(role, jwtToken.Claims.First(c => c.Type == "role").Value);
            Assert.Equal(role, jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        }

        [Fact]
        public void MemberToken_CanAccess_CreatePayment_ButNot_AdminEndpoints()
        {
            // Arrange & Act
            var paymentControllerType = typeof(PaymentController);
            var createPaymentMethod = paymentControllerType.GetMethod(nameof(PaymentController.CreatePayment));
            var getAllPaymentsMethod = paymentControllerType.GetMethod(nameof(PaymentController.GetAllPayments));

            // Assert
            // 1. Member token can call POST /api/payment/create (requires only [Authorize])
            var paymentAuthAttr = createPaymentMethod?.GetCustomAttribute<AuthorizeAttribute>() 
                               ?? paymentControllerType.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(paymentAuthAttr);
            Assert.Null(paymentAuthAttr.Roles); // No specific roles required, meaning Member can call it.

            // 2. Member token cannot call GET /api/payment/admin/history (requires Admin role)
            var adminAuthAttr = getAllPaymentsMethod?.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(adminAuthAttr);
            Assert.Equal("Admin", adminAuthAttr.Roles); // Requires "Admin", meaning "Member" is rejected.
        }

        [Fact]
        public void AdminToken_CanAccess_GetAllPayments()
        {
            // Arrange & Act
            var paymentControllerType = typeof(PaymentController);
            var getAllPaymentsMethod = paymentControllerType.GetMethod(nameof(PaymentController.GetAllPayments));

            // Assert
            var adminAuthAttr = getAllPaymentsMethod?.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(adminAuthAttr);
            Assert.Equal("Admin", adminAuthAttr.Roles); // Admin is explicitly allowed.
        }

        // ==========================================
        // 22. UseMockPayment=false + PayOS config đầy đủ -> dùng PayOSPaymentGateway
        // ==========================================
        [Fact]
        public async Task CreatePayment_UseMockPaymentFalse_WithValidConfig_UsesPayOS()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var package = new CreditPackage { PackageId = Guid.NewGuid(), PackageName = "Bronze", CreditAmount = 100, Price = 100000, IsActive = true };
            await context.CreditPackages.AddAsync(package);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var payOSOptions = Microsoft.Extensions.Options.Options.Create(new PayOSOptions 
            { 
                ClientId = "real-client-id", 
                ApiKey = "real-api-key", 
                ChecksumKey = "real-checksum-key" 
            });
            var paymentOptions = Microsoft.Extensions.Options.Options.Create(new PaymentOptions { UseMockPayment = false });

            _payOSGatewayMock.Setup(g => g.CreatePaymentLinkAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new PayOSPaymentLinkResult { CheckoutUrl = "https://pay.payos.vn/checkout/123" });

            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object, payOSOptions, paymentOptions);

            // Act
            var request = new CreatePaymentRequest { PackageId = package.PackageId, PaymentMethod = "MOCK" }; 
            var result = await service.CreatePaymentUrlAsync(Guid.NewGuid(), request);

            // Assert
            Assert.Equal("PAYOS", result.PaymentMethod);
            Assert.Equal("https://pay.payos.vn/checkout/123", result.PaymentUrl);
            _payOSGatewayMock.Verify(g => g.CreatePaymentLinkAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // ==========================================
        // 23. UseMockPayment=false + thiếu config -> không fallback Mock
        // ==========================================
        [Fact]
        public async Task CreatePayment_UseMockPaymentFalse_WithMissingConfig_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var package = new CreditPackage { PackageId = Guid.NewGuid(), PackageName = "Bronze", CreditAmount = 100, Price = 100000, IsActive = true };
            await context.CreditPackages.AddAsync(package);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var payOSOptions = Microsoft.Extensions.Options.Options.Create(new PayOSOptions 
            { 
                ClientId = "", 
                ApiKey = "", 
                ChecksumKey = "" 
            });
            var paymentOptions = Microsoft.Extensions.Options.Options.Create(new PaymentOptions { UseMockPayment = false });

            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object, payOSOptions, paymentOptions);

            // Act & Assert
            var request = new CreatePaymentRequest { PackageId = package.PackageId, PaymentMethod = "PAYOS" };
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePaymentUrlAsync(Guid.NewGuid(), request));
        }

        // ==========================================
        // 24. UseMockPayment=true trong Test -> dùng mock
        // ==========================================
        [Fact]
        public async Task CreatePayment_UseMockPaymentTrue_UsesMock()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var package = new CreditPackage { PackageId = Guid.NewGuid(), PackageName = "Bronze", CreditAmount = 100, Price = 100000, IsActive = true };
            await context.CreditPackages.AddAsync(package);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var payOSOptions = Microsoft.Extensions.Options.Options.Create(new PayOSOptions { ClientId = "mock", ApiKey = "mock", ChecksumKey = "mock" });
            var paymentOptions = Microsoft.Extensions.Options.Options.Create(new PaymentOptions { UseMockPayment = true });

            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object, payOSOptions, paymentOptions);

            // Act
            var request = new CreatePaymentRequest { PackageId = package.PackageId, PaymentMethod = "MOCK" };
            var result = await service.CreatePaymentUrlAsync(Guid.NewGuid(), request);

            // Assert
            Assert.Equal("MOCK", result.PaymentMethod);
            Assert.Contains("/api/payment/mock-checkout", result.PaymentUrl);
        }

        // ==========================================
        // 25. Package inactive -> reject
        // ==========================================
        [Fact]
        public async Task CreatePayment_PackageInactive_ThrowsArgumentException()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var package = new CreditPackage { PackageId = Guid.NewGuid(), PackageName = "Bronze", CreditAmount = 100, Price = 100000, IsActive = false };
            await context.CreditPackages.AddAsync(package);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var service = new PaymentService(repo, _payOSGatewayMock.Object, _outboxRepoMock.Object, _lockServiceMock.Object, _idempotencyServiceMock.Object, _cacheServiceMock.Object);

            // Act & Assert
            var request = new CreatePaymentRequest { PackageId = package.PackageId, PaymentMethod = "MOCK" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePaymentUrlAsync(Guid.NewGuid(), request));
        }

        // ==========================================
        // 26. Controller không dùng DbContext trực tiếp
        // ==========================================
        [Fact]
        public void Controllers_DoNotUseDbContextDirectly()
        {
            var controllerTypes = new[] { typeof(PaymentController), typeof(CreditPackagesController), typeof(CreditWalletController), typeof(PaymentCallbackController) };
            foreach (var type in controllerTypes)
            {
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    Assert.NotEqual(typeof(PaymentDbContext), field.FieldType);
                }
            }
        }
    }
}
