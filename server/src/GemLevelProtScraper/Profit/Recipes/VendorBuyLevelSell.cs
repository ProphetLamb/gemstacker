using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyLevelSell : IProfitRecipe
{
    public string Name => "vendor_buy_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (!ctx.Skill.CanBuyFromVendor() || ctx.MaxLevel is not { } max || max.GemLevel == 1)
        {
            return null;
        }

        return LevelSell.ProfitMarginUnchecked(ctx, max, max.ToVendorFreePrice());
    }
}
