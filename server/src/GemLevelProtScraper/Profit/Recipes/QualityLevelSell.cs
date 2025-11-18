using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class QualityLevelSell : IProfitRecipe
{
    public string Name => "quality_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if ((ctx.MaxLevel20Quality ?? ctx.CorruptedMaxLevel20Quality) is not { } max || ctx.MinLevel is not { } min)
        {
            return null;
        }

        if (ctx.Skill.CanBuyFromVendor())
        {
            return null;
        }

        return ProfitMarginUnchecked(ctx, max, min);
    }

    public static ProfitMargin ProfitMarginUnchecked(SkillProfitCalculationContext ctx, SkillGemPrice max,
        SkillGemPrice min)
    {
        // quality the gem, level the gem, sell the gem
        var levelEarning = max.ChaosValue - min.ChaosValue;
        Dictionary<string, double> recipeCost = new()
        {
            [CurrencyTypeName.GemcuttersPrism] = max.GemQuality - min.GemQuality,
        };

        var deltaExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(max));

        var adjustedEarnings = levelEarning - ctx.RecipeCost(recipeCost);
        return new()
        {
            GainMargin = ctx.GainMargin(adjustedEarnings, deltaExperience),
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = adjustedEarnings,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(max, deltaExperience),
            RecipeCost = recipeCost,
        };
    }
}
