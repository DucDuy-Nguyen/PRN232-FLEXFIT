using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FlexFit.Identity.Domain.Entities;
using FlexFit.Identity.Infrastructure.Persistence;
using FlexFit.Identity.Infrastructure.Persistence.Repositories;
using FlexFit.Identity.Infrastructure.Security;
using Xunit;

namespace FlexFit.BuildingBlocks.Tests;

public sealed class IdentityPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IdentityDbContext> _options;

    public IdentityPersistenceTests()
    {
        // Setup SQLite connection for in-memory database
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        // Register SQL Server equivalent functions in SQLite to support migrations and defaults
        _connection.CreateFunction("getdate", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        _connection.CreateFunction("newid", () => Guid.NewGuid().ToString());

        _options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Ensure database schema is created
        using var context = new IdentityDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    // Helpers to create context instances
    private IdentityDbContext CreateContext() => new(_options);

    // 1. Password Hash & Verify Tests
    [Fact]
    public void PasswordHasher_ShouldHashAndVerifySuccessfully()
    {
        // Arrange
        var hasher = new Pbkdf2PasswordHasher();
        var password = "SecurePassword123!";

        // Act
        var hash = hasher.Hash(password);
        var verifySuccess = hasher.Verify(password, hash);
        var verifyFail = hasher.Verify("WrongPassword!", hash);

        // Assert
        Assert.NotNull(hash);
        Assert.Contains(".", hash);
        Assert.True(verifySuccess);
        Assert.False(verifyFail);
    }

    [Fact]
    public void PasswordHasher_ShouldVerifyLegacyMonolithHashSuccessfully()
    {
        // Arrange
        var hasher = new Pbkdf2PasswordHasher();
        var salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var rawHash = Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
            "LegacyPass123",
            salt,
            Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA256,
            10000,
            32);
        var customStoredHash = Convert.ToBase64String(salt) + "." + Convert.ToBase64String(rawHash);

        // Act
        var verifySuccess = hasher.Verify("LegacyPass123", customStoredHash);

        // Assert
        Assert.True(verifySuccess);
    }

    // 2. Unique Email Constraint Test
    [Fact]
    public async Task DbContext_ShouldRejectDuplicateEmails()
    {
        // Arrange
        var user1 = User.Create("First User", "duplicate@flexfit.com", "hash", "123");
        var user2 = User.Create("Second User", "duplicate@flexfit.com", "hash", "456");

        using (var context1 = CreateContext())
        {
            await context1.Users.AddAsync(user1);
            await context1.SaveChangesAsync();
        }

        // Act & Assert
        using (var context2 = CreateContext())
        {
            await context2.Users.AddAsync(user2);
            await Assert.ThrowsAsync<DbUpdateException>(async () => await context2.SaveChangesAsync());
        }
    }

    // 3. Unique Role Name Constraint Test
    [Fact]
    public async Task DbContext_ShouldRejectDuplicateRoleNames()
    {
        // Clean seeded roles if any to prevent conflicts
        using (var prepContext = CreateContext())
        {
            prepContext.Roles.RemoveRange(prepContext.Roles);
            await prepContext.SaveChangesAsync();
        }

        // EF Core mapping using reflection since Role name property lacks public setter
        var r1 = (Role)Activator.CreateInstance(typeof(Role), true)!;
        typeof(Role).GetProperty(nameof(Role.RoleName))!.SetValue(r1, "DuplicateRole");
        var r2 = (Role)Activator.CreateInstance(typeof(Role), true)!;
        typeof(Role).GetProperty(nameof(Role.RoleName))!.SetValue(r2, "DuplicateRole");

        using (var context1 = CreateContext())
        {
            await context1.Roles.AddAsync(r1);
            await context1.SaveChangesAsync();
        }

        // Act & Assert
        using (var context2 = CreateContext())
        {
            await context2.Roles.AddAsync(r2);
            await Assert.ThrowsAsync<DbUpdateException>(async () => await context2.SaveChangesAsync());
        }
    }

    // 4. UserRole Composite Key Constraint Test
    [Fact]
    public async Task DbContext_ShouldRejectDuplicateUserRoleAssignment()
    {
        // Arrange
        var user = User.Create("Test User", "test@example.com", "hash", "123");
        Guid roleId;

        using (var context = CreateContext())
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            roleId = context.Roles.First().RoleId;
        }

        var ur1 = UserRole.Create(user.UserId, roleId);
        var ur2 = UserRole.Create(user.UserId, roleId);

        using (var context1 = CreateContext())
        {
            await context1.UserRoles.AddAsync(ur1);
            await context1.SaveChangesAsync();
        }

        // Act & Assert
        using (var context2 = CreateContext())
        {
            await context2.UserRoles.AddAsync(ur2);
            await Assert.ThrowsAsync<DbUpdateException>(async () => await context2.SaveChangesAsync());
        }
    }

    // 5. MemberProfile One-to-One Relation Test
    [Fact]
    public async Task DbContext_ShouldRejectMultipleProfilesForSingleUser()
    {
        // Arrange
        var user = User.Create("Profile User", "profile@example.com", "hash", "123");
        using (var context = CreateContext())
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        var profile1 = MemberProfile.Create(user.UserId);
        var profile2 = MemberProfile.Create(user.UserId);

        using (var context1 = CreateContext())
        {
            await context1.MemberProfiles.AddAsync(profile1);
            await context1.SaveChangesAsync();
        }

        // Act & Assert
        using (var context2 = CreateContext())
        {
            await context2.MemberProfiles.AddAsync(profile2);
            await Assert.ThrowsAsync<DbUpdateException>(async () => await context2.SaveChangesAsync());
        }
    }

    // 6. Repository GetByEmail & GetWithRoles Tests
    [Fact]
    public async Task UserRepository_ShouldFetchUserByEmailWithRoles()
    {
        // Arrange
        var user = User.Create("User Repos", "fetch@example.com", "hash", "123");
        Guid roleId;

        using (var context = CreateContext())
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            roleId = context.Roles.First(r => r.RoleName == "Member").RoleId;
        }

        var ur = UserRole.Create(user.UserId, roleId);
        using (var context = CreateContext())
        {
            await context.UserRoles.AddAsync(ur);
            await context.SaveChangesAsync();
        }

        using (var context = CreateContext())
        {
            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByEmailAsync("FETCH@EXAMPLE.COM");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("User Repos", result.FullName);
            Assert.Single(result.UserRoles);
            Assert.Equal("Member", result.UserRoles.First().Role.RoleName);
        }
    }

    // 7. UnitOfWork & Transaction Rolback Tests
    [Fact]
    public async Task UnitOfWork_ShouldRollbackChangesOnFailure()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var uow = new UnitOfWork(context);
            var repository = new UserRepository(context);

            // Act
            await using (var transaction = await uow.BeginTransactionAsync())
            {
                var user = User.Create("Tx User", "tx@example.com", "hash", "123");
                await repository.AddAsync(user);
                await uow.SaveChangesAsync();

                // Explicit Rollback
                await transaction.RollbackAsync();
            }
        }

        // Assert
        using (var context = CreateContext())
        {
            var repository = new UserRepository(context);
            var exists = await repository.ExistsByEmailAsync("tx@example.com");
            Assert.False(exists);
        }
    }

    // 8. Cross-service Navigation Property Verification (Static/Compile-time proof)
    [Fact]
    public void DomainModels_ShouldNotExposeCrossContextNavigationProperties()
    {
        // Assert: Access the User type properties programmatically to guarantee isolation
        var userProperties = typeof(User).GetProperties().Select(p => p.PropertyType.Name).ToList();

        // List of disallowed cross-service classes in User entity
        var disallowed = new List<string>
        {
            "Gym", "Branch", "Booking", "Payment", "Review", "Notification", "CheckInLog", "Class"
        };

        foreach (var prop in userProperties)
        {
            Assert.False(disallowed.Any(d => prop.Contains(d)), $"User properties should not contain reference to {prop}");
        }
    }
}
