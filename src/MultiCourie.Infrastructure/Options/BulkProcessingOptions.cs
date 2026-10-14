namespace MultiCourie.Infrastructure.Options;

public sealed class BulkProcessingOptions
{
    public const string SectionName = "BulkProcessing";

    public int MaxConcurrency { get; set; } = 10;
}
