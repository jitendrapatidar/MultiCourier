using System.Threading.Channels;
using MultiCourie.Application.Orders;

namespace MultiCourie.Infrastructure.Background;
/// <summary>
/// A thread-safe queue for bulk order work items, implemented using a channel.
/// </summary>
public sealed class BulkOrderChannelQueue : IBulkOrderQueue
{
    private readonly Channel<BulkOrderWorkItem> _channel = Channel.CreateUnbounded<BulkOrderWorkItem>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<BulkOrderWorkItem> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(BulkOrderWorkItem workItem, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(workItem, cancellationToken);
}
