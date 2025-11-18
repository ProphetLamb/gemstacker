using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyLevelDoubleCorruptAddLevelAndQualitySell : IProfitRecipe
{
    public string Name => "vendor_buy_level_corrupt_add_level_and_quality_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if ((ctx.CorruptedAddLevel20Quality ?? ctx.CorruptedAddLevel) is not { } corruptAddLevel
            || (ctx.CorruptedAddLevel ?? corruptAddLevel) is not { } corruptAddLevelRemQuality
            || (ctx.CorruptedMaxLevel20Quality ?? ctx.CorruptedMaxLevel) is not { } corruptFailure
            || (ctx.CorruptedMaxLevel23Quality ?? corruptFailure) is not { } corruptAddQuality
            || (ctx.CorruptedMaxLevel ?? corruptFailure) is not { } corruptRemQuality
            || ctx.CorruptedMaxLevel23Quality is not { } corruptMaxLevel23Quality)
        {
            return null;
        }

        if (!ctx.Skill.CanBuyFromVendor())
        {
            return null;
        }
        return LevelDoubleCorruptAddLevelAndQualitySell.ProfitMarginUnchecked(
            ctx,
            corruptAddLevel,
            corruptMaxLevel23Quality,
            corruptAddLevelRemQuality,
            corruptAddQuality,
            corruptRemQuality,
            corruptFailure,
            corruptFailure.ToVendorFreePrice() with { Corrupted = false }
        );
    }
}
