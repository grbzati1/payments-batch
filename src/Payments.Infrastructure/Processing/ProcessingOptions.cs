namespace Payments.Infrastructure.Processing;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";
    public int PollIntervalSeconds { get; set; } = 2;
    public int RetryDelaySeconds { get; set; } = 5;
    public int MaxRetryCount { get; set; } = 3;
    public int StaleProcessingThresholdSeconds { get; set; } = 60;
}
