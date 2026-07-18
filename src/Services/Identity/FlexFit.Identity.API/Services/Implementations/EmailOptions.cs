using System.ComponentModel.DataAnnotations;

namespace FlexFit.Identity.API.Services.Implementations;

public sealed class EmailOptions
{
    public const string SectionName = "EmailSettings";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "SMTP Port must be a valid port number.")]
    public int Port { get; init; } = 587;

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string SenderEmail { get; init; } = string.Empty;

    [Required]
    public string SenderName { get; init; } = string.Empty;
}
