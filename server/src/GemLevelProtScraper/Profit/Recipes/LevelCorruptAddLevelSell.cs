using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelCorruptAddLevelSell : IProfitRecipe
{
    public string Name => "level_corrupt_add_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (ctx.MinLevel is not { } min
            || ctx.CorruptedAddLevel is not { } max
            || (ctx.CorruptedMaxLevel20Quality ?? ctx.CorruptedMaxLevel) is not { } corruptFailure
            || (ctx.CorruptedMaxLevel23Quality ?? corruptFailure) is not { } corruptQuality
        )
        {
            return null;
        }

        if (ctx.Skill.CanBuyFromVendor())
        {
            return null;
        }

        // buy gem, level, corrupt for level, sell
        // 25% unchanged -> failure
        // 25% remove level -> failure, more exp required
        // 25% add level -> success
        // 25% add quality -> failure
        var profitSuccess = (max.ChaosValue - min.ChaosValue) * 0.25;
        var profitQuality = (corruptQuality.ChaosValue - min.ChaosValue) * 0.25;
        var profitFailure = (corruptFailure.ChaosValue - min.ChaosValue) * 0.5;
        var levelEarning = profitSuccess + profitFailure + profitQuality;

        var failureAddExperience = ctx.Skill.LastLevelExperience
                                * 0.25
                                * ctx.ExperienceFactor(ctx.GemQuality(min));
        var successExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));
        var deltaExperience = successExperience + failureAddExperience;
        return new()
        {
            GainMargin = ctx.GainMargin(levelEarning, deltaExperience),
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = levelEarning,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(max, successExperience),
        };
    }
}
