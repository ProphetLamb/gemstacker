using DotNet.Globbing;
using GemLevelProtScraper.Poe;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.PoeNinja;

public sealed class PoeNinjaRepository(IOptions<PoeNinjaDatabaseSettings> settings, IMongoMigrationCompletion completion, ISystemClock clock)
{
    private readonly IMongoCollection<PoeNinjaApiLeaugeGemPrice> _gemPriceCollection = new MongoClient(settings.Value.ConnectionString)
        .GetDatabase(settings.Value.DatabaseName)
        .GetCollection<PoeNinjaApiLeaugeGemPrice>(settings.Value.GemPriceCollectionName);

    internal async Task AddOrUpdateAsync(LeaugeMode leaugeMode, PoeNinjaApiGemPrice newGemPrice, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        _ = await _gemPriceCollection.FindOneAndReplaceAsync(
            gem
                => gem.Leauge == leaugeMode
                && gem.Price.GemLevel == newGemPrice.GemLevel
                && gem.Price.GemQuality == newGemPrice.GemQuality
                && gem.Price.Name == newGemPrice.Name,
            new(leaugeMode, clock.UtcNow.UtcDateTime, newGemPrice),
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameAsync(string? skillName, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(skillName))
        {
            return await _gemPriceCollection
                .Find(FilterDefinition<PoeNinjaApiLeaugeGemPrice>.Empty)
                .Project(g => g.Price)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        return await _gemPriceCollection
            .Find(g => g.Price.Name == skillName)
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameListAsync(IEnumerable<string> skillNames, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var skillNameSet = skillNames.ToHashSet();
        return await _gemPriceCollection
            .Find(s => skillNameSet.Contains(s.Price.Name))
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<long> RemoveOlderThanAsync(LeaugeMode league, DateTimeOffset oldestDateTime, CancellationToken cancellationToken = default)
    {
        var utcTimestamp = oldestDateTime.UtcDateTime;
        var result = await _gemPriceCollection
            .DeleteManyAsync(g => g.Leauge == league && g.UtcTimestamp < utcTimestamp, cancellationToken)
            .ConfigureAwait(false);
        return result.IsAcknowledged ? result.DeletedCount : -1;
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameGlobAsync(string? nameWildcard, CancellationToken cancellationToken = default)
    {
        if (nameWildcard == "*")
        {
            nameWildcard = null;
        }
        if (nameWildcard is null || !nameWildcard.ContainsGlobChars())
        {
            return await GetByNameAsync(nameWildcard, cancellationToken).ConfigureAwait(false);
        }
        var names = await ListNamesAsync(cancellationToken).ConfigureAwait(false);
        var nameGlob = Glob.Parse(nameWildcard);
        var validNamed = names.Where(nameGlob.IsMatch);
        var prices = await GetByNameListAsync(validNamed, cancellationToken).ConfigureAwait(false);
        return prices;
    }


    internal async Task<IReadOnlyList<string>> ListNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _gemPriceCollection
            .Find(FilterDefinition<PoeNinjaApiLeaugeGemPrice>.Empty)
            .Project(s => s.Price.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
