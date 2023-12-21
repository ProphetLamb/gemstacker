using DotNet.Globbing;
using GemLevelProtScraper.Poe;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.PoeNinja;

public sealed class PoeNinjaRepository(IOptions<PoeNinjaDatabaseSettings> settings, IMongoMigrationCompletion completion, ISystemClock clock)
{
    private readonly IMongoCollection<PoeNinjaApiGemPriceEnvalope> _gemPriceCollection = settings.Value.GetGemPriceCollection();

    internal async Task AddOrUpdateAsync(LeagueMode leagueMode, PoeNinjaApiGemPrice newGemPrice, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        _ = await _gemPriceCollection.FindOneAndReplaceAsync(
            gem
                => gem.League == leagueMode
                && gem.Price.GemLevel == newGemPrice.GemLevel
                && gem.Price.GemQuality == newGemPrice.GemQuality
                && gem.Price.Name == newGemPrice.Name,
            new(leagueMode, clock.UtcNow.UtcDateTime, newGemPrice),
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameAsync(LeagueMode league, string? skillName, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(skillName))
        {
            return await _gemPriceCollection
                .Find(e => e.League == league)
                .Project(g => g.Price)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        return await _gemPriceCollection
            .Find(g => g.Price.Name == skillName)
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameListAsync(LeagueMode league, IEnumerable<string> skillNames, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var skillNameSet = skillNames.ToHashSet();
        return await _gemPriceCollection
            .Find(s => s.League == league && skillNameSet.Contains(s.Price.Name))
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<long> RemoveOlderThanAsync(LeagueMode league, DateTimeOffset oldestDateTime, CancellationToken cancellationToken = default)
    {
        var utcTimestamp = oldestDateTime.UtcDateTime;
        var result = await _gemPriceCollection
            .DeleteManyAsync(g => g.League == league && g.UtcTimestamp < utcTimestamp, cancellationToken)
            .ConfigureAwait(false);
        return result.IsAcknowledged ? result.DeletedCount : -1;
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameGlobAsync(LeagueMode league, string? nameWildcard, CancellationToken cancellationToken = default)
    {
        if (nameWildcard == "*")
        {
            nameWildcard = null;
        }
        if (nameWildcard is null || !nameWildcard.ContainsGlobChars())
        {
            return await GetByNameAsync(league, nameWildcard, cancellationToken).ConfigureAwait(false);
        }
        var names = await ListNamesAsync(league, cancellationToken).ConfigureAwait(false);
        var nameGlob = Glob.Parse(nameWildcard);
        var validNamed = names.Where(nameGlob.IsMatch);
        var prices = await GetByNameListAsync(league, validNamed, cancellationToken).ConfigureAwait(false);
        return prices;
    }


    internal async Task<IReadOnlyList<string>> ListNamesAsync(LeagueMode league, CancellationToken cancellationToken = default)
    {
        return await _gemPriceCollection
            .Find(e => e.League == league)
            .Project(s => s.Price.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
