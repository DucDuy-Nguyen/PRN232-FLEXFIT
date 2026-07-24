using System;
using System.Collections.Generic;

namespace FlexFit.Identity.Service.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    string? JwtId { get; }
    IReadOnlyCollection<string> Roles { get; }
}
