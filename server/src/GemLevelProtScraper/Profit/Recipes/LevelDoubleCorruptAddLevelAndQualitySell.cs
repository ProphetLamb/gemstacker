using System.Diagnostics.CodeAnalysis;
using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelDoubleCorruptAddLevelAndQualitySell : IProfitRecipe
{
    public string Name => "level_corrupt_add_level_and_quality_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if ((ctx.MinLevel ?? ctx.MinLevel20Quality) is not { } min
            || (ctx.CorruptedAddLevel20Quality ?? ctx.CorruptedAddLevel) is not { } corruptAddLevel
            || (ctx.CorruptedAddLevel ?? corruptAddLevel) is not { } corruptAddLevelRemQuality
            || (ctx.CorruptedMaxLevel20Quality ?? ctx.CorruptedMaxLevel) is not { } corruptFailure
            || (ctx.CorruptedMaxLevel23Quality ?? corruptFailure) is not { } corruptAddQuality
            || (ctx.CorruptedMaxLevel ?? corruptFailure) is not { } corruptRemQuality
            || ctx.CorruptedAddLevel23Quality is not { } corruptAddLevel23Quality)
        {
            return null;
        }

        if (ctx.CanBuyFromVendor())
        {
            return null;
        }

        return ProfitMarginUnchecked(
            ctx,
            corruptAddLevel,
            corruptAddLevel23Quality,
            corruptAddLevelRemQuality,
            corruptAddQuality,
            corruptRemQuality,
            corruptFailure,
            min
        );
    }

    [SuppressMessage("Style", "IDE0048:Add parentheses for clarity")]
    [SuppressMessage("Style", "IDE0047:Remove unnecessary parentheses")]
    [SuppressMessage("ReSharper", "ArrangeMissingParentheses")]
    public static ProfitMargin ProfitMarginUnchecked(
        SkillProfitCalculationContext ctx,
        SkillGemPrice corruptAddLevel,
        SkillGemPrice corruptAddLevel23Quality,
        SkillGemPrice corruptAddLevelRemQuality,
        SkillGemPrice corruptAddQuality,
        SkillGemPrice corruptRemQuality,
        SkillGemPrice corruptFailure,
        SkillGemPrice min
    )
    {
        Dictionary<string, double> recipeCost = new()
        {
            [CurrencyTypeName.GemcuttersPrism] = corruptAddLevel.GemQuality - min.GemQuality, // 20 quality
            [CurrencyTypeName.ChaosOrb] = 50, // "Lapidary Lens" isn't currency we value it with 50c for now
        };

        var addLevel23q = (1 / 8.0m) * ((1 / 3.0m) * (8 / 21.0m)) + ((1 / 4.0m) * (8 / 21.0m)) * (1 / 6.0m);
        var addLevel20q = (1 / 8.0m) * (1 / 3.0m) + (2 / 8.0m) * (1 / 6.0m);
        var addLevel10q = addLevel20q - addLevel23q;
        var maxLevel23q = (3 / 4.0m) * ((1 / 3.0m) * (8 / 21.0m)) + ((1 / 4.0m) * (8 / 21.0m)) * (2 / 3.0m);
        var anyLevel10q = maxLevel23q + addLevel23q; // add and remove level & quality have the same chances
        var remLevelAnyQ = 1 / 8.0 + 7 / 8.0 * 1 / 6.0;
        List<ProbabilisticProfitMargin> probabilistic =
        [
            new()
            {
                Chance =
                    (double)addLevel23q,
                Earnings = corruptAddLevel23Quality.ChaosValue - min.ChaosValue,
                Label = "double_corrupt_add_level_add_quality",
            },
            new()
            {
                Chance = (double)addLevel10q,
                Earnings = corruptAddLevelRemQuality.ChaosValue - min.ChaosValue,
                Label = "double_corrupt_add_level_rem_quality"
            },
            new()
            {
                Chance = (double)addLevel20q,
                Earnings = corruptAddLevel.ChaosValue - min.ChaosValue,
                Label = "double_corrupt_add_level_max_quality",
            },
            new()
            {
                Chance = (double)maxLevel23q,
                Earnings = corruptAddQuality.ChaosValue - min.ChaosValue,
                Label = "double_corrupt_max_level_add_quality"
            },
            new()
            {
                Chance = (double)anyLevel10q,
                Earnings = corruptRemQuality.ChaosValue - min.ChaosValue,
                Label = "double_corrupt_corrupt_any_level_rem_quality"
            },
            new()
            {
                Chance = (double)(1.0m - anyLevel10q - maxLevel23q - addLevel20q - addLevel10q - addLevel23q),
                Earnings = corruptFailure.ChaosValue - min.ChaosValue,
                Label = "no_change"
            },
        ];

        var corruptExperienceRemoveLevel = ctx.Skill.LastLevelExperience
                                           * remLevelAnyQ
                                           * ctx.ExperienceFactor(ctx.GemQuality(min));
        var levelExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));
        var deltaExperience = levelExperience + corruptExperienceRemoveLevel;
        var levelEarning = ctx.ProbabilisticEarnings(probabilistic) - ctx.RecipeCost(recipeCost);

        // replace the divine with lapidary lens for output
        _ = recipeCost.Remove(CurrencyTypeName.DivineOrb);
        recipeCost["Lapidary Lens"] = 1;
        return new()
        {
            GainMargin = ctx.GainMargin(levelEarning, deltaExperience),
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = levelEarning,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(corruptAddLevel, levelExperience),
            MinAttemptsToProfit = ctx.AttemptsToProfit(probabilistic),
            RecipeCost = recipeCost,
            Probabilistic = probabilistic
        };
    }
}
