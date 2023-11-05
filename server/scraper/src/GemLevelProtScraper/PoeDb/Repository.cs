using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.PoeDb;

public sealed class PoeDbRepository(IOptions<PoeDbDatabaseSettings> settings, IMongoMigrationCompletion completion)
{
    private readonly IMongoCollection<PoeDbSkill> _skillCollection = new MongoClient(settings.Value.ConnectionString)
            .GetDatabase(settings.Value.DatabaseName)
            .GetCollection<PoeDbSkill>(settings.Value.SkillCollectionName);

    internal async Task<PoeDbSkill> AddOrUpdateAsync(PoeDbSkill newSkill, CancellationToken cancellationToken = default)
    {
        // _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _skillCollection.FindOneAndReplaceAsync(
            skill => skill.Name == newSkill.Name,
            newSkill,
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeDbSkill>> GetByNameAsync(string skillNameWildcard, CancellationToken cancellationToken = default)
    {
        // _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _skillCollection.Aggregate().Search(Builders<PoeDbSkill>.Search.Wildcard(s => s.Name.Name, skillNameWildcard)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
