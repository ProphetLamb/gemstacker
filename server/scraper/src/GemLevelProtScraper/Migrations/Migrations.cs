using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GemLevelProtScraper.Migrations;

public sealed record DatabaseAlias(string Alias, string Name)
{
    public const string PoeDb = "PoeDb";
    public const string PoeNinja = "PoeNinja";
}

[MigrationDefinition(DatabaseAlias.PoeNinja, 0, 1, Description = $"Add composite index {GemIdentifierIndexName}")]
public sealed class PoeNinjaAddCompositeIndexMigration(IOptions<PoeNinjaDatabaseSettings> optionsAccessor) : IMigration
{
    public const string GemIdentifierIndexName = "GemIdentifier";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var gemPriceCollection = GetGemPriceCollection(optionsAccessor, database);
        await gemPriceCollection.Indexes.DropOneAsync(GemIdentifierIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var gemPriceCollection = GetGemPriceCollection(optionsAccessor, database);
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPrice> builder = new();
        var combinedIndex = builder.Combine(
            builder.Hashed(p => p.Name),
            builder.Ascending(p => p.GemLevel),
            builder.Ascending(p => p.GemQuality)
        );

        CreateIndexModel<PoeNinjaApiGemPrice> model = new(combinedIndex, new()
        {
            Unique = true,
            Name = GemIdentifierIndexName
        });
        _ = await gemPriceCollection.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }

    private static IMongoCollection<PoeNinjaApiGemPrice> GetGemPriceCollection(IOptions<PoeNinjaDatabaseSettings> optionsAccessor, IMongoDatabase database)
    {
        return database.GetCollection<PoeNinjaApiGemPrice>(optionsAccessor.Value.GemPriceCollectionName);
    }
}
