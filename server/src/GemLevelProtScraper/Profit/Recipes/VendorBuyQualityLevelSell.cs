using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyQualityLevelSell : IProfitRecipe
{
    public string Name => "vendor_buy_quality_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (!ctx.Skill.CanBuyFromVendor() || (ctx.MaxLevel20Quality ?? ctx.CorruptedMaxLevel20Quality) is not { } max)
        {
            return null;
        }

        return QualityLevelSell.ProfitMarginUnchecked(ctx, max, max.ToVendorFreePrice());
    }
}
