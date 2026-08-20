using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Sabemi.Payments.Core.Processing;

namespace Sabemi.Payments.Infrastructure.Processing;

public sealed class ChannelPaymentEventQueue(ILogger<ChannelPaymentEventQueue> logger) : IPaymentEventQueue
{
    private const int Capacity = 1_000;

    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public bool TryEnqueue(Guid eventId)
    {
        if (_channel.Writer.TryWrite(eventId))
        {
            return true;
        }

        logger.LogWarning(
            "Fila de processamento cheia. O evento {EventId} será recuperado pela varredura periódica.",
            eventId);

        return false;
    }
}
