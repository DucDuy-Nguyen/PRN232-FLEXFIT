using System;
using System.Collections.Generic;

namespace FlexFit.Identity.Application.Abstractions;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    string? JwtId { get; }
    IReadOnlyCollection<string> Roles { get; }
}
