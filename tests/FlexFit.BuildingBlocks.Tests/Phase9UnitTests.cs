using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Application.Authentication.GoogleLogin;
using FlexFit.Identity.Application.Users.AssignRole;
using FlexFit.Identity.Application.Users.RevokeRole;
using FlexFit.Identity.Application.Users.ChangeUserStatus;
using FlexFit.Identity.Application.Users.Queries;
using FlexFit.Identity.Domain.Entities;
using FlexFit.Identity.Domain.Enums;
using Xunit;

namespace FlexFit.BuildingBlocks.Tests;

public sealed class Phase9UnitTests
{
    // ─── Shared mocks ────────────────────────────────────────────────────────
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IMemberProfileRepository _profileRepo = Substitute.For<IMemberProfileRepository>();
    private readonly IRefreshTokenCacheService _refreshTokenCache = Substitute.For<IRefreshTokenCacheService>();
    private readonly ITokenBlacklistService _blacklist = Substitute.For<ITokenBlacklistService>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IRedisEventPublisher _events = Substitute.For<IRedisEventPublisher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IGoogleTokenValidator _googleValidator = Substitute.For<IGoogleTokenValidator>();

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static User MakeActiveUser(bool emailVerified = true)
    {
        var user = User.Create("Test User", "test@example.com", "hash", null);
        if (emailVerified)
        {
            // Use reflection to avoid bypassing domain validation
            typeof(User).GetProperty("IsEmailVerified")!
                .SetValue(user, true);
        }
        return user;
    }

