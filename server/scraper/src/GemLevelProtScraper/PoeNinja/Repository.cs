using DotNet.Globbing;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.PoeNinja;

public sealed class PoeNinjaRepository(IOptions<PoeNinjaDatabaseSettings> settings, IMongoMigrationCompletion completion)
{
    private readonly IMongoCollection<PoeNinjaApiGemPrice> _gemPriceCollection = new MongoClient(settings.Value.ConnectionString)
        .GetDatabase(settings.Value.DatabaseName)
        .GetCollection<PoeNinjaApiGemPrice>(settings.Value.GemPriceCollectionName);

    internal async Task<PoeNinjaApiGemPrice> AddOrUpdateAsync(PoeNinjaApiGemPrice newGemPrice, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _gemPriceCollection.FindOneAndReplaceAsync(
            gemPrice
                => gemPrice.GemLevel == newGemPrice.GemLevel
                && gemPrice.GemQuality == newGemPrice.GemQuality
                && gemPrice.Name == newGemPrice.Name,
            newGemPrice,
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameAsync(string skillName, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _gemPriceCollection.Find(p => p.Name == skillName).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeNinjaApiGemPrice>> GetByNameGlobAsync(string nameWildcard, CancellationToken cancellationToken)
    {
        if (!nameWildcard.ContainsGlobChars())
        {
            return await GetByNameAsync(nameWildcard, cancellationToken).ConfigureAwait(false);
        }
        var names = await ListNamesAsync(cancellationToken).ConfigureAwait(false);
        var nameGlob = Glob.Parse(nameWildcard);
        var validNamed = names.Where(nameGlob.IsMatch).Distinct();
        var prices = await Task.WhenAll(validNamed.Select(n => GetByNameAsync(n, cancellationToken))).ConfigureAwait(false);
        return prices.SelectMany(p => p).ToArray();
    }


    internal async Task<IReadOnlyList<string>> ListNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _gemPriceCollection.Find(s => true).Project(s => s.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
