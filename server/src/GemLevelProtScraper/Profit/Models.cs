using GemLevelProtScraper.Poe;
using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit;


public sealed record ProfitRequest
{
    public required LeagueMode League { get; init; }
    public required string? GemNameWildcard { get; init; }
    public long AddedQuality { get; init; }
    public double? MinSellPriceChaos { get; init; }
    public double? MaxBuyPriceChaos { get; init; }
    public double? MinExperienceDelta { get; init; }
    public long MinimumListingCount { get; init; }
    public IReadOnlySet<string>? DisallowedRecipes { get; init; }
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
    public required string PreferredRecipe { get; init; }
    public required IReadOnlyDictionary<string, ProfitMargin> Recipes { get; init; }
}

public sealed record ProfitLevelResponse
{
    public required long Level { get; init; }
    public required long Quality { get; init; }
    public required bool Corrupted { get; init; }
    public required double Price { get; init; }
    public required double Experience { get; init; }
    public required long ListingCount { get; init; }
}

public sealed record ProfitMargin
{
    public required ProfitLevelResponse Buy { get; init; }
    public required ProfitLevelResponse Sell { get; init; }
    public required double AdjustedEarnings { get; init; }
    public required double ExperienceDelta { get; init; }
    public required double GainMargin { get; init; }
    public IReadOnlyList<ProbabilisticProfitMargin>? Probabilistic { get; init; }
}

public sealed record ProbabilisticProfitMargin
{
    public required double Chance { get; init; }
    public required double Earnings { get; init; }
    public string? Label { get; init; }
}


public enum GemColor
{
    White = 0,
    Blue = 1,
    Green = 2,
    Red = 3,
}

internal readonly record struct PriceWithExp(ProfitLevelResponse Data, double Exp);
internal readonly record struct PriceDelta(IReadOnlyDictionary<string, ProfitMargin> ProfitMargins, PriceWithExp Min, PriceWithExp Max, string PreferredRecipe, double MaxGainMargin, SkillGem Data);
