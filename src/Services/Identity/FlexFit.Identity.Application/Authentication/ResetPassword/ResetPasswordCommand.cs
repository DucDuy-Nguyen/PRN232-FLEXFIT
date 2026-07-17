using MediatR;

namespace FlexFit.Identity.Application.Authentication.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string OtpCode,
    string Password,
    string ConfirmPassword) : IRequest<ResetPasswordResponse>;

public sealed record ResetPasswordResponse(
    string Message);
