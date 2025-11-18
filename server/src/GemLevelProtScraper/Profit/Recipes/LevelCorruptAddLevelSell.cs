using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelCorruptAddLevelSell : IProfitRecipe
{
    public string Name => "level_corrupt_add_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if (ctx.MinLevel is not { } min
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

        return ProfitMarginUnchecked(ctx, corruptAddLevel, corruptAddQuality, corruptRemQuality, corruptFailure, min);
    }

    public static ProfitMargin? ProfitMarginUnchecked(SkillProfitCalculationContext ctx, SkillGemPrice corruptAddLevel,
        SkillGemPrice corruptAddQuality, SkillGemPrice corruptRemQuality, SkillGemPrice corruptFailure, SkillGemPrice min)
    {
        // buy gem, level, corrupt for level, sell
        // 25% unchanged or vaal -> failure
        // 25% add or remove level -> failure, more exp required
        // 25% add quality -> failure
        var profitAddLevel = (corruptAddLevel.ChaosValue - min.ChaosValue) * 0.125;
        var profitAddQuality = (corruptAddQuality.ChaosValue - min.ChaosValue) * 0.125;
        var profitRemQuality = (corruptRemQuality.ChaosValue - min.ChaosValue) * 0.125;
        var profitFailure = (corruptFailure.ChaosValue - min.ChaosValue) * 0.625;
        var levelEarning = profitAddLevel + profitAddQuality + profitRemQuality + profitFailure;

        var corruptExperienceRemoveLevel = ctx.Skill.LastLevelExperience
                                           * 0.125
                                           * ctx.ExperienceFactor(ctx.GemQuality(min));
        var levelExperience = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));
        var deltaExperience = levelExperience + corruptExperienceRemoveLevel;
        return new()
        {
            GainMargin = ctx.GainMargin(levelEarning, deltaExperience),
            ExperienceDelta = deltaExperience,
            AdjustedEarnings = levelEarning,
            Buy = ctx.ToProfitLevelResponse(min, 0),
            Sell = ctx.ToProfitLevelResponse(corruptAddLevel, levelExperience),
            Probabilistic =
            [
                new() { Chance = 0.125, Earnings = profitAddLevel, Label = "corrupt_add_level" },
                new() { Chance = 0.125, Earnings = profitAddQuality, Label = "corrupt_add_quality" },
                new() { Chance = 0.125, Earnings = profitFailure, Label = "corrupt_remove_level" },
                new() { Chance = 0.125, Earnings = profitAddQuality, Label = "corrupt_remove_quality" },
                new() { Chance = 0.5, Earnings = profitFailure, Label = "no_change" },
            ]
        };
    }
}
