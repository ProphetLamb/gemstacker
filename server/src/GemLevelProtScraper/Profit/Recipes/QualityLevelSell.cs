using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class QualityLevelSell : IProfitRecipe
{
    public string Name => "quality_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (ctx.MaxLevel20Quality is not { } max || ctx.MinLevel is not { } min)
        {
            return null;
        }

        if (max.GemQuality <= min.GemQuality)
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
        var qualityCost = 20 * (ctx.ExchangeRate(CurrencyTypeName.CartographersChisel) ?? 1);

        var adjustedEarnings = levelEarning - qualityCost;

        var deltaExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(max));
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
