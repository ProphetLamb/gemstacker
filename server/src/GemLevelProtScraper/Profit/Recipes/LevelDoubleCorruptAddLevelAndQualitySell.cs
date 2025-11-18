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
            || ctx.CorruptedMaxLevel23Quality is not { } corruptMaxLevel23Quality)
        {
            return null;
        }

        if (ctx.Skill.CanBuyFromVendor())
        {
            return null;
        }

        return ProfitMarginUnchecked(
            ctx,
            corruptAddLevel,
            corruptMaxLevel23Quality,
            corruptAddLevelRemQuality,
            corruptAddQuality,
            corruptRemQuality,
            corruptFailure,
            min
        );
    }

    public static ProfitMargin ProfitMarginUnchecked(
        SkillProfitCalculationContext ctx,
        SkillGemPrice corruptAddLevel,
        SkillGemPrice corruptMaxLevel23Quality,
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
            [CurrencyTypeName.DivineOrb] = 1, // "Lapidary Lens" isn't currency we value it with 1div for now
        };

        // lens corrupts twice and cant choose the same outcome twice
        // probability for a successful event is
        // - the event -> anything else = 1/8 * 4/6
        // - anything else -> the event = 5/8 * 1/6
        // anything else := exclude double event lvl21/23, and we exclude rem quality, or rem level
        // depending on whether were calculating add quality or add level
        var pOne = (1 / 8.0 * (5 / 6.0)) + (6 / 8.0 * (1 / 6.0));
        // probability for two events is 1/8 * 1/6
        var pTwo = 1 / 8.0 * 1 / 6.0;
        List<ProbabilisticProfitMargin> probabilistic =
        [
            new()
            {
                Chance = pTwo,
                Earnings = corruptMaxLevel23Quality.ChaosValue - min.ChaosValue,
                Label = "corrupt_add_level_add_quality",
            },
            new()
            {
                Chance = pTwo,
                Earnings = corruptAddLevelRemQuality.ChaosValue - min.ChaosValue,
                Label = "corrupt_add_level_remove_quality"
            },
            new()
            {
                Chance = pOne, Earnings = corruptAddLevel.ChaosValue - min.ChaosValue, Label = "corrupt_add_level",
            },
            new()
            {
                Chance = pOne,
                Earnings = corruptAddQuality.ChaosValue - min.ChaosValue,
                Label = "corrupt_add_quality"
            },
            // probability for remove quality is
            // - the event -> anything else = 1/8 * 6/6
            // - anything else-> the event = 7/8 * 1/6
            new()
            {
                Chance = (1 / 8.0) + (7 / 8.0 * (1 / 6.0)),
                Earnings = corruptRemQuality.ChaosValue - min.ChaosValue,
                Label = "corrupt_rem_quality"
            },
            // probability for no change is
            // - no change and vaal -> same, but no change or vaal = 5/8 * 2/6
            // - remove level -> remainder = 1/8 * 1/6
            // anything else := exclude add level, add quality, remove quality
            new()
            {
                Chance = (5 / 8.0 * (2 / 6.0)) + pTwo,
                Earnings = corruptFailure.ChaosValue - min.ChaosValue,
                Label = "no_change"
            },
        ];

        var corruptExperienceRemoveLevel = ctx.Skill.LastLevelExperience
                                           * pOne
                                           * ctx.ExperienceFactor(ctx.GemQuality(min));
        var levelExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));
        var deltaExperience = levelExperience + corruptExperienceRemoveLevel;
        var levelEarning = ctx.ProbabilisticEarnings(probabilistic) - ctx.RecipeCost(recipeCost);

        return new()
        {
            GainMargin = ctx.GainMargin(levelEarning, deltaExperience),
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = levelEarning,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(corruptAddLevel, levelExperience),
            RecipeCost = recipeCost,
            Probabilistic = probabilistic
        };
    }
}
