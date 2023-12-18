using System.Collections.Immutable;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;

namespace GemLevelProtScraper;

public sealed record ProfitRequest
{
    public required string GemNameWindcard { get; init; }
    public decimal? MinSellPriceChaos { get; init; }
    public decimal? MaxBuyPriceChaos { get; init; }
    public decimal? MinExperienceDelta { get; init; }
}

public sealed record ProfitResponse
{
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public required ProfitLevelResponse Min { get; init; }
    public required ProfitLevelResponse Max { get; init; }
}

public sealed record ProfitLevelResponse
{
    public required long Level { get; init; }
    public required long Quality { get; init; }
    public required decimal Price { get; init; }
    public required decimal Experience { get; init; }
    public required long ListingCount { get; init; }
}

public sealed class ProfitService(PoeDbRepository poeDbRepository, PoeNinjaRepository poeNinjaRepository)
{
    public async Task<ImmutableArray<ProfitResponse>> GetProfitAsync(ProfitRequest request, CancellationToken cancellationToken = default)
    {
        var gemPricesTask = poeNinjaRepository.GetByNameGlobAsync(request.GemNameWindcard, cancellationToken);
        var gemDataTask = poeDbRepository.GetByNameGlobAsync(request.GemNameWindcard, cancellationToken);
        var gemPrices = await gemPricesTask.ConfigureAwait(false);
        var gemData = await gemDataTask.ConfigureAwait(false);

        var eligiblePrices = gemPrices
            .Where(p => (request.MaxBuyPriceChaos is not { } maxBuy || p.ChaosValue <= maxBuy) && (request.MinSellPriceChaos is not { } minSell || p.ChaosValue >= minSell))
            .ToImmutableArray();
        if (eligiblePrices.IsDefaultOrEmpty || gemPrices.Count == 0)
        {
            return ImmutableArray<ProfitResponse>.Empty;
        }

        var eligibleGemsWithPrices = gemData.GroupJoin(
            eligiblePrices,
            d => d.Name.Name,
            p => p.Name,
            ComputeSkillGainMargin
        )
            .SelectTruthy(t => t);

        var responses = eligibleGemsWithPrices.Select(t => new ProfitResponse()
        {
            Name = t.Data.Name.Name,
            Icon = t.Max.Data.Icon ?? t.Min.Data.Icon,
            Min = FromPrice(t.Min.Data, t.Min.Exp),
            Max = FromPrice(t.Max.Data, t.Max.Exp)
        });

        var result = responses.ToImmutableArray();
        return result;

        static ProfitLevelResponse FromPrice(PoeNinjaApiGemPrice price, decimal exp) => new()
        {
            Experience = exp,
            Level = price.GemLevel,
            Quality = price.GemQuality,
            ListingCount = price.ListingCount,
            Price = price.ChaosValue
        };
        static (decimal Margin, (PoeNinjaApiGemPrice Data, decimal Exp) Min, (PoeNinjaApiGemPrice Data, decimal Exp) Max, PoeDbSkill Data)? ComputeSkillGainMargin(PoeDbSkill skill, IEnumerable<PoeNinjaApiGemPrice> prices)
        {
            var pricesWithExperience = prices
                .Where(p => !p.Corrupted && p.ListingCount >= 4)
                .OrderBy(p => p.ChaosValue)
                .Select(p => (
                    p,
                    skill.LevelEffects.SelectTruthy(l => l.Level < p.GemLevel ? l.Experience : null).Sum()
                ))
                .ToImmutableArray();
            if (pricesWithExperience.IsDefaultOrEmpty)
            {
                return null;
            }
            var (min, minExp) = pricesWithExperience.First();
            var (max, maxExp) = pricesWithExperience.Last();
            var deltaExp = maxExp - minExp;
            var deltaPrice = max.ChaosValue - min.ChaosValue;
            var margin = deltaExp == 0 ? 0 : deltaPrice * 1000000 / deltaExp;
            return (
                margin,
                (min, minExp),
                (max, maxExp),
                skill
            );
        }
    }
}
