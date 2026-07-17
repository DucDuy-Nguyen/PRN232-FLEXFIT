namespace FlexFit.Identity.API.Authorization;

public static class IdentityPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string ProfileOwnerOrAdmin = "ProfileOwnerOrAdmin";
    public const string UserManagement = "UserManagement";
}
