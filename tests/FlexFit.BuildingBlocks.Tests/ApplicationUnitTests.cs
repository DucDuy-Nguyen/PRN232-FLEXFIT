using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Application.Authentication.ChangePassword;
using FlexFit.Identity.Application.Authentication.ForgotPassword;
using FlexFit.Identity.Application.Authentication.Login;
using FlexFit.Identity.Application.Authentication.Logout;
using FlexFit.Identity.Application.Authentication.RefreshToken;
using FlexFit.Identity.Application.Authentication.Register;
using FlexFit.Identity.Application.Authentication.ResendOtp;
using FlexFit.Identity.Application.Authentication.ResetPassword;
using FlexFit.Identity.Application.Authentication.VerifyEmail;
using FlexFit.Identity.Domain.Entities;
using FlexFit.Identity.Domain.Enums;
using Xunit;

namespace FlexFit.BuildingBlocks.Tests;

public sealed class ApplicationUnitTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IMemberProfileRepository _profileRepository = Substitute.For<IMemberProfileRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IOtpCacheService _otpCache = Substitute.For<IOtpCacheService>();
    private readonly IRefreshTokenCacheService _refreshTokenCache = Substitute.For<IRefreshTokenCacheService>();
    private readonly ILoginAttemptService _loginAttempt = Substitute.For<ILoginAttemptService>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IRedisEventPublisher _eventPublisher = Substitute.For<IRedisEventPublisher>();
    private readonly ITokenBlacklistService _blacklistService = Substitute.For<ITokenBlacklistService>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // 1. Register Success Test
    [Fact]
    public async Task Handle_Register_ShouldCreateUserAndSendVerificationEmail()
    {
        // Arrange
        var command = new RegisterCommand("newuser@example.com", "Password123", "Password123", "New User");
        
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("Password123").Returns("hashed-pass");
        
        var memberRole = (Role)Activator.CreateInstance(typeof(Role), true)!;
        typeof(Role).GetProperty(nameof(Role.RoleId))!.SetValue(memberRole, Guid.NewGuid());
        typeof(Role).GetProperty(nameof(Role.RoleName))!.SetValue(memberRole, "Member");
        
        _roleRepository.GetByNameAsync("Member", Arg.Any<CancellationToken>()).Returns(memberRole);
        _otpCache.CreateOtpAsync(Arg.Any<string>(), OtpPurpose.VerifyEmail, Arg.Any<CancellationToken>()).Returns("123456");

        var handler = new RegisterCommandHandler(
            _userRepository, _roleRepository, _profileRepository, _passwordHasher, 
            _otpCache, _emailService, _eventPublisher, _unitOfWork, Substitute.For<ILogger<RegisterCommandHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser@example.com", result.Email);
        
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _profileRepository.Received(1).AddAsync(Arg.Any<MemberProfile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendOtpEmailAsync("newuser@example.com", "New User", "123456", "EmailVerification", Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(RedisStreams.IdentityEvents, Arg.Any<UserRegisteredEvent>());
    }

    // 2. Register Duplicate Email Test
    [Fact]
    public async Task Handle_Register_ShouldThrowException_WhenEmailExists()
    {
        // Arrange
        var command = new RegisterCommand("existing@example.com", "Password123", "Password123", "New User");
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new RegisterCommandHandler(
            _userRepository, _roleRepository, _profileRepository, _passwordHasher, 
            _otpCache, _emailService, _eventPublisher, _unitOfWork, Substitute.For<ILogger<RegisterCommandHandler>>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.Handle(command, CancellationToken.None));
    }

    // 3. Login Success Test
    [Fact]
    public async Task Handle_Login_ShouldReturnTokens_WhenCredentialsCorrect()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "CorrectPassword");
        
        var user = User.Create("John", "user@example.com", "hash", "123");
        user.MarkEmailVerified(); // Email verified check passes
        
        _loginAttempt.IsBlockedAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("CorrectPassword", "hash").Returns(true);
        
        _jwtService.GenerateAccessTokenAsync(user, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new AccessTokenResult("access-token-123", "jti-1", DateTimeOffset.UtcNow.AddHours(1)));
        
        _refreshTokenCache.CreateAsync(user.UserId, Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("refresh-token-123", "rt-1", "family-1", DateTimeOffset.UtcNow.AddDays(1)));

        var handler = new LoginCommandHandler(
            _userRepository, _passwordHasher, _jwtService, _refreshTokenCache, _loginAttempt, Substitute.For<ILogger<LoginCommandHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token-123", result.AccessToken);
        Assert.Equal("refresh-token-123", result.RefreshToken);
        await _loginAttempt.Received(1).ResetAsync("user@example.com", Arg.Any<CancellationToken>());
    }

    // 4. Login Failed Wrong Password Test
    [Fact]
    public async Task Handle_Login_ShouldIncrementFailures_WhenPasswordIncorrect()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "WrongPassword");
        var user = User.Create("John", "user@example.com", "hash", "123");
        user.MarkEmailVerified();

        _loginAttempt.IsBlockedAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("WrongPassword", "hash").Returns(false);

        var handler = new LoginCommandHandler(
            _userRepository, _passwordHasher, _jwtService, _refreshTokenCache, _loginAttempt, Substitute.For<ILogger<LoginCommandHandler>>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.Handle(command, CancellationToken.None));
        await _loginAttempt.Received(1).RecordFailureAsync("user@example.com", Arg.Any<CancellationToken>());
    }

    // 5. Login Lockout Check
    [Fact]
    public async Task Handle_Login_ShouldLockout_WhenBlocked()
    {
        // Arrange
        var command = new LoginCommand("blocked@example.com", "Password");
        _loginAttempt.IsBlockedAsync("blocked@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var handler = new LoginCommandHandler(
            _userRepository, _passwordHasher, _jwtService, _refreshTokenCache, _loginAttempt, Substitute.For<ILogger<LoginCommandHandler>>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.Handle(command, CancellationToken.None));
    }

    // 6. Verify Email Test
    [Fact]
    public async Task Handle_VerifyEmail_ShouldMarkUserVerified_WhenOtpIsValid()
    {
        // Arrange
        var command = new VerifyEmailCommand("user@example.com", "123456");
        var user = User.Create("John", "user@example.com", "hash", null);

        _otpCache.ValidateOtpAsync("user@example.com", OtpPurpose.VerifyEmail, "123456", Arg.Any<CancellationToken>())
            .Returns(OtpValidationResult.Valid);
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var handler = new VerifyEmailHandler(
            _otpCache, _userRepository, _eventPublisher, _unitOfWork, Substitute.For<ILogger<VerifyEmailHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(user.IsEmailVerified);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(RedisStreams.IdentityEvents, Arg.Any<EmailVerifiedEvent>());
    }

    // 7. Forgot Password Test
    [Fact]
    public async Task Handle_ForgotPassword_ShouldGenerateOtp_WhenUserExists()
    {
        // Arrange
        var command = new ForgotPasswordCommand("user@example.com");
        var user = User.Create("John", "user@example.com", "hash", null);

        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _otpCache.IsInCooldownAsync("user@example.com", OtpPurpose.ForgotPassword, Arg.Any<CancellationToken>()).Returns(false);
        _otpCache.CreateOtpAsync("user@example.com", OtpPurpose.ForgotPassword, Arg.Any<CancellationToken>()).Returns("654321");

        var handler = new ForgotPasswordHandler(
            _userRepository, _otpCache, _emailService, Substitute.For<ILogger<ForgotPasswordHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _emailService.Received(1).SendOtpEmailAsync("user@example.com", "John", "654321", "PasswordReset", Arg.Any<CancellationToken>());
    }

    // 8. Reset Password Test
    [Fact]
    public async Task Handle_ResetPassword_ShouldUpdatePasswordAndRevokeSessions()
    {
        // Arrange
        var command = new ResetPasswordCommand("user@example.com", "654321", "NewPassword123", "NewPassword123");
        var user = User.Create("John", "user@example.com", "old-hash", null);

        _otpCache.ValidateOtpAsync("user@example.com", OtpPurpose.ForgotPassword, "654321", Arg.Any<CancellationToken>())
            .Returns(OtpValidationResult.Valid);
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Hash("NewPassword123").Returns("new-hash");

        var handler = new ResetPasswordHandler(
            _userRepository, _passwordHasher, _otpCache, _refreshTokenCache, _unitOfWork, Substitute.For<ILogger<ResetPasswordHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-hash", user.PasswordHash);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokenCache.Received(1).RevokeUserSessionsAsync(user.UserId, Arg.Any<CancellationToken>());
    }

    // 9. Refresh Token Rotation Test
    [Fact]
    public async Task Handle_RefreshToken_ShouldRotateTokensAndBlacklistOldJwt()
    {
        // Arrange
        var command = new RefreshTokenCommand("expired-jwt", "rt-old");
        var user = User.Create("John", "user@example.com", "hash", null);

        _refreshTokenCache.ValidateAsync("rt-old", Arg.Any<CancellationToken>())
            .Returns(new CachedRefreshTokenInfo("rt-1", "family-1", user.UserId, DateTimeOffset.UtcNow.AddHours(1), false));
        _userRepository.GetByIdAsync(user.UserId, Arg.Any<CancellationToken>()).Returns(user);
        
        _refreshTokenCache.RotateAsync("rt-old", Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("rt-new-raw", "rt-2", "family-1", DateTimeOffset.UtcNow.AddDays(1)));
        
        // Mock claims identity
        var claims = new List<Claim> { new Claim(JwtRegisteredClaimNames.Jti, "jti-old"), new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _jwtService.ValidateExpiredToken("expired-jwt").Returns(principal);
        
        _jwtService.GenerateAccessTokenAsync(user, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new AccessTokenResult("new-jwt", "jti-new", DateTimeOffset.UtcNow.AddMinutes(30)));

        var handler = new RefreshTokenCommandHandler(
            _refreshTokenCache, _userRepository, _jwtService, _blacklistService, Substitute.For<ILogger<RefreshTokenCommandHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-jwt", result.AccessToken);
        Assert.Equal("rt-new-raw", result.RefreshToken);
        await _blacklistService.Received(1).BlacklistAsync("jti-old", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    // 10. Logout Test
    [Fact]
    public async Task Handle_Logout_ShouldBlacklistAccessAndRevokeRefreshTokens()
    {
        // Arrange
        var command = new LogoutCommand("current-jwt", "rt-123.secret");
        
        var claims = new List<Claim> { new Claim(JwtRegisteredClaimNames.Jti, "jti-active"), new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _jwtService.ValidateExpiredToken("current-jwt").Returns(principal);

        var handler = new LogoutCommandHandler(
            _refreshTokenCache, _jwtService, _blacklistService, Substitute.For<ILogger<LogoutCommandHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _blacklistService.Received(1).BlacklistAsync("jti-active", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokenCache.Received(1).RevokeAsync("rt-123", Arg.Any<CancellationToken>());
    }

    // 11. Change Password Test
    [Fact]
    public async Task Handle_ChangePassword_ShouldUpdatePasswordHashAndRevokeAllSessions()
    {
        // Arrange
        var user = User.Create("John", "user@example.com", "old-hash", null);
        var command = new ChangePasswordCommand(user.UserId, "OldPassword", "NewPassword", "active-jwt");

        _userRepository.GetByIdAsync(user.UserId, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("OldPassword", "old-hash").Returns(true);
        _passwordHasher.Hash("NewPassword").Returns("new-hash");

        // Mock JWT claims
        var claims = new List<Claim> { new Claim(JwtRegisteredClaimNames.Jti, "jti-active"), new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _jwtService.ValidateExpiredToken("active-jwt").Returns(principal);

        var handler = new ChangePasswordHandler(
            _userRepository, _passwordHasher, _refreshTokenCache, _jwtService, _blacklistService, _unitOfWork, Substitute.For<ILogger<ChangePasswordHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-hash", user.PasswordHash);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokenCache.Received(1).RevokeUserSessionsAsync(user.UserId, Arg.Any<CancellationToken>());
        await _blacklistService.Received(1).BlacklistAsync("jti-active", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
