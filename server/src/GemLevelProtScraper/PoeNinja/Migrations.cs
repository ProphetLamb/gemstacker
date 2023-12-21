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

    public const string Alias = "PoeNinja";

    PoeNinjaDatabaseSettings IOptions<PoeNinjaDatabaseSettings>.Value => this;

    public MongoMigrableDefinition GetMigratableDefinition()
    {
        return new()
        {
            ConnectionString = ConnectionString,
            Database = new(Alias, DatabaseName),
            MirgrationStateCollectionName = "DATABASE_MIGRATIONS"
        };
    }

    internal IMongoCollection<PoeNinjaApiGemPriceEnvalope> GetGemPriceCollection()
    {
        MongoClient client = new(ConnectionString);
        var database = client.GetDatabase(DatabaseName);
        return GetGemPriceCollection(database);
    }
    internal IMongoCollection<PoeNinjaApiGemPriceEnvalope> GetGemPriceCollection(IMongoDatabase database)
    {
        return database.GetCollection<PoeNinjaApiGemPriceEnvalope>(GemPriceCollectionName);
    }
}

[MongoMigration(PoeNinjaDatabaseSettings.Alias, 2, 3, Description = $"Add text index {GemLeagueIndexName}.")]
public sealed class PoeNinjaAddLeagueIndexMigration(IOptions<PoeNinjaDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string GemLeagueIndexName = "GemLeague";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetGemPriceCollection(database);
        await col.Indexes.DropOneAsync(GemLeagueIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetGemPriceCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPriceEnvalope> builder = new();
        var index = builder.Text(e => e.League);

        CreateIndexModel<PoeNinjaApiGemPriceEnvalope> model = new(index, new()
        {
            Name = GemLeagueIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }

}

[MongoMigration(PoeNinjaDatabaseSettings.Alias, 1, 2, Description = $"Add text index {GemNameIndexName}.")]
public sealed class PoeNinjaAddNameIndexMigration(IOptions<PoeNinjaDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string GemNameIndexName = "GemName";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetGemPriceCollection(database);
        await col.Indexes.DropOneAsync(GemNameIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetGemPriceCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPriceEnvalope> builder = new();
        var index = builder.Text(e => e.Price.Name);

        CreateIndexModel<PoeNinjaApiGemPriceEnvalope> model = new(index, new()
        {
            Name = GemNameIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}

[MongoMigration(PoeNinjaDatabaseSettings.Alias, 0, 1, Description = $"Add unique composite index {GemIdentifierIndexName}.")]
public sealed class PoeNinjaAddIdentifierIndexMigration(IOptions<PoeNinjaDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string GemIdentifierIndexName = "GemIdentifier";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var gemPriceCollection = optionsAccessor.Value.GetGemPriceCollection(database);
        await gemPriceCollection.Indexes.DropOneAsync(GemIdentifierIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetGemPriceCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPriceEnvalope> builder = new();
        var index = builder.Combine(
            builder.Text(e => e.Price.Name),
            builder.Ascending(e => e.League),
            builder.Ascending(e => e.Price.GemLevel),
            builder.Ascending(e => e.Price.GemQuality)
        );

        CreateIndexModel<PoeNinjaApiGemPriceEnvalope> model = new(index, new()
        {
            Unique = true,
            Name = GemIdentifierIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}
