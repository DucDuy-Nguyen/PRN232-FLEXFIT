namespace FlexFit.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = string.Empty;
    public string InstanceName { get; init; } = "flexfit:";
}
