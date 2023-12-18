using DotNet.Globbing;
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
            skill => skill.Name.RelativeUrl == newSkill.Name.RelativeUrl,
            newSkill,
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeDbSkill>> GetByNameAsync(string? skillName, CancellationToken cancellationToken = default)
    {
        // _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(skillName))
        {
            return await _skillCollection.Find(FilterDefinition<PoeDbSkill>.Empty).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        return await _skillCollection.Find(s => s.Name.Name == skillName).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeDbSkill>> GetByNameListAsync(IEnumerable<string> nameList, CancellationToken cancellationToken)
    {
        var nameSet = nameList.ToHashSet();
        return await _skillCollection.Find(s => nameSet.Contains(s.Name.Name)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeDbSkill>> GetByNameGlobAsync(string? nameWildcard, CancellationToken cancellationToken)
    {
        if (nameWildcard == "*")
        {
            nameWildcard = null;
        }
        if (nameWildcard is null || !nameWildcard.ContainsGlobChars())
        {
            return await GetByNameAsync(nameWildcard, cancellationToken).ConfigureAwait(false);
        }
        var names = await ListNamesAsync(cancellationToken).ConfigureAwait(false);
        var nameGlob = Glob.Parse(nameWildcard);
        var validNamed = names.Where(nameGlob.IsMatch).Distinct();
        var dataList = await GetByNameListAsync(validNamed, cancellationToken).ConfigureAwait(false);
        return dataList;
    }

    internal async Task<IReadOnlyList<string>> ListNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _skillCollection.Find(s => true).Project(s => s.Name.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
