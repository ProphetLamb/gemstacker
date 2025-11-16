using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyLevelVendorQualitySell : IProfitRecipe
{
    public string Name => "vendor_buy_level_vendor_quality_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (!ctx.Skill.CanBuyFromVendor() || ctx.MaxLevel is not { } max || max.GemLevel == 1)
        {
            return null;
        }

        if (!ctx.Skill.IsSupportGem())
        {
            return null;
        }

        return LevelVendorQualitySell.ProfitMarginUnchecked(ctx, max, max.ToVendorFreePrice());
    }
}
