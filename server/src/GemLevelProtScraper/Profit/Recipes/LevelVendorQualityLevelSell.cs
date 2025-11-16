using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelVendorQualityLevelSell : IProfitRecipe
{
    public string Name => "level_vendor_quality_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (ctx.MaxLevel20Quality is not { } max || ctx.MinLevel is not { } min)
        {
            return null;
        }

        if (ctx.Skill.CanBuyFromVendor())
        {
            return null;
        }

        if (max.GemQuality <= min.GemQuality)
        {
            return null;
        }

        if (!ctx.Skill.IsSupportGem())
        {
            return null;
        }

        return ProfitMarginUnchecked(ctx, max, min);
    }

    public static ProfitMargin ProfitMarginUnchecked(SkillProfitCalculationContext ctx, SkillGemPrice max, SkillGemPrice min)
    {
        // level the gem, vendor it with 1x Gem Cutter, level it again, sell it
        var levelEarning = max.ChaosValue - min.ChaosValue;
        var qualityCost = ctx.ExchangeRate(CurrencyTypeName.CartographersChisel) ?? 1;

        var adjustedEarnings = levelEarning - qualityCost;

        var deltaExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min))
                              + ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(max));
        var gainMargin = ctx.GainMargin(adjustedEarnings, deltaExperience);

        return new()
        {
            GainMargin = gainMargin,
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = adjustedEarnings,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(max, deltaExperience)
        };
    }
}
