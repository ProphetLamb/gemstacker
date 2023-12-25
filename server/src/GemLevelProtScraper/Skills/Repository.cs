using System.Collections.Immutable;
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
    private readonly IMongoCollection<PoeNinjaApiGemPriceEnvalope> _prices = options.Value.GetPoeNinjaPriceCollection();

    public async IAsyncEnumerable<SkillGemPriced> GetPricedGemsAsync(LeagueMode leauge, string? containsName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = await completion.WaitAsync(options.Value, cancellationToken).ConfigureAwait(false);
        var cursor = await _skills.AsQueryable()
            .Where(s => containsName == null || s.Name.Contains(containsName))
            .GroupJoin(
                _prices.AsQueryable(),
                s => s.Name, e => e.Price.Name,
                (s, e) => new
                {
                    Skill = s,
                    Prices = e
                }
            )
            .Select(r
                => new
                {
                    r.Skill,
                    Prices = r.Prices
                        .Where(e => e.League == leauge)
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
                        )
                        .ToArray()
                }
            )
            .Where(r => r.Prices.Length > 0)
            .ToCursorAsync(cancellationToken).ConfigureAwait(false);
        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var item in cursor.Current)
            {
                var prices = item.Prices;
                var pricesArr = Unsafe.As<SkillGemPrice[], ImmutableArray<SkillGemPrice>>(ref prices);
                yield return new(item.Skill, pricesArr);
            }
        }
    }

}
