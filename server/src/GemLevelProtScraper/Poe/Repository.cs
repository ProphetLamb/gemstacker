using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.Poe;

public sealed class PoeRepository(
    IOptions<PoeDatabaseSettings> settings,
    IMongoMigrationCompletion completion,
    DataflowSignal<PoeLeagueListCompleted> poeLeagueListCompleted
)
{
    private readonly IMongoCollection<PoeLeague> _leagueCollection = settings.Value.GetPoeLeagueCollection();

    internal async Task<PoeLeague> AddOrUpdateAsync(PoeLeague newLeague, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _leagueCollection
            .FindOneAndReplaceAsync(
                league => league.Name == newLeague.Name && league.Realm == newLeague.Realm,
                newLeague,
                new() { IsUpsert = true },
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<PoeLeague?> ByName(
        string? leagueName,
        string? realmName,
        CancellationToken cancellationToken = default
    )
    {
        if (!RealmHelper.TryParse(realmName, out var realm))
        {
            if (string.IsNullOrEmpty(realmName))
            {
                realm = Realm.Pc;
            }
            else
            {
                return null;
            }
        }

        await poeLeagueListCompleted.WaitInitialAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(leagueName))
        {
            return await _leagueCollection
                .Find(x => x.Realm == realm)
                .SortBy(x => x.Name.Length)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return await _leagueCollection
            .Find(x => x.Name == leagueName && x.Realm == realm)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<PoeLeague>> GetLeagues(CancellationToken cancellationToken = default)
    {
        await poeLeagueListCompleted.WaitInitialAsync(cancellationToken).ConfigureAwait(false);
        return await _leagueCollection.Find(x => true).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
