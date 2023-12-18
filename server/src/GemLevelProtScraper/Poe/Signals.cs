using ScrapeAAS;

namespace GemLevelProtScraper.Poe;

public sealed class PoeLeagesListInitialSignal : IDataflowHandler<PoeLeagueListCompleted>
{
    private readonly TaskCompletionSource _tcs = new();

    ValueTask IDataflowHandler<PoeLeagueListCompleted>.HandleAsync(PoeLeagueListCompleted message, CancellationToken cancellationToken)
    {
        _tcs.TrySetResult();
        return default;
    }

    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        return _tcs.Task.WaitAsync(cancellationToken);
    }
}
