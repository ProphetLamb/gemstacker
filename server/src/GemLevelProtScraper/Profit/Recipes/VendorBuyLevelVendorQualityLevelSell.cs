using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public sealed class VendorBuyLevelVendorQualityLevelSell : IProfitRecipe
{
    public string Name => "vendor_buy_level_vendor_quality_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (!ctx.Skill.CanBuyFromVendor() || ctx.MaxLevel is not { } max)
        {
            return null;
        }

        if (!ctx.Skill.IsSupportGem())
        {
            return null;
        }

        return LevelVendorQualityLevelSell.ProfitMarginUnchecked(
            ctx,
            max,
            max.ToVendorFreePrice()
        );
    }
}
