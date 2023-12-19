using ScrapeAAS;

namespace GemLevelProtScraper;

public sealed class DataflowSignal<T> : IDataflowHandler<T>
{
    private readonly TaskCompletionSource<T> _tcs = new();

    ValueTask IDataflowHandler<T>.HandleAsync(T message, CancellationToken cancellationToken)
    {
        _tcs.TrySetResult(message);
        return default;
    }

    public Task<T> WaitAsync(CancellationToken cancellationToken = default)
    {
        return _tcs.Task.WaitAsync(cancellationToken);
    }
}
