using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyCorruptLevelSellVaal : IProfitRecipe
{
    public string Name => "vendor_buy_corrupt_level_sell_vaal";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (ctx.MaxLevel is not { } max || !ctx.Skill.IsVaalSkillGem())
        {
            return null;
        }

        // buy gem from vendor, corrupt it
        // if is not vaal gem, repeat
        // otherwise, level, then sell gem
        var attempts = Math.Ceiling(Math.Log(.25) / Math.Log(.66)); // 66% success needed, rounded up
        var vaalOrb = ctx.ExchangeRate(CurrencyTypeName.VaalOrb) ?? 1;
        var divineOrb = ctx.ExchangeRate(CurrencyTypeName.DivineOrb) ?? 1;
        var costChaos = attempts * vaalOrb;
        var costDivine = costChaos / divineOrb;
        return LevelSell.ProfitMarginUnchecked(
            ctx,
            max.ToVendorFreePrice() with { ChaosValue = costChaos, DivineValue = costDivine },
            max
        );
    }
}
