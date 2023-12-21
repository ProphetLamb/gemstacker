using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GemLevelProtScraper.Poe;

public sealed class PoeRepository(IOptions<PoeDatabaseSettings> settings, DataflowSignal<PoeLeagueListCompleted> poeLeagueListCompleted)
{
    private readonly IMongoCollection<PoeLeague> _leagueCollection = new MongoClient(settings.Value.ConnectionString)
            .GetDatabase(settings.Value.DatabaseName)
            .GetCollection<PoeLeague>(settings.Value.LeaguesCollectionName);


    internal async Task<PoeLeague> AddOrUpdateAsync(PoeLeague newLeague, CancellationToken cancellationToken = default)
    {
        // _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _leagueCollection.FindOneAndReplaceAsync(
            league => league.Mode == newLeague.Mode && league.Realm == newLeague.Realm,
            newLeague,
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<PoeLeague?> GetByModeAndRealmAsync(LeagueMode mode, Realm realm, CancellationToken cancellationToken = default)
    {
        await poeLeagueListCompleted.WaitInitialAsync(cancellationToken).ConfigureAwait(false);
        return await _leagueCollection
            .Find(l => l.Mode == mode && l.Realm == realm)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
