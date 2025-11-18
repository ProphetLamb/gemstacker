using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyLevelSell : IProfitRecipe
{
    public string Name => "vendor_buy_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (!ctx.CanBuyFromVendor() || (ctx.MaxLevel ?? ctx.CorruptedMaxLevel) is not { } max)
        {
            return null;
        }

        return LevelSell.ProfitMarginUnchecked(ctx, max, max.ToVendorFreePrice());
    }
}
