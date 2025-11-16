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
        field ??= PricesAscending.Where(x => !x.Corrupted).MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? MaxLevel => field ??= PricesAscending.Where(x => !x.Corrupted).MaxBy(x => x, LevelComparer);

    public SkillGemPrice? MinLevel20Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: false, GemQuality: 20 })
            .MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? MaxLevel20Quality =>
        field ??= PricesAscending.Where(x => x is { Corrupted: false, GemQuality: 20 }).MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedLevel =>
        field ??= MaxLevel is { } maxLevel
            ? PricesAscending.Where(x => x.Corrupted && x.GemLevel > maxLevel.GemLevel).MaxBy(x => x, LevelComparer)
            : null;

    public SkillGemPrice? CorruptedLevel20Quality =>
        field ??= MaxLevel is { } maxLevel
            ? PricesAscending
                .Where(x => x.Corrupted && x.GemLevel > maxLevel.GemLevel && x.GemQuality == 20)
                .MaxBy(x => x, LevelComparer)
            : null;

    public SkillGemPrice? CorruptedLevel23Quality =>
        field ??= MaxLevel is { } maxLevel
            ? PricesAscending
                .Where(x => x.Corrupted && x.GemLevel > maxLevel.GemLevel && x.GemQuality == 23)
                .MaxBy(x => x, LevelComparer)
            : null;

    public SkillGemPrice? Corrupted23QualityMaxLevel =>
        field ??= MaxLevel is { } maxLevel
            ? PricesAscending
                .Where(x => x.Corrupted && x.GemLevel == maxLevel.GemLevel && x.GemQuality == 23)
                .MaxBy(x => x, LevelComparer)
            : null;


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

public static class ProfitRecipeExtension
{
    public static IServiceCollection AddProfitRecipes(this IServiceCollection services)
    {
        AddRecipe(typeof(LevelSell));
        AddRecipe(typeof(LevelVendorQualityLevelSell));
        AddRecipe(typeof(LevelVendorQualitySell));
        AddRecipe(typeof(QualityLevelSell));
        AddRecipe(typeof(VendorBuyLevelSell));
        AddRecipe(typeof(VendorBuyLevelVendorQualityLevelSell));
        AddRecipe(typeof(VendorBuyLevelVendorQualitySell));
        AddRecipe(typeof(VendorBuyQualityLevelSell));
        return services;

        void AddRecipe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type t) =>
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IProfitRecipe), t, ServiceLifetime.Transient));
    }
}
