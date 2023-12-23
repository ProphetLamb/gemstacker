using DotNet.Globbing;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;

namespace GemLevelProtScraper.PoeDb;

public sealed class PoeDbRepository(IOptions<PoeDatabaseSettings> settings, IMongoMigrationCompletion completion, ISystemClock clock)
{
    private readonly IMongoCollection<PoeDbSkillEnvalope> _skillCollection = settings.Value.GetPoeDbSkillCollection();

    internal async Task AddOrUpdateAsync(PoeDbSkill newSkill, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        _ = await _skillCollection.FindOneAndReplaceAsync(
            e => e.Skill.Name.Id == newSkill.Name.Id,
            new(clock.UtcNow.UtcDateTime, newSkill),
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal async Task<long> RemoveOlderThanAsync(DateTimeOffset oldestTimestamp, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var utcTimestamp = oldestTimestamp.UtcDateTime;
        var result = await _skillCollection.DeleteManyAsync(
            e => e.UtcTimestamp < utcTimestamp,
            cancellationToken
        ).ConfigureAwait(false);
        return result.IsAcknowledged ? result.DeletedCount : -1;
    }

    internal async Task<IReadOnlyList<PoeDbSkill>> GetByNameAsync(string? skillName, CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
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
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        var nameSet = nameList.ToHashSet();
        return await _skillCollection
            .Find(e => nameSet.Contains(e.Skill.Name.Id))
            .Project(e => e.Skill)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<PoeDbSkill>> GetByNameGlobAsync(string? nameWildcard, CancellationToken cancellationToken)
    {
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
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
        _ = await completion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        return await _skillCollection
            .Find(FilterDefinition<PoeDbSkillEnvalope>.Empty)
            .Project(e => e.Skill.Name.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
