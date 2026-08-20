namespace Sabemi.Payments.Core.Domain;

public enum EventProcessingStatus
{
    Pending,

    Processing,

    Processed,

    Invalid,

    Failed,

    PermanentlyFailed
}
