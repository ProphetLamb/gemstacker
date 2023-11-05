using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;
using MongoDB.Migration.Core;

namespace GemLevelProtScraper.PoeNinja;

public sealed record PoeNinjaDatabaseSettings : IOptions<PoeNinjaDatabaseSettings>, IMongoMigratable
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public required string GemPriceCollectionName { get; init; }

    PoeNinjaDatabaseSettings IOptions<PoeNinjaDatabaseSettings>.Value => this;

    public MongoMigrableDefinition GetMigratableDefinition()
    {
        return new()
        {
            ConnectionString = ConnectionString,
            Database = new("PoeNinja", DatabaseName),
            MirgrationStateCollectionName = "DATABASE_MIGRATIONS"
        };
    }
}

[MongoMigration("PoeNinja", 0, 1, Description = $"Add composite index {GemIdentifierIndexName}")]
public sealed class PoeNinjaAddCompositeIndexMigration(IOptions<PoeNinjaDatabaseSettings> optionsAccessor) : IMongoMigration
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

[MongoMigration("PoeNinja", 1, 2, Description = $"Add windcard index {NameWindcardIndexName}")]
public sealed class PoeNinjaAddNameWildcardIndexMigration(IOptions<PoeNinjaDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string NameWindcardIndexName = "NameWildcard";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var gemPriceCollection = GetGemPriceCollection(optionsAccessor, database);
        await gemPriceCollection.Indexes.DropOneAsync(NameWindcardIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var gemPriceCollection = GetGemPriceCollection(optionsAccessor, database);
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPrice> builder = new();
        var wildcardIndex = builder.Wildcard(p => p.Name);
        CreateIndexModel<PoeNinjaApiGemPrice> model = new(wildcardIndex, new()
        {
            Name = NameWindcardIndexName
        });
        _ = await gemPriceCollection.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }

    private static IMongoCollection<PoeNinjaApiGemPrice> GetGemPriceCollection(IOptions<PoeNinjaDatabaseSettings> optionsAccessor, IMongoDatabase database)
    {
        return database.GetCollection<PoeNinjaApiGemPrice>(optionsAccessor.Value.GemPriceCollectionName);
    }
}
