using System;
using System.Collections.Generic;

namespace FlexFit.Identity.API.Models.DTOs;

public sealed record UserDto(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsEmailVerified,
    bool IsActive,
    string? AvatarUrl,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastLoginAt);
