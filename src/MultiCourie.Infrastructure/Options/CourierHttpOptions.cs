namespace MultiCourie.Infrastructure.Options;

public sealed class CourierHttpOptions
{
    public const string SectionName = "CourierHttp";

    public int TimeoutSeconds { get; set; } = 30;

    public int RetryCount { get; set; } = 3;

    public int RetryBaseDelayMilliseconds { get; set; } = 500;
}
