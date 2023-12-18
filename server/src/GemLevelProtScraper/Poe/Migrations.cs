using Microsoft.Extensions.Options;
using MongoDB.Migration;
using MongoDB.Migration.Core;

namespace GemLevelProtScraper.Poe;

public sealed record PoeDatabaseSettings : IOptions<PoeDatabaseSettings>, IMongoMigratable
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public required string LeaguesCollectionName { get; init; }

    PoeDatabaseSettings IOptions<PoeDatabaseSettings>.Value => this;

    public MongoMigrableDefinition GetMigratableDefinition()
    {
        return new()
        {
            ConnectionString = ConnectionString,
            Database = new("Poe", DatabaseName),
            MirgrationStateCollectionName = "DATABASE_MIGRATIONS"
        };
    }
}
