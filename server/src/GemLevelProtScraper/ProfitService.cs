using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.Skills;
using Microsoft.Extensions.Options;

namespace GemLevelProtScraper;

public sealed class ProfitServiceOptions : IOptions<ProfitServiceOptions>
{
    public required Dictionary<string, double> SpecialExperienceFactorPerQualityGams { get; init; }

    ProfitServiceOptions IOptions<ProfitServiceOptions>.Value => this;
}

public sealed record ProfitRequest
{
    public required LeagueMode League { get; init; }
    public required string? GemNameWildcard { get; init; }
    public long AddedQuality { get; init; }
    public double? MinSellPriceChaos { get; init; }
    public double? MaxBuyPriceChaos { get; init; }
    public double? MinExperienceDelta { get; init; }
    public long MinimumListingCount { get; init; }
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
    public required double PriceInDivines { get; init; }
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

public sealed class ProfitService(SkillGemRepository repository, ExchangeRateProvider exchangeRateProvider, IOptions<ProfitServiceOptions> options)
{
    public async IAsyncEnumerable<ProfitResponse> GetProfitAsync(ProfitRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var valueSpecialExperienceFactorPerQualityGams = options.Value.SpecialExperienceFactorPerQualityGams;
        var exchangeRates = await exchangeRateProvider.GetExchangeRatesAsync(cancellationToken).ConfigureAwait(false);
        var pricedGems = repository.GetPricedGemsAsync(request.League, request.GemNameWildcard, cancellationToken);
        var eligiblePricedGems = pricedGems
            .SelectTruthy(g
                => new ProftMarginCalculator(
                    request,
                    g.Skill,
                    exchangeRates,
                    valueSpecialExperienceFactorPerQualityGams
                ).ComputeProfitMargin(g.Prices) is { } delta
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
            Price = price.ChaosValue,
            PriceInDivines = price.DivineValue
        };
    }
}

internal readonly struct ProftMarginCalculator(ProfitRequest request, SkillGem skill, ExchangeRateCollection exchangeRates, IReadOnlyDictionary<string, double> experienceFactorAddQualityByName)
{
    public const double GainMarginFactor = 1000000;

    private readonly double _chaosToChisels = exchangeRates.TryGetValue(request.League, CurrencyTypeName.CartographersChisel, out var rate) ? rate.ChaosEquivalent : 1;

    private static double ComputeGainMargin(double earnings, double experience)
    {
        return experience == 0 ? 0 : earnings * GainMarginFactor / experience;
    }

    private long GemQuality(SkillGemPrice price)
    {
        return price.GemQuality + request.AddedQuality;
    }

    private double ExperienceFactor(long quality)
    {
        if (experienceFactorAddQualityByName.TryGetValue(skill.BaseType, out var addFactorPerQuality))
        {
            return 1.0 / (1.0 + (addFactorPerQuality * quality));
        }

        return 1.0;
    }

    public PriceDelta? ComputeProfitMargin(IEnumerable<SkillGemPrice> prices)
    {
        var profitRequest = request;
        var skillGem = skill;
        var pricesOrdered = prices
            .Where(p => !p.Corrupted && p.ListingCount >= profitRequest.MinimumListingCount)
            .Where(p
                => (profitRequest.MaxBuyPriceChaos is not { } maxBuy || p.ChaosValue <= maxBuy)
                && (profitRequest.MinSellPriceChaos is not { } minSell || p.ChaosValue >= minSell)
            )
            .ToArray();
        // order by ChaosValue descending
        pricesOrdered.AsSpan().Sort((lhs, rhs) => rhs.ChaosValue.CompareTo(lhs.ChaosValue));

        var minPrice = pricesOrdered.LastOrDefault(p => p.GemLevel == 1);
        var maxPrice = pricesOrdered.FirstOrDefault(p => p.GemLevel == skillGem.MaxLevel);
        if (minPrice is null || maxPrice is null)
        {
            return null;
        }

        var requiredExp = skillGem.SumExperience;

        if (requiredExp < profitRequest.MinExperienceDelta)
        {
            return null;
        }

        return new(
            ComputeQualityThenLevel(requiredExp, minPrice, maxPrice),
            ComputeLevelVendorLevel(requiredExp, minPrice, maxPrice),
            new(minPrice, 0),
            new(maxPrice, requiredExp),
            skillGem
        );
    }

    private ProfitMargin ComputeQualityThenLevel(double experineceDelta, SkillGemPrice min, SkillGemPrice max)
    {
        // quality the gem then level it
        var levelEarning = max.ChaosValue - min.ChaosValue;
        var qualitySpent = Math.Max(0, max.GemQuality - min.GemQuality);
        var qualityCost = qualitySpent * _chaosToChisels;

        var experienceFactor = ExperienceFactor(GemQuality(max));

        var adjustedEarnings = levelEarning - qualityCost;

        var gainMargin = ComputeGainMargin(adjustedEarnings, experineceDelta * experienceFactor);

        return new()
        {
            GainMargin = gainMargin,
            ExperienceDelta = experineceDelta,
            AdjustedEarnings = adjustedEarnings,
            QualitySpent = qualitySpent
        };
    }

    private ProfitMargin ComputeLevelVendorLevel(double experienceDelta, SkillGemPrice min, SkillGemPrice max)
    {
        // level the gem, vendor it with 1x Gem Cutter, level it again
        var vendorRequired = max.GemQuality > min.GemQuality;
        var levelEarning = max.ChaosValue - min.ChaosValue;
        var qualitySpent = vendorRequired ? 1 : 0;
        var qualityCost = qualitySpent * _chaosToChisels;

        var experienceFactor = ExperienceFactor(GemQuality(min)) + (vendorRequired ? ExperienceFactor(GemQuality(max)) : 0);

        var adjustedEarnings = levelEarning - qualityCost;

        var gainMargin = ComputeGainMargin(adjustedEarnings, experienceDelta * experienceFactor);

        return new()
        {
            GainMargin = gainMargin,
            ExperienceDelta = experienceDelta,
            AdjustedEarnings = adjustedEarnings,
            QualitySpent = qualitySpent
        };
    }
}
