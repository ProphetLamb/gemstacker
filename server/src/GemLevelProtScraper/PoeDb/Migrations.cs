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

    internal IMongoCollection<PoeDbSkillEnvalope> GetSkillCollection()
    {
        MongoClient client = new(ConnectionString);
        var database = client.GetDatabase(DatabaseName);
        return GetSkillCollection(database);
    }

    private IMongoCollection<PoeDbSkillEnvalope> GetSkillCollection(IMongoDatabase database)
    {
        return database.GetCollection<PoeDbSkillEnvalope>(SkillCollectionName);
    }
}
