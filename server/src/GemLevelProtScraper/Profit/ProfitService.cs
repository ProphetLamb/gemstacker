using System.Runtime.CompilerServices;
using GemLevelProtScraper.Profit.Recipes;
using GemLevelProtScraper.Skills;
using Microsoft.Extensions.Options;

namespace GemLevelProtScraper.Profit;

public sealed class ProfitServiceOptions : IOptions<ProfitServiceOptions>
{
    public required Dictionary<string, double> SpecialExperienceFactorPerQualityGams
    {
        get;
        init;
    }

    ProfitServiceOptions IOptions<ProfitServiceOptions>.Value => this;
}

public sealed class ProfitService(
    SkillGemRepository repository,
    ExchangeRateProvider exchangeRateProvider,
    IOptions<ProfitServiceOptions> options,
    IEnumerable<IProfitRecipe> profitRecipes,
    ILoggerFactory loggerFactory
)
{
    private List<IProfitRecipe> ProfitRecipes
    {
        get;
    } = profitRecipes.ToList();

    public async IAsyncEnumerable<ProfitResponse> GetProfitAsync(
        ProfitRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var valueSpecialExperienceFactorPerQualityGams = options.Value.SpecialExperienceFactorPerQualityGams;
        var exchangeRates = await exchangeRateProvider.GetExchangeRatesAsync(cancellationToken).ConfigureAwait(false);
        var pricedGems = repository.GetPricedGemsAsync(request.League, request.GemNameWildcard, cancellationToken);
        var eligiblePricedGems = pricedGems
            .SelectTruthy(g => new ProfitMarginCalculator(
                    request,
                    g.Skill,
                    exchangeRates,
                    valueSpecialExperienceFactorPerQualityGams,
                    ProfitRecipes,
                    loggerFactory.CreateLogger<ProfitMarginCalculator>()
                ).ComputeProfitMargin(g.Prices) is { } delta
                    ? new { g.Skill, Delta = delta }
                    : null
            )
            .OrderByDescending(g => g.Delta.MaxGainMargin);

        var result = eligiblePricedGems.Select(g => new ProfitResponse
            {
                Name = g.Skill.Name,
                Color = g.Skill.Color,
                Discriminator = g.Skill.Discriminator,
                Type = g.Skill.BaseType,
                ForeignInfoUrl = $"https://poedb.tw{g.Skill.RelativeUrl}",
                PreferredRecipe = g.Delta.PreferredRecipe,
                Recipes = g.Delta.ProfitMargins,
                GainMargin = g.Delta.MaxGainMargin,
                Icon = g.Skill.IconUrl,
                Max = g.Delta.Max.Data,
                Min = g.Delta.Min.Data,
            }
        );

        await using var en = result.GetAsyncEnumerator(cancellationToken);
        while (await en.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return en.Current;
        }
    }
}

internal readonly struct ProfitMarginCalculator(
    ProfitRequest request,
    SkillGem skill,
    ExchangeRateCollection exchangeRates,
    IReadOnlyDictionary<string, double> experienceFactorAddQualityByName,
    IReadOnlyCollection<IProfitRecipe> recipes,
    ILogger<ProfitMarginCalculator> logger
)
{
    public PriceDelta? ComputeProfitMargin(IEnumerable<SkillGemPrice> prices)
    {
        var profitRequest = request;
        var pricesOrdered = prices
            .Where(p => (profitRequest.MaxBuyPriceChaos is not { } maxBuy || p.ChaosValue <= maxBuy)
                        && (profitRequest.MinSellPriceChaos is not { } minSell || p.ChaosValue >= minSell)
            )
            .ToArray();
        if (pricesOrdered.Sum(p => p.ListingCount) < request.MinimumListingCount)
        {
            return null;
        }

        // order by ChaosValue descending
        pricesOrdered.AsSpan().Sort((lhs, rhs) => rhs.ChaosValue.CompareTo(lhs.ChaosValue));

        SkillProfitCalculationContext context = new(
            profitRequest,
            skill,
            exchangeRates,
            experienceFactorAddQualityByName,
            pricesOrdered
        );

        Dictionary<string, ProfitMargin> profitMargins = [];
        foreach (var recipe in recipes)
        {
            try
            {
                if (recipe.Execute(context) is { } margin)
                {
                    profitMargins[recipe.Name] = margin;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to evaluate recipe {Recipe} for skill {Skill}",
                    recipe.Name,
                    skill.Name
                );
            }
        }

        foreach (var (key, _) in profitMargins.Where(ProfitMarginViolatesRequestConstraint).ToList())
        {
            profitMargins.Remove(key);
        }

        if (profitMargins.Count == 0)
        {
            return null;
        }

        var (recipeName, bestProfit) = profitMargins.MaxBy(kvp => kvp.Value.GainMargin);

        return new(
            profitMargins,
            new(bestProfit.Buy, 0),
            new(bestProfit.Sell, bestProfit.ExperienceDelta),
            recipeName,
            bestProfit.GainMargin,
            skill
        );

        bool ProfitMarginViolatesRequestConstraint(KeyValuePair<string, ProfitMargin> kvp)
        {
            var (_, margin) = kvp;
            if (margin.ExperienceDelta < profitRequest.MinExperienceDelta)
            {
                return true;
            }

            if (profitRequest.MaxBuyPriceChaos is { } max && margin.Buy.Price > max)
            {
                return true;
            }

            if (profitRequest.MinSellPriceChaos is { } min && margin.Sell.Price < min)
            {
                return true;
            }

            return false;
        }
    }
}
