using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyQualityLevelSell : IProfitRecipe
{
    public string Name => "vendor_buy_quality_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (!ctx.Skill.CanBuyFromVendor() || ctx.MaxLevel is not { } max)
        {
            return null;
        }

        return QualityLevelSell.ProfitMarginUnchecked(ctx, max, max.ToVendorFreePrice());
    }
}