    private static Role MakeRole(string name)
    {
        // Role has no public factory — use reflection to bypass private constructor for test setup
        var role = (Role)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Role));
        typeof(Role).GetProperty("RoleId")!.SetValue(role, Guid.NewGuid());
        typeof(Role).GetProperty("RoleName")!.SetValue(role, name);
        typeof(Role).GetProperty("Description")!.SetValue(role, $"{name} role");
        typeof(Role).GetProperty("CreatedAt")!.SetValue(role, DateTime.UtcNow);
        return role;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GOOGLE LOGIN TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GoogleLogin_InvalidToken_ThrowsUnauthorized()
    {
        _googleValidator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GoogleUserInfo?)null);

        var handler = MakeGoogleHandler();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new GoogleLoginCommand("bad-token"), CancellationToken.None));
    }

    [Fact]
    public async Task GoogleLogin_EmailNotVerified_ThrowsUnauthorized()
    {
        _googleValidator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("sub123", "user@gmail.com", "User", null, EmailVerified: false));

        var handler = MakeGoogleHandler();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None));
    }

    [Fact]
    public async Task GoogleLogin_ExistingInactiveUser_ThrowsUnauthorized()
    {
        var googleInfo = new GoogleUserInfo("sub123", "test@example.com", "Test", null, true);
        _googleValidator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(googleInfo);

        var user = User.Create("Test", "test@example.com", "hash", null);
        // User is inactive by default (IsActive=true by domain), so deactivate it
        user.SetActive(false);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var handler = MakeGoogleHandler();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None));
    }

    [Fact]
    public async Task GoogleLogin_ExistingActiveUser_ReturnsTokens()
    {
        var googleInfo = new GoogleUserInfo("sub123", "test@example.com", "Test", null, true);
        _googleValidator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(googleInfo);

        var user = User.Create("Test", "test@example.com", "hash", null);
        var role = MakeRole("Member");
        var userRole = UserRole.Create(user.UserId, role.RoleId);
        typeof(UserRole).GetProperty("Role")!.SetValue(userRole, role);
        user.AddUserRole(userRole);

        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateAccessTokenAsync(Arg.Any<User>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new AccessTokenResult("access-token", "jti-1", DateTimeOffset.UtcNow.AddHours(1)));
        _refreshTokenCache.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("raw-refresh", "tok-id", "fam-id", DateTimeOffset.UtcNow.AddDays(7)));

        var handler = MakeGoogleHandler();
        var result = await handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("raw-refresh", result.RefreshToken);
        Assert.False(result.IsNewUser);
    }

    [Fact]
    public async Task GoogleLogin_NewUser_CreatesUserAndPublishesEvents()
    {
        var googleInfo = new GoogleUserInfo("sub456", "new@gmail.com", "New User", null, true);
        _googleValidator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(googleInfo);

        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var role = MakeRole("Member");
        _roleRepo.GetByNameAsync("Member", Arg.Any<CancellationToken>()).Returns(role);

        _jwtService.GenerateAccessTokenAsync(Arg.Any<User>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new AccessTokenResult("access-token", "jti-2", DateTimeOffset.UtcNow.AddHours(1)));
        _refreshTokenCache.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("raw-refresh", "tok-id", "fam-id", DateTimeOffset.UtcNow.AddDays(7)));

        var handler = MakeGoogleHandler();
        var result = await handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        Assert.True(result.IsNewUser);
        await _userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _profileRepo.Received(1).AddAsync(Arg.Any<MemberProfile>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ASSIGN ROLE TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRole_UserNotFound_ThrowsKeyNotFound()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = MakeAssignRoleHandler();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new AssignRoleCommand(Guid.NewGuid(), "Staff", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task AssignRole_RoleNotFound_ThrowsKeyNotFound()
    {
        var user = User.Create("Test", "test@example.com", "hash", null);
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var handler = MakeAssignRoleHandler();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new AssignRoleCommand(user.UserId, "Staff", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task AssignRole_DuplicateRole_ThrowsInvalidOperation()
    {
        var user = User.Create("Test", "test@example.com", "hash", null);
        var role = MakeRole("Staff");
        var userRole = UserRole.Create(user.UserId, role.RoleId);

        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetByNameAsync("Staff", Arg.Any<CancellationToken>()).Returns(role);
        _roleRepo.GetUserRoleAsync(user.UserId, role.RoleId, Arg.Any<CancellationToken>()).Returns(userRole);

        var handler = MakeAssignRoleHandler();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new AssignRoleCommand(user.UserId, "Staff", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task AssignRole_Success_AddsRoleAndPublishesEvent()
    {
        var actorId = Guid.NewGuid();
        var user = User.Create("Test", "test@example.com", "hash", null);
        var role = MakeRole("Staff");

        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetByNameAsync("Staff", Arg.Any<CancellationToken>()).Returns(role);
        _roleRepo.GetUserRoleAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserRole?)null);

        var handler = MakeAssignRoleHandler();
        await handler.Handle(new AssignRoleCommand(user.UserId, "Staff", actorId), CancellationToken.None);

        await _roleRepo.Received(1).AddUserRoleAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _events.Received(1).PublishAsync(
            Arg.Any<string>(),
            Arg.Is<UserRoleChangedEvent>(e => e.Action == "Assigned" && e.ChangedBy == actorId));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REVOKE ROLE TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeRole_RoleNotFound_ThrowsKeyNotFound()
    {
        _roleRepo.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var handler = MakeRevokeRoleHandler();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new RevokeRoleCommand(Guid.NewGuid(), "Admin", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RevokeRole_AssignmentNotFound_ThrowsKeyNotFound()
    {
        var role = MakeRole("Staff");
        _roleRepo.GetByNameAsync("Staff", Arg.Any<CancellationToken>()).Returns(role);
        _roleRepo.GetUserRoleAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserRole?)null);

        var handler = MakeRevokeRoleHandler();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new RevokeRoleCommand(Guid.NewGuid(), "Staff", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RevokeRole_LastAdmin_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var role = MakeRole("Admin");
        var userRole = UserRole.Create(userId, role.RoleId);

        _roleRepo.GetByNameAsync("Admin", Arg.Any<CancellationToken>()).Returns(role);
        _roleRepo.GetUserRoleAsync(userId, role.RoleId, Arg.Any<CancellationToken>()).Returns(userRole);
        _userRepo.CountAdminsAsync(Arg.Any<CancellationToken>()).Returns(1); // only 1 admin

        var handler = MakeRevokeRoleHandler();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new RevokeRoleCommand(userId, "Admin", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RevokeRole_Success_RemovesRoleRevokesSessionsPublishesEvent()
    {
        var targetId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var role = MakeRole("Staff");
        var userRole = UserRole.Create(targetId, role.RoleId);

        _roleRepo.GetByNameAsync("Staff", Arg.Any<CancellationToken>()).Returns(role);
        _roleRepo.GetUserRoleAsync(targetId, role.RoleId, Arg.Any<CancellationToken>()).Returns(userRole);

        var handler = MakeRevokeRoleHandler();
        await handler.Handle(new RevokeRoleCommand(targetId, "Staff", actorId), CancellationToken.None);

        await _roleRepo.Received(1).RemoveUserRoleAsync(userRole, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokenCache.Received(1).RevokeUserSessionsAsync(targetId, Arg.Any<CancellationToken>());
        await _events.Received(1).PublishAsync(
            Arg.Any<string>(),
            Arg.Is<UserRoleChangedEvent>(e => e.Action == "Revoked" && e.ChangedBy == actorId));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CHANGE USER STATUS TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_UserNotFound_ThrowsKeyNotFound()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = MakeChangeStatusHandler();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new ChangeUserStatusCommand(Guid.NewGuid(), false, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ChangeStatus_SelfDeactivation_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("Test", "test@example.com", "hash", null);
        typeof(User).GetProperty("UserId")!.SetValue(user, userId);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = MakeChangeStatusHandler();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new ChangeUserStatusCommand(userId, false, userId), CancellationToken.None));
    }

    [Fact]
    public async Task ChangeStatus_ActiveToInactive_RevokesSessionsAndPublishesEvent()
    {
        var targetId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var user = User.Create("Test", "test@example.com", "hash", null);
        typeof(User).GetProperty("UserId")!.SetValue(user, targetId);
        // user starts active
        _userRepo.GetByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = MakeChangeStatusHandler();
        await handler.Handle(new ChangeUserStatusCommand(targetId, false, actorId), CancellationToken.None);

        Assert.False(user.IsActive);
        await _refreshTokenCache.Received(1).RevokeUserSessionsAsync(targetId, Arg.Any<CancellationToken>());
        await _events.Received(1).PublishAsync(
            Arg.Any<string>(),
            Arg.Is<UserStatusChangedEvent>(e => e.NewIsActive == false && e.OldIsActive == true));
    }

    [Fact]
    public async Task ChangeStatus_InactiveToActive_DoesNotRevokeSessions()
    {
        var targetId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var user = User.Create("Test", "test@example.com", "hash", null);
        typeof(User).GetProperty("UserId")!.SetValue(user, targetId);
        user.SetActive(false); // start inactive

        _userRepo.GetByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = MakeChangeStatusHandler();
        await handler.Handle(new ChangeUserStatusCommand(targetId, true, actorId), CancellationToken.None);

        Assert.True(user.IsActive);
        await _refreshTokenCache.DidNotReceive().RevokeUserSessionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // USER QUERIES TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersQuery_PaginationRespected()
    {
        var users = new List<User>
        {
            User.Create("User A", "a@test.com", "h", null),
            User.Create("User B", "b@test.com", "h", null)
        };

        _userRepo.GetPagedAsync(
            Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<bool?>(),
            Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns((users.AsReadOnly() as IReadOnlyList<User>, 2));

        var handler = new GetUsersQueryHandler(_userRepo);
        var result = await handler.Handle(new GetUsersQuery(1, 10), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetUsersQuery_NoSensitiveFieldsExposed()
    {
        var users = new List<User>
        {
            User.Create("User A", "a@test.com", "hash_should_not_appear", null)
        };

        _userRepo.GetPagedAsync(
            Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<bool?>(),
            Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)users, 1));

        var handler = new GetUsersQueryHandler(_userRepo);
        var result = await handler.Handle(new GetUsersQuery(1, 10), CancellationToken.None);

        var dto = result.Items.First();
        // Verify the DTO type has no PasswordHash field
        var props = typeof(UserDto).GetProperties();
        Assert.DoesNotContain(props, p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, p => p.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, p => p.Name.Contains("Otp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, p => p.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetUsersQuery_InvalidPageSize_ClampedTo100()
    {
        _userRepo.GetPagedAsync(
            1, 100, // clamped
            Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<bool?>(),
            Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)new List<User>(), 0));

        var handler = new GetUsersQueryHandler(_userRepo);
        var result = await handler.Handle(new GetUsersQuery(1, 9999), CancellationToken.None); // pageSize clamped

        Assert.Equal(100, result.PageSize);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Handler factories
    // ─────────────────────────────────────────────────────────────────────────

    private GoogleLoginCommandHandler MakeGoogleHandler() => new(
        _googleValidator, _userRepo, _roleRepo, _profileRepo,
        _jwtService, _refreshTokenCache, _events, _uow,
        Substitute.For<ILogger<GoogleLoginCommandHandler>>());

    private AssignRoleCommandHandler MakeAssignRoleHandler() => new(
        _userRepo, _roleRepo, _events, _uow,
        Substitute.For<ILogger<AssignRoleCommandHandler>>());

    private RevokeRoleCommandHandler MakeRevokeRoleHandler() => new(
        _userRepo, _roleRepo, _refreshTokenCache, _events, _uow,
        Substitute.For<ILogger<RevokeRoleCommandHandler>>());

    private ChangeUserStatusCommandHandler MakeChangeStatusHandler() => new(
        _userRepo, _refreshTokenCache, _blacklist, _events, _uow,
        Substitute.For<ILogger<ChangeUserStatusCommandHandler>>());
}
