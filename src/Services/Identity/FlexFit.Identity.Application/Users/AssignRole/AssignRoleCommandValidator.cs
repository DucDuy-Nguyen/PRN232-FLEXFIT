using FluentValidation;

namespace FlexFit.Identity.Application.Users.AssignRole;

public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Member", "Staff", "GymPartner"
    };

    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage("Invalid role name. Allowed: Admin, Member, Staff, GymPartner.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor user ID is required.");
    }
}
