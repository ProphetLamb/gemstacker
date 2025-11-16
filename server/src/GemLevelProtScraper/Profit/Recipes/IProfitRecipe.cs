using System.Diagnostics.CodeAnalysis;
using GemLevelProtScraper.Skills;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GemLevelProtScraper.Profit.Recipes;

public interface IProfitRecipe
{
    public string Name
    {
        get;
    }

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx);
}

public sealed class SkillProfitCalculationContext(
    ProfitRequest request,
    SkillGem skill,
    ExchangeRateCollection exchangeRates,
    IReadOnlyDictionary<string, double> experienceFactorAddQualityByName,
    IReadOnlyList<SkillGemPrice> pricesAscending
)
{
    public const double GainMarginFactor = 1000000;

    private static IComparer<SkillGemPrice> LevelComparer
    {
        get;
    } = Comparer<SkillGemPrice>.Create((x, y) =>
        {
            var gemLevel = x.GemLevel.CompareTo(y.GemLevel);
            if (gemLevel != 0)
            {
                return gemLevel;
            }

            return x.ChaosValue.CompareTo(y.ChaosValue);
            ;
        }
    );

    private static IComparer<SkillGemPrice> LevelDescComparer
    {
        get;
    } = Comparer<SkillGemPrice>.Create((x, y) =>
        {
            var gemLevel = -x.GemLevel.CompareTo(y.GemLevel);
            if (gemLevel != 0)
            {
                return gemLevel;
            }

            return x.ChaosValue.CompareTo(y.ChaosValue);
            ;
        }
    );

    public SkillGem Skill => skill;

    public IReadOnlyList<SkillGemPrice> PricesAscending => pricesAscending;

    public SkillGemPrice? MinLevel =>
        field ??= PricesAscending.Where(x => x is { Corrupted: false, GemLevel: 1 }).MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? CorruptedMinLevel =>
        field ??= PricesAscending.Where(x => x is { Corrupted: true, GemLevel: 1 }).MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? MaxLevel =>
        field ??= PricesAscending.Where(x => !x.Corrupted && x.GemLevel == Skill.MaxLevel).MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedMaxLevel =>
        field ??= PricesAscending.Where(x => x.Corrupted && x.GemLevel == Skill.MaxLevel).MaxBy(x => x, LevelComparer);

    public SkillGemPrice? MinLevel20Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: false, GemQuality: 20, GemLevel: 1 })
            .MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? MaxLevel20Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: false, GemQuality: 20 } && x.GemLevel == Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedAddLevel =>
        field ??= PricesAscending.Where(x => x.Corrupted && x.GemLevel > Skill.MaxLevel).MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedAddLevel23Quality =>
        field ??= PricesAscending
                .Where(x => x.Corrupted && x.GemLevel > Skill.MaxLevel && x.GemQuality == 23)
                .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? Corrupted23QualityMaxLevel =>
        field ??= PricesAscending
                .Where(x => x.Corrupted && x.GemLevel == Skill.MaxLevel && x.GemQuality == 23)
                .MaxBy(x => x, LevelComparer);
    public (SkillGemPrice Min, SkillGemPrice Max)? MinAndMaxMaybeCorrupted()
    {
        return (MaxLevel ?? CorruptedMaxLevel) is { } max
               && (MinLevel ?? CorruptedMinLevel) is { } min
               && min.GemLevel < max.GemLevel
               && min.Corrupted == max.Corrupted
            ? (min, max)
            : null;
    }

    public ProfitRequest Request => request;

    public double? ExchangeRate(CurrencyTypeName name)
    {
        return exchangeRates.TryGetValue(request.League, name, out var rate) ? rate.ChaosEquivalent : null;
    }

    public double GainMargin(double earnings, double experience)
    {
        return experience == 0 ? 0 : earnings * GainMarginFactor / experience;
    }

    public long GemQuality(SkillGemPrice price)
    {
        return price.GemQuality + request.AddedQuality;
    }

    public double ExperienceFactor(long quality)
    {
        if (experienceFactorAddQualityByName.TryGetValue(skill.BaseType, out var addFactorPerQuality))
        {
            return 1.0 / (1.0 + (addFactorPerQuality * quality));
        }

        return 1.0;
    }

    public ProfitLevelResponse ToProfitLevelResponse(SkillGemPrice price, double experience)
    {
        return new()
        {
            Price = price.ChaosValue,
            Level = price.GemLevel,
            Quality = price.GemQuality,
            Corrupted = price.Corrupted,
            Experience = experience,
            ListingCount = price.ListingCount
        };
    }
}

public static class GemCorruptionHelper
{
    public static double Attempts(int successfulOutcomes)
    {
        return Math.Log(successfulOutcomes / 4.0) / Math.Log(.6); // 60% success needed
    }

    public static double AttemptsForOneInFour => Attempts(1);
    public static double AttemptsForThreeInFour => Attempts(3);
}

public static class ProfitRecipeExtension
{
    public static IServiceCollection AddProfitRecipes(this IServiceCollection services)
    {
        AddRecipe(typeof(LevelCorruptAddLevelSell));
        AddRecipe(typeof(LevelSell));
        AddRecipe(typeof(LevelVendorQualityLevelSell));
        AddRecipe(typeof(LevelVendorQualitySell));
        AddRecipe(typeof(QualityLevelSell));
        AddRecipe(typeof(VendorBuyCorruptLevelSellVaal));
        AddRecipe(typeof(VendorBuyLevelSell));
        AddRecipe(typeof(VendorBuyLevelVendorQualityLevelSell));
        AddRecipe(typeof(VendorBuyLevelVendorQualitySell));
        AddRecipe(typeof(VendorBuyQualityLevelSell));
        return services;

        void AddRecipe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type t) =>
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IProfitRecipe), t, ServiceLifetime.Transient));
    }
}
