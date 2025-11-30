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
    IReadOnlySet<string> vendorRestrictedGems,
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
            return gemLevel != 0 ? gemLevel : x.ChaosValue.CompareTo(y.ChaosValue);
        }
    );

    private static IComparer<SkillGemPrice> LevelDescComparer
    {
        get;
    } = Comparer<SkillGemPrice>.Create((x, y) =>
        {
            var gemLevel = -x.GemLevel.CompareTo(y.GemLevel);
            return gemLevel != 0 ? gemLevel : x.ChaosValue.CompareTo(y.ChaosValue);
        }
    );

    public SkillGem Skill => skill;

    public IReadOnlyList<SkillGemPrice> PricesAscending => pricesAscending;

    public SkillGemPrice? MinLevel =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: false, GemQuality: 0, GemLevel: 1 })
            .MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? CorruptedMinLevel =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 0, GemLevel: 1 })
            .MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? MinLevel20Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: false, GemQuality: 20, GemLevel: 1 })
            .MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? CorruptedMinLevel20Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 20, GemLevel: 1 })
            .MaxBy(x => x, LevelDescComparer);

    public SkillGemPrice? MaxLevel =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 0 } && x.GemLevel == Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedMaxLevel =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 0 } && x.GemLevel == Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? MaxLevel20Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: false, GemQuality: 20 } && x.GemLevel == Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedMaxLevel20Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 20 } && x.GemLevel == Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedMaxLevel23Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 23 } && x.GemLevel == Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedAddLevel =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 0 } && x.GemLevel > Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedAddLevel20Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 20 } && x.GemLevel > Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

    public SkillGemPrice? CorruptedAddLevel23Quality =>
        field ??= PricesAscending
            .Where(x => x is { Corrupted: true, GemQuality: 23 } && x.GemLevel > Skill.MaxLevel)
            .MaxBy(x => x, LevelComparer);

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

    public double RecipeCost(IReadOnlyDictionary<string, double> recipeCost)
    {
        return recipeCost.Sum(x => (ExchangeRate(new(x.Key)) ?? 0) * x.Value);
    }

    public double ProbabilisticEarnings(IReadOnlyList<ProbabilisticProfitMargin> probabilistic)
    {
        return probabilistic.Sum(x => x.Earnings * x.Chance);
    }

    public double AttemptsToProfit(IReadOnlyList<ProbabilisticProfitMargin> probabilistic)
    {
        var maxProfit = probabilistic.MaxBy(x => x.Earnings)?.Earnings ?? 0;
        if (maxProfit == 0)
        {
            return 0;
        }
        // calculate the expectation chance for profit by weighing each profit between the max and min profitable outcome
        var virtualSignificantProfitChance = probabilistic
            .Sum(x => x.Earnings / maxProfit * x.Chance);
        var total = probabilistic.Sum(x => x.Chance);
        if (total == 0)
        {
            return 0;
        }
        var normalizedProfitChance = virtualSignificantProfitChance / total;

        if (normalizedProfitChance <= 0)
        {
            return 0;
        }

        if (normalizedProfitChance >= 1)
        {
            return 1;
        }
        // number of attempts until we have a 66% expectation of profit
        var attempts = Math.Log(1 - (2.0 / 3)) / Math.Log(1.0 - normalizedProfitChance);
        // if every attempt is profitable we get a negative number of attempts
        return Math.Ceiling(Math.Max(1, attempts));
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

    public bool CanBuyFromVendor()
    {
        return Skill.DropLevel is > 0
               && string.IsNullOrEmpty(Skill.Discriminator)
               && !vendorRestrictedGems.Contains(Skill.Name)
               && !Skill.IsVaalSkillGem()
               && !Skill.IsAwakenedGem();
    }
}

public static class ProfitRecipeExtension
{
    public static IServiceCollection AddProfitRecipes(this IServiceCollection services)
    {
        AddRecipe(typeof(LevelCorruptAddLevelDropFailureSell));
        AddRecipe(typeof(LevelCorruptAddLevelSell));
        AddRecipe(typeof(LevelDoubleCorruptAddLevelAndQualitySell));
        AddRecipe(typeof(LevelSell));
        AddRecipe(typeof(LevelVendorQualityLevelSell));
        AddRecipe(typeof(LevelVendorQualitySell));
        AddRecipe(typeof(QualityLevelSell));
        AddRecipe(typeof(VendorBuyCorruptLevelSellVaal));
        AddRecipe(typeof(VendorBuyLevelCorruptAddLevelDropFailureSell));
        AddRecipe(typeof(VendorBuyLevelCorruptAddLevelSell));
        AddRecipe(typeof(VendorBuyLevelDoubleCorruptAddLevelAndQualitySell));
        AddRecipe(typeof(VendorBuyLevelSell));
        AddRecipe(typeof(VendorBuyLevelVendorQualityLevelSell));
        AddRecipe(typeof(VendorBuyLevelVendorQualitySell));
        AddRecipe(typeof(VendorBuyQualityLevelSell));
        return services;

        void AddRecipe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type t) =>
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IProfitRecipe), t, ServiceLifetime.Transient));
    }
}

public readonly struct CorruptionOutcome(string value) : IEquatable<CorruptionOutcome>
{
    public static CorruptionOutcome AddLevel => new("corrupt_add_level");
    public static CorruptionOutcome RemLevel => new("corrupt_rem_level");
    public static CorruptionOutcome AddQuality => new("corrupt_add_quality");
    public static CorruptionOutcome RemQuality => new("corrupt_rem_quality");
    public static CorruptionOutcome AddLevelAddQuality => new("double_corrupt_add_level_add_quality");
    public static CorruptionOutcome AddLevelRemQuality => new("double_corrupt_add_level_rem_quality");
    public static CorruptionOutcome AddLevelMaxQuality => new("double_corrupt_add_level_max_quality");
    public static CorruptionOutcome MaxLevelAddQuality => new("double_corrupt_max_level_add_quality");
    public static CorruptionOutcome AnyLevelRemQuality => new("double_corrupt_corrupt_any_level_rem_quality");
    public static CorruptionOutcome NoChange => new("no_change");

    public string Value => value;

    public bool Equals(CorruptionOutcome other)
    {
        var x = string.IsNullOrEmpty(Value) ? null : Value;
        var y = string.IsNullOrEmpty(other.Value) ? null : other.Value;
        return StringComparer.OrdinalIgnoreCase.Equals(x, y);
    }

    public override bool Equals(object? obj)
    {
        return obj is CorruptionOutcome other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public static CorruptionOutcome From(string? value)
    {
        return string.IsNullOrEmpty(value) ? default : new(value);
    }

    public static implicit operator string(CorruptionOutcome x) => x.Value;
    public static bool operator ==(CorruptionOutcome x, CorruptionOutcome y) => x.Equals(y);
    public static bool operator !=(CorruptionOutcome x, CorruptionOutcome y) => !(x == y);
}
