using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelCorruptAddLevelSell : IProfitRecipe
{
    public string Name => "level_corrupt_add_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (ctx.MinLevel is not { } min
            || ctx.CorruptedAddLevel is not { } corruptAddLevel
            || (ctx.CorruptedMaxLevel20Quality ?? ctx.CorruptedMaxLevel) is not { } corruptFailure
            || (ctx.CorruptedMaxLevel23Quality ?? corruptFailure) is not { } corruptAddQuality
        )
        {
            return null;
        }

        if (ctx.Skill.CanBuyFromVendor())
        {
            return null;
        }

        // buy gem, level, corrupt for level, sell
        // 25% unchanged or vaal -> failure
        // 25% add or remove level -> failure, more exp required
        // 25% add quality -> failure
        var profitAddLevel = (corruptAddLevel.ChaosValue - min.ChaosValue) * 0.125;
        var profitQuality = (corruptAddQuality.ChaosValue - min.ChaosValue) * 0.125;
        var profitFailure = (corruptFailure.ChaosValue - min.ChaosValue) * 0.75;
        var levelEarning = profitAddLevel + profitFailure + profitQuality;

        var corruptExperienceRemoveLevel = ctx.Skill.LastLevelExperience
                                * 0.125
                                * ctx.ExperienceFactor(ctx.GemQuality(min));
        var levelExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));
        var deltaExperience = levelExperience + corruptExperienceRemoveLevel;
        return new()
        {
            GainMargin = ctx.GainMargin(levelEarning, deltaExperience),
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = levelEarning,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(corruptAddLevel, levelExperience),
        };
    }
}
