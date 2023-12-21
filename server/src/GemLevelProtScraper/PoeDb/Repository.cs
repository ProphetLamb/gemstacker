using DotNet.Globbing;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.PoeDb;

public sealed class PoeDbRepository(IOptions<PoeDbDatabaseSettings> settings, IMongoMigrationCompletion completion, ISystemClock clock)
{
    private readonly IMongoCollection<PoeDbSkillEnvalope> _skillCollection = new MongoClient(settings.Value.ConnectionString)
            .GetDatabase(settings.Value.DatabaseName)
            .GetCollection<PoeDbSkillEnvalope>(settings.Value.SkillCollectionName);

    internal async Task AddOrUpdateAsync(PoeDbSkill newSkill, CancellationToken cancellationToken = default)
    {
        // _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        await _skillCollection.FindOneAndReplaceAsync(
            e => e.Skill.Name.RelativeUrl == newSkill.Name.RelativeUrl,
            new(clock.UtcNow.UtcDateTime, newSkill),
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeDbSkill>> GetByNameAsync(string? skillName, CancellationToken cancellationToken = default)
    {
        // _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(skillName))
        {
            return await _skillCollection
                .Find(FilterDefinition<PoeDbSkillEnvalope>.Empty)
                .Project(e => e.Skill)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        return await _skillCollection
            .Find(e => e.Skill.Name.Id == skillName)
            .Project(e => e.Skill)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeDbSkill>> GetByNameListAsync(IEnumerable<string> nameList, CancellationToken cancellationToken)
    {
        var nameSet = nameList.ToHashSet();
        return await _skillCollection
            .Find(e => nameSet.Contains(e.Skill.Name.Id))
            .Project(e => e.Skill)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
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
        return await _skillCollection
            .Find(FilterDefinition<PoeDbSkillEnvalope>.Empty)
            .Project(e => e.Skill.Name.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
