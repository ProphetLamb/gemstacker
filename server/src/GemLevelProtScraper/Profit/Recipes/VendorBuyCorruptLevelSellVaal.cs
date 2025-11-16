using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyCorruptLevelSellVaal : IProfitRecipe
{
    public string Name => "vendor_buy_corrupt_level_sell_vaal";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if ((ctx.MaxLevel ?? ctx.CorruptedMaxLevel) is not { } max || !ctx.Skill.IsVaalSkillGem())
        {
            return null;
        }

        // buy gem from vendor, corrupt it
        // if is not vaal gem, repeat
        // otherwise, level, then sell gem
        var attempts = GemCorruptionHelper.AttemptsForOneInFour;
        var vaalOrb = ctx.ExchangeRate(CurrencyTypeName.VaalOrb) ?? 1;
        var costChaos = attempts * vaalOrb;

        var levelEarning = max.ChaosValue - costChaos;
        var min = max.ToVendorFreePrice() with { Corrupted = false };
        var deltaExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));
        return new()
        {
            GainMargin = ctx.GainMargin(levelEarning, deltaExperience),
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = levelEarning,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(max, deltaExperience),
        };
    }
}
