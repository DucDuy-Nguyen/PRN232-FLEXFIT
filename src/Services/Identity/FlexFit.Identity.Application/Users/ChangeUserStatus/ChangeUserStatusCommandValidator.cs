using FluentValidation;

namespace FlexFit.Identity.Application.Users.ChangeUserStatus;

public sealed class ChangeUserStatusCommandValidator : AbstractValidator<ChangeUserStatusCommand>
{
    public ChangeUserStatusCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor user ID is required.");
    }
}
