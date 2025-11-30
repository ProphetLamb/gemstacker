using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelCorruptAddLevelSell : IProfitRecipe
{
    public string Name => "level_corrupt_add_level_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if ((ctx.MinLevel ?? ctx.MinLevel20Quality) is not { } min
            || (ctx.CorruptedAddLevel20Quality ?? ctx.CorruptedAddLevel) is not { } corruptAddLevel
            || (ctx.CorruptedMaxLevel20Quality ?? ctx.CorruptedMaxLevel) is not { } corruptFailure
            || (ctx.CorruptedMaxLevel23Quality ?? corruptFailure) is not { } corruptAddQuality
            || (ctx.CorruptedMaxLevel ?? corruptFailure) is not { } corruptRemQuality)
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
            corruptAddQuality,
            corruptRemQuality,
            corruptFailure,
            min
        );
    }

    public static ProfitMargin ProfitMarginUnchecked(
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
            [CurrencyTypeName.VaalOrb] = 1, // 12.5% to 50% chance = 4 attempts
            [CurrencyTypeName.GemcuttersPrism] = corruptAddLevel.GemQuality - min.GemQuality, // 20 quality
        };
        // buy gem, level, corrupt for level, sell
        // 25% unchanged or vaal -> failure
        // 25% add or remove level -> failure, more exp required
        // 25% add quality -> failure
        var p = 1 / 8.0m;
        var addQ = 1 / 4.0m * 8 / 21.0m;
        var remQ = 1 / 4.0m * 10 / 21.0m;
        List<ProbabilisticProfitMargin> probabilistic = [
            new() { Chance = (double)p, Earnings = corruptAddLevel.ChaosValue - min.ChaosValue, Label = CorruptionOutcome.AddLevel },
            new() { Chance = (double)addQ, Earnings = corruptAddQuality.ChaosValue - min.ChaosValue, Label =  CorruptionOutcome.AddQuality },
            new() { Chance = (double)remQ, Earnings =  corruptRemQuality.ChaosValue - min.ChaosValue, Label = CorruptionOutcome.RemQuality },
            new() { Chance = (double)(1.0m - p - addQ - remQ), Earnings = corruptFailure.ChaosValue - min.ChaosValue, Label = CorruptionOutcome.NoChange },
        ];

        var corruptExperienceRemoveLevel = ctx.Skill.LastLevelExperience
                                           * (double)p
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
            MinAttemptsToProfit = ctx.AttemptsToProfit(probabilistic),
            RecipeCost = recipeCost,
            Probabilistic = probabilistic
        };
    }
}
