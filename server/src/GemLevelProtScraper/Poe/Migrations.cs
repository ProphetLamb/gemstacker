using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;
using MongoDB.Migration.Core;

namespace GemLevelProtScraper.Poe;

public sealed record PoeDatabaseSettings : IOptions<PoeDatabaseSettings>, IMongoMigratable
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public required string LeaguesCollectionName { get; init; }

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
}


[MongoMigration(PoeDatabaseSettings.Alias, 0, 1, Description = $"Add unique index {LeagueNameRealmIndexName}.")]
public sealed class PoeNinjaAddNameIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
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
