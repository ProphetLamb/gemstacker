using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class VendorBuyLevelCorruptAddLevelDropFailureSell : IProfitRecipe
{
    public string Name => "vendor_buy_level_corrupt_add_level_drop_failure_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if ((ctx.CorruptedAddLevel20Quality ?? ctx.CorruptedAddLevel) is not { } corruptAddLevel
            || (ctx.CorruptedMaxLevel20Quality ?? ctx.CorruptedMaxLevel) is not { } corruptFailure
            || (ctx.CorruptedMaxLevel23Quality ?? corruptFailure) is not { } corruptAddQuality
            || (ctx.CorruptedMaxLevel ?? corruptFailure) is not { } corruptRemQuality)
        {
            return null;
        }

        if (!ctx.CanBuyFromVendor())
        {
            return null;
        }

        return LevelCorruptAddLevelDropFailureSell.ProfitMarginUnchecked(
            ctx,
            corruptAddLevel,
            corruptAddQuality,
            corruptRemQuality,
            corruptFailure,
            corruptFailure.ToVendorFreePrice() with { Corrupted = false },
            ctx.CorruptedMinLevel20Quality ?? ctx.CorruptedMinLevel ?? corruptFailure
        );
    }
}
