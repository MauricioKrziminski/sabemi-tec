namespace Sabemi.Payments.Core.Processing;

public interface IPaymentEventQueue
{
    bool TryEnqueue(Guid eventId);
}
