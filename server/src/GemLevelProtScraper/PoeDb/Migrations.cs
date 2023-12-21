using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;
using MongoDB.Migration.Core;

namespace GemLevelProtScraper.PoeDb;

public sealed record PoeDbDatabaseSettings : IOptions<PoeDbDatabaseSettings>, IMongoMigratable
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public required string SkillCollectionName { get; init; }

    public const string Alias = "PoeDb";

    PoeDbDatabaseSettings IOptions<PoeDbDatabaseSettings>.Value => this;

    public MongoMigrableDefinition GetMigratableDefinition()
    {
        return new()
        {
            ConnectionString = ConnectionString,
            Database = new(Alias, DatabaseName),
            MirgrationStateCollectionName = "DATABASE_MIGRATIONS"
        };
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
}

[MongoMigration(PoeDbDatabaseSettings.Alias, 0, 1, Description = $"Add unique index {SkillNameIndexName}.")]
public sealed class PoeNinjaAddNameIndexMigration(IOptions<PoeDbDatabaseSettings> optionsAccessor) : IMongoMigration
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
