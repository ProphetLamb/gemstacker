
using System.Runtime.CompilerServices;
using ScrapeAAS;

namespace GemLevelProtScraper;

public sealed class SignalTaskStorage
{
    private readonly Dictionary<Type, object> _tcsByType = new();

    public TaskCompletionSource<T> GetOrAdd<T>()
    {
        lock (_tcsByType)
        {
            if (!_tcsByType.TryGetValue(typeof(T), out var tcs))
            {
                tcs = new TaskCompletionSource<T>();
                _tcsByType[typeof(T)] = tcs;
            }
            return ((TaskCompletionSource<T>)tcs)!;
        }
    }

    public TaskCompletionSource<T>? Replace<T>(T result)
    {
        lock (_tcsByType)
        {
            if (_tcsByType.TryGetValue(typeof(T), out var tcs))
            {
                var existing = ((TaskCompletionSource<T>)tcs)!;
                _ = existing.TrySetResult(result);

                tcs = new TaskCompletionSource<T>();
                _tcsByType[typeof(T)] = tcs;
                return ((TaskCompletionSource<T>)tcs)!;
            }
            return null;
        }
    }
}

public sealed class DataflowSignal<T>(SignalTaskStorage storage) : IDataflowHandler<T>, IAsyncEnumerable<T>
{
    ValueTask IDataflowHandler<T>.HandleAsync(T message, CancellationToken cancellationToken)
    {
        _ = storage.Replace(message);
        return default;
    }

    public Task<T> WaitAsync(CancellationToken cancellationToken = default)
    {
        return storage.GetOrAdd<T>().Task.WaitAsync(cancellationToken);
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
