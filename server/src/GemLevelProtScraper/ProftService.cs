using System.Collections.Immutable;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;

namespace GemLevelProtScraper;

public sealed record ProfitRequest
{
    public required LeagueMode League { get; init; }
    public required string GemNameWindcard { get; init; }
    public double? MinSellPriceChaos { get; init; }
    public double? MaxBuyPriceChaos { get; init; }
    public double? MinExperienceDelta { get; init; }
    public int MinimumListingCount { get; init; }
}

public sealed record ProfitResponse
{
    public required string Name { get; init; }
    public required string? Icon { get; init; }
    public required GemColor Color { get; init; }
    public required ProfitLevelResponse Min { get; init; }
    public required ProfitLevelResponse Max { get; init; }
    public required double GainMargin { get; init; }
    public required string Type { get; init; }
    public required string? Discriminator { get; init; }
    public required string ForeignInfoUrl { get; init; }
}

public sealed record ProfitLevelResponse
{
    public required long Level { get; init; }
    public required long Quality { get; init; }
    public required double Price { get; init; }
    public required double Experience { get; init; }
    public required long ListingCount { get; init; }
}


public enum GemColor
{
    White = 0,
    Blue = 1,
    Green = 2,
    Red = 3,
}

internal readonly record struct ProfitMargin(double Margin, (PoeNinjaApiGemPrice Data, double Exp) Min, (PoeNinjaApiGemPrice Data, double Exp) Max, PoeDbSkill Data);

public sealed class ProfitService(PoeDbRepository poeDbRepository, PoeNinjaRepository poeNinjaRepository)
{
    public async Task<ImmutableArray<ProfitResponse>> GetProfitAsync(ProfitRequest request, CancellationToken cancellationToken = default)
    {
        var gemPrices = await poeNinjaRepository.GetByNameGlobAsync(request.League, request.GemNameWindcard, cancellationToken).ConfigureAwait(false);

        var eligiblePrices = gemPrices
            .Where(p => (request.MaxBuyPriceChaos is not { } maxBuy || p.ChaosValue <= maxBuy) && (request.MinSellPriceChaos is not { } minSell || p.ChaosValue >= minSell))
            .Where(p => !p.Corrupted && p.ListingCount >= request.MinimumListingCount)
            .ToImmutableArray();
        if (eligiblePrices.IsDefaultOrEmpty || gemPrices.Count == 0)
        {
            return ImmutableArray<ProfitResponse>.Empty;
        }

        var gemData = await poeDbRepository.GetByNameListAsync(eligiblePrices.Select(p => p.Name), cancellationToken).ConfigureAwait(false);

        var eligibleGemsWithPrices = gemData
            .AsParallel()
            .GroupJoin(
                eligiblePrices.AsParallel(),
                d => d.Name.Id,
                p => p.Name,
                ComputeSkillProfitMargin
            )
            .SelectTruthy(t => t)
            .OrderByDescending(t => t.Margin);

        var responses = eligibleGemsWithPrices.Select(t => new ProfitResponse()
        {
            Name = t.Data.Name.Id,
            Icon = t.Max.Data.Icon ?? t.Min.Data.Icon ?? t.Data.IconUrl,
            Color = (GemColor)(int)t.Data.Name.Color,
            Min = FromPrice(t.Min.Data, t.Min.Exp),
            Max = FromPrice(t.Max.Data, t.Max.Exp),
            GainMargin = t.Margin,
            Type = t.Data.Stats.BaseType,
            Discriminator = t.Data.Discriminator,
            ForeignInfoUrl = $"https://poedb.tw{t.Data.Name.RelativeUrl}"
        });

        var result = responses.ToImmutableArray();
        return result;

        static ProfitLevelResponse FromPrice(PoeNinjaApiGemPrice price, double exp) => new()
        {
            Experience = exp,
            Level = price.GemLevel,
            Quality = price.GemQuality,
            ListingCount = price.ListingCount,
            Price = price.ChaosValue
        };

        ProfitMargin? ComputeSkillProfitMargin(PoeDbSkill skill, IEnumerable<PoeNinjaApiGemPrice> prices)
        {
            var pricesWithExperience = prices
                .Select(p => (
                    Price: p,
                    Exp: skill.LevelEffects.SelectTruthy(l => l.Level < p.GemLevel ? l.Experience : null).Sum()
                ))
                .OrderBy(t => t.Price.ChaosValue)
                .TryGetFirstAndLast(out var min, out var max);
            var (minPrice, minExp) = min;
            var (maxPrice, maxExp) = max;
            var requiredExp = maxExp - minExp;

            if (requiredExp < request.MinExperienceDelta)
            {
                return null;
            }

            var levelEarning = maxPrice.ChaosValue - minPrice.ChaosValue;
            // penalize quality upgrades
            var qualityCost = Math.Max(0, maxPrice.GemQuality - minPrice.GemQuality);

            var adjustedEarnings = Math.Max(0, levelEarning - qualityCost);

            var margin = requiredExp == 0 ? 0 : adjustedEarnings * 1000000 / requiredExp;
            return new(
                margin,
                (minPrice, minExp),
                (maxPrice, maxExp),
                skill
            );
        }
    }
}
