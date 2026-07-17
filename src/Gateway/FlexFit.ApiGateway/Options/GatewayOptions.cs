using System.Collections.Generic;

namespace FlexFit.ApiGateway.Options;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public List<string> AllowedOrigins { get; init; } = new();

    public long RequestBodyLimitBytes { get; init; } = 1048576; // Default to 1MB
}
