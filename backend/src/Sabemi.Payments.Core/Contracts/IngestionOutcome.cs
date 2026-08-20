using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Core.Contracts;

public abstract record IngestionOutcome
{
    public sealed record Unparseable(string Message) : IngestionOutcome;

    public sealed record Accepted(Guid EventId) : IngestionOutcome;

    public sealed record Rejected(Guid EventId, IReadOnlyList<string> Errors) : IngestionOutcome;

    public sealed record Duplicate(Guid EventId, EventProcessingStatus Status, bool PayloadDiverges) : IngestionOutcome;
}
