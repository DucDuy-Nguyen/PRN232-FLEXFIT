using MediatR;
using FlexFit.Identity.Domain.Enums;

namespace FlexFit.Identity.Application.Authentication.ResendOtp;

public sealed record ResendOtpCommand(
    string Email,
    OtpPurpose Purpose) : IRequest<ResendOtpResponse>;

public sealed record ResendOtpResponse(
    string Message);
