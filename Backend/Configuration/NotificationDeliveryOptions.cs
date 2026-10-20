namespace Backend.Configuration;

public class NotificationDeliveryOptions
{
    public const string SectionName = "NotificationDelivery";

    // How often the worker looks for deliveries that are due.
    public int PollIntervalSeconds { get; set; } = 15;

    // How many deliveries one run handles at most.
    public int BatchSize { get; set; } = 50;

    // After this many failed attempts a delivery becomes Undeliverable.
    public int MaxAttempts { get; set; } = 5;

    // The wait before retry n is BaseRetryDelaySeconds * 2^(n-1), capped at MaxRetryDelaySeconds.
    public int BaseRetryDelaySeconds { get; set; } = 30;

    public int MaxRetryDelaySeconds { get; set; } = 3600;
}
