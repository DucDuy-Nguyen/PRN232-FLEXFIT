using MediatR;

namespace FlexFit.Identity.Application.Authentication.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email) : IRequest<ForgotPasswordResponse>;

public sealed record ForgotPasswordResponse(
    string Message);
