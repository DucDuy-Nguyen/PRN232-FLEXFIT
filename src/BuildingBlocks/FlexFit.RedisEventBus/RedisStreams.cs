namespace FlexFit.RedisEventBus;

public static class RedisStreams
{
    public const string IdentityEvents = "flexfit:events:identity";
    public const string CatalogEvents = "flexfit:events:catalog";
    public const string BookingEvents = "flexfit:events:booking";
    public const string PaymentEvents = "flexfit:events:payment";
    public const string EngagementEvents = "flexfit:events:engagement";

    public const string DeadLetterEvents = "flexfit:events:dead-letter";
}
