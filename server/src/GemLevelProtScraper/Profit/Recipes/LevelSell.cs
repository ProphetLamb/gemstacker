using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelSell : IProfitRecipe
{
    public string Name => "level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (ctx.MinAndMaxMaybeCorrupted() is not { } prices)
        {
            return null;
        }

        if (ctx.Skill.CanBuyFromVendor())
        {
            return null;
        }

        return ProfitMarginUnchecked(ctx, prices.Max, prices.Min);
    }

    public static ProfitMargin ProfitMarginUnchecked(SkillProfitCalculationContext ctx, SkillGemPrice max, SkillGemPrice min)
    {
        // level the gem, sell it
        var levelEarning = max.ChaosValue - min.ChaosValue;
        var deltaExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));

        return new()
        {
            GainMargin = ctx.GainMargin(levelEarning, deltaExperience),
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = levelEarning,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(max, deltaExperience)
        };
    }
}
