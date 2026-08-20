namespace Sabemi.Payments.Core.Processing;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";

    public TimeSpan SimulatedWorkDelay { get; set; } = TimeSpan.FromSeconds(2);

    public int MaxAttempts { get; set; } = 5;

    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    public int MaxDegreeOfParallelism { get; set; } = 4;

    public TimeSpan RecoveryInterval { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan StuckTimeout { get; set; } = TimeSpan.FromMinutes(2);

    public int RecoveryBatchSize { get; set; } = 100;
}
