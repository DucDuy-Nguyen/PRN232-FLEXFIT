using MediatR;

namespace FlexFit.Identity.Application.Authentication.VerifyEmail;

public sealed record VerifyEmailCommand(
    string Email,
    string OtpCode) : IRequest<VerifyEmailResponse>;

public sealed record VerifyEmailResponse(
    string Message);
