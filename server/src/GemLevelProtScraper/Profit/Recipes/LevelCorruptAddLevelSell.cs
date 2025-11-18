using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelCorruptAddLevelSell : IProfitRecipe
{
    public string Name => "level_corrupt_add_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if ((ctx.MinLevel ?? ctx.MinLevel20Quality) is not { } min
            || ctx.CorruptedAddLevel is not { } corruptAddLevel
            || (ctx.CorruptedMaxLevel20Quality ?? ctx.CorruptedMaxLevel) is not { } corruptFailure
            || (ctx.CorruptedMaxLevel23Quality ?? corruptFailure) is not { } corruptAddQuality
            || (ctx.CorruptedMaxLevel ?? corruptFailure) is not { } corruptRemQuality)
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
            corruptAddQuality,
            corruptRemQuality,
            corruptFailure,
            min
        );
    }

    public static ProfitMargin? ProfitMarginUnchecked(
        SkillProfitCalculationContext ctx,
        SkillGemPrice corruptAddLevel,
        SkillGemPrice corruptAddQuality,
        SkillGemPrice corruptRemQuality,
        SkillGemPrice corruptFailure,
        SkillGemPrice min
    )
    {
        Dictionary<string, double> recipeCost = new()
        {
            [CurrencyTypeName.GemcuttersPrism] = corruptAddLevel.GemQuality - min.GemQuality, // 20 quality
            [CurrencyTypeName.VaalOrb] = 4, // 12.5% to 50% chance = 4 attempts
        };
        // buy gem, level, corrupt for level, sell
        // 25% unchanged or vaal -> failure
        // 25% add or remove level -> failure, more exp required
        // 25% add quality -> failure
        var p = 1 / 8.0;
        List<ProbabilisticProfitMargin> probabilistic = [
            new() { Chance = p, Earnings = corruptAddLevel.ChaosValue - min.ChaosValue, Label = "corrupt_add_level" },
            new() { Chance = p, Earnings = corruptAddQuality.ChaosValue - min.ChaosValue, Label = "corrupt_add_quality" },
            new() { Chance = p, Earnings =  corruptRemQuality.ChaosValue - min.ChaosValue, Label = "corrupt_remove_quality" },
            new() { Chance = p, Earnings = corruptFailure.ChaosValue - min.ChaosValue, Label = "corrupt_remove_level" },
            new() { Chance = p * 4, Earnings = corruptFailure.ChaosValue - min.ChaosValue, Label = "no_change" },
        ];

        var corruptExperienceRemoveLevel = ctx.Skill.LastLevelExperience
                                           * p
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
