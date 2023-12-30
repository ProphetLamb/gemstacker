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
    public required ProfitResponseRecipies Recipies { get; init; }
}

public sealed record ProfitResponseRecipies
{
    public required ProfitMargin QualityThenLevel { get; init; }
    public required ProfitMargin LevelVendorLevel { get; init; }
}

public sealed record ProfitLevelResponse
{
    public required long Level { get; init; }
    public required long Quality { get; init; }
    public required double Price { get; init; }
    public required double Experience { get; init; }
    public required long ListingCount { get; init; }
}

public sealed record ProfitMargin
{
    public required double AdjustedEarnings { get; init; }
    public required double ExperienceDelta { get; init; }
    public required double GainMargin { get; init; }
    public required double QualitySpent { get; init; }
}


public enum GemColor
{
    White = 0,
    Blue = 1,
    Green = 2,
    Red = 3,
}

internal readonly record struct PriceWithExp(SkillGemPrice Data, double Exp);
internal readonly record struct PriceDelta(ProfitMargin QualityThenLevel, ProfitMargin LevelVendorLevel, PriceWithExp Min, PriceWithExp Max, SkillGem Data);

public sealed class ProfitService(SkillGemRepository repository)
{
    public const double GainMarginFactor = 1000000;

    public async IAsyncEnumerable<ProfitResponse> GetProfitAsync(ProfitRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pricedGems = repository.GetPricedGemsAsync(request.League, request.GemNameWindcard, cancellationToken);

        var eligiblePricedGems = pricedGems
            .SelectTruthy(g
                => ComputeSkillProfitMargin(
                        g.Skill,
                        g.Prices
                            .Where(p => !p.Corrupted && p.ListingCount >= request.MinimumListingCount)
                            .Where(p
                                => (request.MaxBuyPriceChaos is not { } maxBuy || p.ChaosValue <= maxBuy)
                                && (request.MinSellPriceChaos is not { } minSell || p.ChaosValue >= minSell)
                            )
                    ) is { } delta
                ? new { g.Skill, Delta = delta }
                : null
            )
            .OrderByDescending(g => Math.Max(g.Delta.LevelVendorLevel.GainMargin, g.Delta.QualityThenLevel.GainMargin))
            ;

        var result = eligiblePricedGems
            .Select(g
                =>
            {
                ProfitResponseRecipies recipies = new()
                {
                    LevelVendorLevel = g.Delta.LevelVendorLevel,
                    QualityThenLevel = g.Delta.QualityThenLevel,
                };
                var gainMargin = Math.Max(g.Delta.LevelVendorLevel.GainMargin, g.Delta.QualityThenLevel.GainMargin);
                return new ProfitResponse
                {
                    Name = g.Skill.Name,
                    Color = g.Skill.Color,
                    Discriminator = g.Skill.Discriminator,
                    Type = g.Skill.BaseType,
                    ForeignInfoUrl = $"https://poedb.tw{g.Skill.RelativeUrl}",
                    Recipies = recipies,
                    GainMargin = gainMargin,
                    Icon = g.Delta.Min.Data.Icon ?? g.Delta.Max.Data.Icon ?? g.Skill.IconUrl,
                    Max = FromPrice(g.Delta.Max.Data, g.Delta.Max.Exp),
                    Min = FromPrice(g.Delta.Min.Data, g.Delta.Min.Exp)
                };
            });

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

        static double ComputeGainMargin(double earnings, double experience) => experience == 0 ? 0 : earnings * GainMarginFactor / experience;

        PriceDelta? ComputeSkillProfitMargin(SkillGem skill, IEnumerable<SkillGemPrice> prices)
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

            return new(
                ComputeQualityThenLevel(requiredExp, minPrice, maxPrice),
                ComputeLevelVendorLevel(requiredExp, minPrice, maxPrice),
                new(minPrice, 0),
                new(maxPrice, requiredExp),
                skill
            );
        }

        static ProfitMargin ComputeQualityThenLevel(double experineceDelta, SkillGemPrice min, SkillGemPrice max)
        {
            // quality the gem then level it
            var levelEarning = max.ChaosValue - min.ChaosValue;
            var qualitySpent = Math.Max(0, max.GemQuality - min.GemQuality);
            var qualityCost = qualitySpent; // todo calcualte price

            var adjustedEarnings = levelEarning - qualityCost;

            var gainMargin = experineceDelta == 0 ? 0 : adjustedEarnings * GainMarginFactor / experineceDelta;

            return new()
            {
                GainMargin = gainMargin,
                ExperienceDelta = experineceDelta,
                AdjustedEarnings = adjustedEarnings,
                QualitySpent = qualitySpent
            };
        }

        static ProfitMargin ComputeLevelVendorLevel(double experineceDelta, SkillGemPrice min, SkillGemPrice max)
        {
            // level the gem, vendor it with 1x Gem Cutter, level it again
            var levelEarning = max.ChaosValue - min.ChaosValue;
            var qualitySpent = max.GemQuality <= min.GemQuality ? 0 : 1;
            var qualityCost = qualitySpent; // todo calcualte price

            var experineceFactor = qualitySpent == 0 ? 1 : 2;

            var adjustedEarnings = levelEarning - qualityCost;

            var gainMargin = ComputeGainMargin(adjustedEarnings, experineceDelta * experineceFactor);

            return new()
            {
                GainMargin = gainMargin,
                ExperienceDelta = experineceDelta,
                AdjustedEarnings = adjustedEarnings,
                QualitySpent = qualitySpent
            };
        }
    }
}
