using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;
using MongoDB.Migration.Core;

namespace GemLevelProtScraper;

public sealed record PoeDatabaseSettings : IOptions<PoeDatabaseSettings>, IMongoMigratable
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public required string LeaguesCollectionName { get; init; }
    public required string SkillCollectionName { get; init; }
    public required string GemPriceCollectionName { get; init; }

    public const string Alias = "Poe";

    PoeDatabaseSettings IOptions<PoeDatabaseSettings>.Value => this;

    public MongoMigrableDefinition GetMigratableDefinition()
    {
        return new()
        {
            ConnectionString = ConnectionString,
            Database = new(Alias, DatabaseName),
            MirgrationStateCollectionName = "DATABASE_MIGRATIONS"
        };
    }

    internal IMongoCollection<PoeLeague> GetLeagueCollection()
    {
        MongoClient client = new(ConnectionString);
        var database = client.GetDatabase(DatabaseName);
        return GetLeagueCollection(database);
    }

    internal IMongoCollection<PoeLeague> GetLeagueCollection(IMongoDatabase database)
    {
        return database.GetCollection<PoeLeague>(LeaguesCollectionName);
    }

    internal IMongoCollection<PoeDbSkillEnvalope> GetSkillCollection()
    {
        MongoClient client = new(ConnectionString);
        var database = client.GetDatabase(DatabaseName);
        return GetSkillCollection(database);
    }

    internal IMongoCollection<PoeDbSkillEnvalope> GetSkillCollection(IMongoDatabase database)
    {
        return database.GetCollection<PoeDbSkillEnvalope>(SkillCollectionName);
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

[MongoMigration(PoeDatabaseSettings.Alias, 4, 5, Description = $"Add text index {GemLeagueIndexName}.")]
public sealed class PoeNinjaAddLeagueIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
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

[MongoMigration(PoeDatabaseSettings.Alias, 3, 4, Description = $"Add text index {GemNameIndexName}.")]
public sealed class PoeNinjaAddNameIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
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
        var index = builder.Hashed(e => e.Price.Name);

        CreateIndexModel<PoeNinjaApiGemPriceEnvalope> model = new(index, new()
        {
            Name = GemNameIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}

[MongoMigration(PoeDatabaseSettings.Alias, 2, 3, Description = $"Add unique composite index {GemIdentifierIndexName}.")]
public sealed class PoeNinjaAddIdentifierIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
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
            builder.Ascending(e => e.Price.Name),
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


[MongoMigration(PoeDatabaseSettings.Alias, 1, 2, Description = $"Add unique index {SkillNameIndexName}.")]
public sealed class PoeDbAddNameIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string SkillNameIndexName = "Name";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetSkillCollection(database);
        await col.Indexes.DropOneAsync(SkillNameIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetSkillCollection(database);
        IndexKeysDefinitionBuilder<PoeDbSkillEnvalope> builder = new();
        var index = builder.Ascending(e => e.Skill.Name.Id);

        CreateIndexModel<PoeDbSkillEnvalope> model = new(index, new()
        {
            Unique = true,
            Name = SkillNameIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}


[MongoMigration(PoeDatabaseSettings.Alias, 0, 1, Description = $"Add unique index {LeagueNameRealmIndexName}.")]
public sealed class PoeApiAddNameIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string LeagueNameRealmIndexName = "NameRealm";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetLeagueCollection(database);
        await col.Indexes.DropOneAsync(LeagueNameRealmIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetLeagueCollection(database);
        IndexKeysDefinitionBuilder<PoeLeague> builder = new();
        var index = builder.Combine(
            builder.Ascending(e => e.Name),
            builder.Ascending(e => e.Realm)
        );

        CreateIndexModel<PoeLeague> model = new(index, new()
        {
            Unique = true,
            Name = LeagueNameRealmIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}
