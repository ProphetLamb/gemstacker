using System.Runtime.CompilerServices;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeNinja;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Migration;

namespace GemLevelProtScraper.Skills;

public sealed class SkillGemRepository(IOptions<PoeDatabaseSettings> options, IMongoMigrationCompletion completion)
{
    private readonly IMongoCollection<SkillGem> _skills = options.Value.GetSkillGemCollection();
    private readonly IMongoCollection<PoeNinjaApiGemPriceEnvalope> _prices = options.Value.GetPoeNinjaGemCollection();

    public async ValueTask<List<SkillGemPriced>> GetPricedGemsAsync(LeagueMode leauge, string? containsName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(options.Value, cancellationToken).ConfigureAwait(false);
        return await _skills.AsQueryable()
            .GroupJoin(
                _prices.AsQueryable(),
                s => s.Name, e => e.Price.Name,
                (s, e) => new
                {
                    Skill = s,
                    Prices = e.Where(e => e.League == leauge)
                }
            )
            .Where(r
                => (containsName == null || r.Skill.Name.Contains(containsName))
                && r.Prices.Any())
            .Select(r
                => new SkillGemPriced(r.Skill, r.Prices
                        .Select(e => new SkillGemPrice
                            (
                                e.Price.Icon,
                                e.Price.Corrupted,
                                e.Price.GemLevel,
                                e.Price.GemQuality,
                                e.Price.ChaosValue,
                                e.Price.DivineValue,
                                e.Price.ListingCount
                            )
                        ).ToList())
            )
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

}
