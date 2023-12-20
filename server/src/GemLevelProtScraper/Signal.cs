using System.Runtime.CompilerServices;
using ScrapeAAS;

namespace GemLevelProtScraper;

public sealed class DataflowSignal<T> : IDataflowHandler<T>, IAsyncEnumerable<T>
{
    private TaskCompletionSource<T> _tcs = new();

    ValueTask IDataflowHandler<T>.HandleAsync(T message, CancellationToken cancellationToken)
    {
        var tcs = Interlocked.Exchange(ref _tcs, new());
        _ = tcs.TrySetResult(message);
        return default;
    }

    public Task<T> WaitAsync(CancellationToken cancellationToken = default)
    {
        return _tcs.Task.WaitAsync(cancellationToken);
    }

    public async Task<T?> WaitAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        await using var en = GetAsyncEnumerator(cancellationToken);
        while (await en.MoveNextAsync().ConfigureAwait(false))
        {
            var item = en.Current;
            if (predicate(item))
            {
                return item;
            }
        }

        return default;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return Enumerable(this, cancellationToken).GetAsyncEnumerator(cancellationToken);

        static async IAsyncEnumerable<T> Enumerable(DataflowSignal<T> signal, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var task = signal.WaitAsync(cancellationToken);
            while (true)
            {
                var innerTask = Interlocked.Exchange(ref task, signal.WaitAsync(cancellationToken));
                var item = await innerTask.ConfigureAwait(false);
                yield return item;
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

}
