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
                _prices.AsQueryable()
                    .Where(e
                        => e.League == leauge
                        && (containsName == null || e.Price.Name.Contains(containsName))
                    ),
                s => s.Name, e => e.Price.Name,
                (s, e) => new SkillGemPriced(
                    s,
                    e.Select(e => new SkillGemPrice
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
                    .ToImmutableArray()
                )
            )
            .ToCursorAsync(cancellationToken).ConfigureAwait(false);
        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var item in cursor.Current)
            {
                yield return item;
            }
        }
    }

}
