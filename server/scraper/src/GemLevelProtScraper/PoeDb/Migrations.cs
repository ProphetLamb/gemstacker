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

    PoeDbDatabaseSettings IOptions<PoeDbDatabaseSettings>.Value => this;

    public MongoMigrableDefinition GetMigratableDefinition()
    {
        return new()
        {
            ConnectionString = ConnectionString,
            Database = new("PoeDb", DatabaseName),
            MirgrationStateCollectionName = "DATABASE_MIGRATIONS"
        };
    }
}

[MongoMigration("PoeDb", 0, 1, Description = $"Add windcard index {NameWindcardIndexName}")]
public sealed class PoeNinjaAddNameWildcardIndexMigration(IOptions<PoeDbDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string NameWindcardIndexName = "NameWildcard";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var gemPriceCollection = GetSkillCollection(optionsAccessor, database);
        await gemPriceCollection.Indexes.DropOneAsync(NameWindcardIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var gemPriceCollection = GetSkillCollection(optionsAccessor, database);
        IndexKeysDefinitionBuilder<PoeDbSkill> builder = new();
        var wildcardIndex = builder.Wildcard(p => p.Name.Name);
        CreateIndexModel<PoeDbSkill> model = new(wildcardIndex, new()
        {
            Name = NameWindcardIndexName
        });
        _ = await gemPriceCollection.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }

    private static IMongoCollection<PoeDbSkill> GetSkillCollection(IOptions<PoeDbDatabaseSettings> optionsAccessor, IMongoDatabase database)
    {
        return database.GetCollection<PoeDbSkill>(optionsAccessor.Value.SkillCollectionName);
    }
}
