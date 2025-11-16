namespace GemLevelProtScraper.Profit.Recipes;

public class LevelCorruptAddLevelSell : IProfitRecipe
{
    public string Name => "level_corrupt_add_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (ctx.MinLevel is not { } min
            || ctx.CorruptedAddLevel is not { } max
            || ctx.CorruptedMaxLevel is not { } corruptFailure)
        {
            return null;
        }

        // buy gem, level, corrupt for level, sell
        // P(success) => max - min
        // P(1-success) => corruptFailure - min
        var profitSuccess = (max.ChaosValue - min.ChaosValue) / GemCorruptionHelper.AttemptsForOneInFour;
        var profitFailure = (corruptFailure.ChaosValue - min.ChaosValue) / GemCorruptionHelper.AttemptsForThreeInFour;
        var levelEarning = profitSuccess + profitFailure;

        var failureExperience = ctx.Skill.LastLevelExperience
                                / GemCorruptionHelper.AttemptsForThreeInFour
                                * ctx.ExperienceFactor(ctx.GemQuality(min));
        var successExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));
        var deltaExperience = successExperience + failureExperience;
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
