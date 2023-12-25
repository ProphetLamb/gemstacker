using System.Runtime.CompilerServices;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper;

public sealed record ProfitRequest
{
    public required LeagueMode League { get; init; }
    public required string? GemNameWindcard { get; init; }
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

internal readonly record struct ProfitMargin(double Margin, (SkillGemPrice Data, double Exp) Min, (SkillGemPrice Data, double Exp) Max, SkillGem Data);

public sealed class ProfitService(SkillGemRepository repository)
{
    public async IAsyncEnumerable<ProfitResponse> GetProfitAsync(ProfitRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pricedGems = repository.GetPricedGemsAsync(request.League, request.GemNameWindcard, cancellationToken);

        var eligiblePricedGems = pricedGems
            .SelectTruthy(g
                => ComputeSkillProfitMargin(
                        g.Skill,
                        g.Prices
                            .Where(p => !p.Corrupted && p.ListingCount >= request.MinimumListingCount)
                            .Where(p => (request.MaxBuyPriceChaos is not { } maxBuy || p.ChaosValue <= maxBuy) && (request.MinSellPriceChaos is not { } minSell || p.ChaosValue >= minSell))
                    ) is { } gain
                ? new { g.Skill, Gain = gain }
                : null
            )
            .OrderByDescending(g => g.Gain.Margin)
            ;

        var result = eligiblePricedGems
            .Select(g
                => new ProfitResponse
                {
                    Name = g.Skill.Name,
                    Color = g.Skill.Color,
                    Discriminator = g.Skill.Discriminator,
                    Type = g.Skill.BaseType,
                    ForeignInfoUrl = $"https://poedb.tw{g.Skill.RelativeUrl}",
                    GainMargin = g.Gain.Margin,
                    Icon = g.Skill.IconUrl,
                    Max = FromPrice(g.Gain.Max.Data, g.Gain.Max.Exp),
                    Min = FromPrice(g.Gain.Min.Data, g.Gain.Min.Exp)
                }
            );

        var en = result.GetAsyncEnumerator(cancellationToken);
        while (await en.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return en.Current;
        }

        static ProfitLevelResponse FromPrice(SkillGemPrice price, double exp) => new()
        {
            Experience = exp,
            Level = price.GemLevel,
            Quality = price.GemQuality,
            ListingCount = price.ListingCount,
            Price = price.ChaosValue
        };

        ProfitMargin? ComputeSkillProfitMargin(SkillGem skill, IEnumerable<SkillGemPrice> prices)
        {
            var pricesOrdered = prices.ToArray();
            // order by ChaosValue descending
            pricesOrdered.AsSpan().Sort((lhs, rhs) => rhs.ChaosValue.CompareTo(lhs.ChaosValue));

            var minPrice = pricesOrdered.Where(p => p.GemLevel == 1).LastOrDefault();
            var maxPrice = pricesOrdered.Where(p => p.GemLevel == skill.MaxLevel).FirstOrDefault();
            if (minPrice is null || maxPrice is null)
            {
                return null;
            }

            var requiredExp = skill.SumExperience;

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
                (minPrice, 0),
                (maxPrice, requiredExp),
                skill
            );
        }
    }
}
