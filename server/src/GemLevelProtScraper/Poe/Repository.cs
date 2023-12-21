using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.Poe;

public sealed class PoeRepository(IOptions<PoeDatabaseSettings> settings, IMongoMigrationCompletion completion, DataflowSignal<PoeLeagueListCompleted> poeLeagueListCompleted)
{
    private readonly IMongoCollection<PoeLeague> _leagueCollection = settings.Value.GetLeagueCollection();

    internal async Task<PoeLeague> AddOrUpdateAsync(PoeLeague newLeague, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _leagueCollection.FindOneAndReplaceAsync(
            league => league.Mode == newLeague.Mode && league.Realm == newLeague.Realm,
            newLeague,
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<PoeLeague?> GetByModeAndRealmAsync(LeagueMode mode, Realm realm, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        await poeLeagueListCompleted.WaitInitialAsync(cancellationToken).ConfigureAwait(false);
        return await _leagueCollection
            .Find(l => l.Mode == mode && l.Realm == realm)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
