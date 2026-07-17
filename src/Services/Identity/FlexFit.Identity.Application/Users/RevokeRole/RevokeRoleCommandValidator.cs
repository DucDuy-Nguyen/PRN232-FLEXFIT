using FluentValidation;

namespace FlexFit.Identity.Application.Users.RevokeRole;

public sealed class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor user ID is required.");
    }
}
