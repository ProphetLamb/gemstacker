using DotNet.Globbing;
using GemLevelProtScraper.Poe;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.PoeNinja;

public sealed class PoeNinjaIncursionTempleRepository(
    IOptions<PoeDatabaseSettings> settings,
    IMongoMigrationCompletion completion,
    ISystemClock clock
)
{
    private readonly IMongoCollection<PoeNinjaApiIncursionTemplePriceEnvelope> _templeCollection = settings.Value.GetPoeNinjaTempleCollection();

    internal async Task AddOrUpdateAsync(LeagueMode leagueMode, PoeNinjaApiTemplePrice newPrice, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        _ = await _templeCollection.FindOneAndReplaceAsync(
            item
                => item.League == leagueMode
                   && item.Price.Name == newPrice.Name,
            new(leagueMode, clock.UtcNow.UtcDateTime, newPrice),
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiTemplePrice>> GetByNameAsync(LeagueMode league, string? templeName, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(templeName))
        {
            return await _templeCollection
                .Find(e => e.League == league)
                .Project(g => g.Price)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        return await _templeCollection
            .Find(g => g.Price.Name == templeName)
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IAsyncCursor<PoeNinjaCurrencyExchangeRate>> GetExchangeRatesAsync(LeagueMode league, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var cursor = await _templeCollection
            .Find(e => e.League == league)
            .Project(s => new PoeNinjaCurrencyExchangeRate(s.Price.Name, s.Price.ChaosValue))
            .ToCursorAsync(cancellationToken)
            .ConfigureAwait(false);
        return cursor;
    }

    internal async Task<IAsyncCursor<KeyValuePair<LeagueMode, PoeNinjaCurrencyExchangeRate>>> GetAllExchangeRatesAsync(
        CancellationToken cancellationToken = default
    )
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var cursor = await _templeCollection
            .Find(x => true)
            .Project(s => new KeyValuePair<LeagueMode, PoeNinjaCurrencyExchangeRate>(s.League, new PoeNinjaCurrencyExchangeRate(s.Price.Name, s.Price.ChaosValue)))
            .ToCursorAsync(cancellationToken)
            .ConfigureAwait(false);
        return cursor;
    }
}

public sealed class PoeNinjaGemRepository(IOptions<PoeDatabaseSettings> settings, IMongoMigrationCompletion completion, ISystemClock clock)
{
    private readonly IMongoCollection<PoeNinjaApiGemPriceEnvelope> _gemCollection = settings.Value.GetPoeNinjaGemCollection();

    internal async Task AddOrUpdateAsync(LeagueMode leagueMode, PoeNinjaApiGemPrice newPrice, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        _ = await _gemCollection.FindOneAndReplaceAsync(
            item
                => item.League == leagueMode
                   && item.Price.GemLevel == newPrice.GemLevel
                   && item.Price.GemQuality == newPrice.GemQuality
                   && item.Price.Name == newPrice.Name,
            new(leagueMode, clock.UtcNow.UtcDateTime, newPrice),
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameAsync(LeagueMode league, string? skillName, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(skillName))
        {
            return await _gemCollection
                .Find(e => e.League == league)
                .Project(g => g.Price)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        return await _gemCollection
            .Find(g => g.Price.Name == skillName)
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameListAsync(LeagueMode league, IEnumerable<string> skillNames, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var skillNameSet = skillNames.ToHashSet();
        return await _gemCollection
            .Find(s => s.League == league && skillNameSet.Contains(s.Price.Name))
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<long> RemoveOlderThanAsync(LeagueMode league, DateTimeOffset oldestDateTime, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var utcTimestamp = oldestDateTime.UtcDateTime;
        var result = await _gemCollection
            .DeleteManyAsync(g => g.League == league && g.UtcTimestamp < utcTimestamp, cancellationToken)
            .ConfigureAwait(false);
        return result.IsAcknowledged ? result.DeletedCount : -1;
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameGlobAsync(LeagueMode league, string? nameWildcard, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
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
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _gemCollection
            .Find(e => e.League == league)
            .Project(s => s.Price.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}


public sealed class PoeNinjaCurrencyRepository(IOptions<PoeDatabaseSettings> settings, IMongoMigrationCompletion completion, ISystemClock clock)
{
    private readonly IMongoCollection<PoeNinjaApiCurrencyPriceEnvelope> _currencyCollection = settings.Value.GetPoeNinjaCurrencyCollection();

    internal async Task AddOrUpdateAsync(LeagueMode leagueMode, PoeNinjaApiCurrencyPrice newCurrencyPrice, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        _ = await _currencyCollection.FindOneAndReplaceAsync(
            gem
                => gem.League == leagueMode
                && gem.Price.CurrencyTypeName == newCurrencyPrice.CurrencyTypeName,
            new(leagueMode, clock.UtcNow.UtcDateTime, newCurrencyPrice),
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiCurrencyPrice>> GetByNameAsync(LeagueMode league, string? currencyName, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(currencyName))
        {
            return await _currencyCollection
                .Find(e => e.League == league)
                .Project(g => g.Price)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        return await _currencyCollection
            .Find(g => g.Price.CurrencyTypeName == currencyName)
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiCurrencyPrice>> GetByNameListAsync(LeagueMode league, IEnumerable<string> skillNames, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var skillNameSet = skillNames.ToHashSet();
        return await _currencyCollection
            .Find(s => s.League == league && skillNameSet.Contains(s.Price.CurrencyTypeName))
            .Project(g => g.Price)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<long> RemoveOlderThanAsync(LeagueMode league, DateTimeOffset oldestDateTime, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var utcTimestamp = oldestDateTime.UtcDateTime;
        var result = await _currencyCollection
            .DeleteManyAsync(g => g.League == league && g.UtcTimestamp < utcTimestamp, cancellationToken)
            .ConfigureAwait(false);
        return result.IsAcknowledged ? result.DeletedCount : -1;
    }

    internal async Task<IReadOnlyList<PoeNinjaApiCurrencyPrice>> GetByNameGlobAsync(LeagueMode league, string? nameWildcard, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
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
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _currencyCollection
            .Find(e => e.League == league)
            .Project(s => s.Price.CurrencyTypeName)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IAsyncCursor<PoeNinjaCurrencyExchangeRate>> GetExchangeRatesAsync(LeagueMode league, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var cursor = await _currencyCollection
            .Find(e => e.League == league)
            .Project(s => new PoeNinjaCurrencyExchangeRate(s.Price.CurrencyTypeName, s.Price.ChaosEquivalent))
            .ToCursorAsync(cancellationToken)
            .ConfigureAwait(false);
        return cursor;
    }

    internal async Task<IAsyncCursor<KeyValuePair<LeagueMode, PoeNinjaCurrencyExchangeRate>>> GetAllExchangeRatesAsync(
        CancellationToken cancellationToken = default
    )
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var cursor = await _currencyCollection
            .Find(x => true)
            .Project(s => new KeyValuePair<LeagueMode, PoeNinjaCurrencyExchangeRate>(s.League, new PoeNinjaCurrencyExchangeRate(s.Price.CurrencyTypeName, s.Price.ChaosEquivalent)))
            .ToCursorAsync(cancellationToken)
            .ConfigureAwait(false);
        return cursor;
    }
}
