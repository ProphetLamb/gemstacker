using System.Text;
using System.Text.Json.Serialization;
using GemLevelProtScraper;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using GemLevelProtScraper.Profit;
using GemLevelProtScraper.Profit.Recipes;
using GemLevelProtScraper.Skills;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization;
using MongoDB.Migration;
using ScrapeAAS;
using Yoh.Text.Json.NamingPolicies;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var apiKey = builder.Configuration["Authentication:ApiKey"];
var webShareKey = builder.Configuration["Authentication:WebShareApiKey"];

// Encoding
var provider = CodePagesEncodingProvider.Instance;
Encoding.RegisterProvider(provider);

// BSON Serialization
BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());

builder
    .Services.Configure<PoeDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDatabaseSettings"))
    .Configure<ProfitServiceOptions>(builder.Configuration.GetSection("ProfitService"))
    .AddMigrations()
    .AddSystemClock()
    .AddSingleton<SignalCompletionStorage>()
    .AddTransient<PoeDbRepository>()
    .AddTransient<PoeNinjaGemRepository>()
    .AddTransient<PoeNinjaCurrencyRepository>()
    .AddTransient<PoeRepository>()
    .AddTransient<SkillGemRepository>()
    .AddTransient<ProfitService>()
    .AddHostedService<ExchangeRateProvider>()
    .AddSingleton(sf => sf.GetServices<IHostedService>().OfType<ExchangeRateProvider>().First())
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddHostedService<PoeScraper>()
    .AddProfitRecipes()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .WithLongLivingServiceLifetime(ServiceLifetime.Scoped)
        .UseWebShareProxyProvider(o =>
            {
                o.ApiKey = webShareKey;
                o.CacheExpiration = TimeSpan.FromMinutes(30);
            }
        )
        .AddDataflow<DataflowSignal<PoeDbListCompleted>>()
        .AddDataflow<DataflowSignal<PoeLeagueListCompleted>>()
        .AddDataflow<DataflowSignal<PoeNinjaListCompleted>>()
    )
    .AddMemoryCache()
    .AddResponseCaching()
    .AddHttpContextAccessor()
    .AddOutputCache(o => o.AddBasePolicy(b => b.Cache().Expire(TimeSpan.FromSeconds(60))));

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
    {
        o.SerializerOptions.AllowTrailingCommas = true;
        o.SerializerOptions.PropertyNameCaseInsensitive = true;
        o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower;
        o.SerializerOptions.WriteIndented = true;
        o.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
        o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicies.SnakeCaseLower));
    }
);

builder
    .Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("token", o => o.ApiKey = apiKey);

var app = builder.Build();

app.UseResponseCaching();

app
    .MapGet(
        "exchange-rate-to-chaos",
        async (
            [FromServices] ExchangeRateProvider exchangeRateProvider,
            [FromServices] PoeRepository poeRepository,
            [FromQuery(Name = "currency")] string[] currencyNames,
            [FromQuery(Name = "league")] string? league = null,
            CancellationToken cancellationToken = default
        ) =>
        {
            var (leagueMode, failure) =
                await poeRepository.TryParseLeague(league, cancellationToken).ConfigureAwait(false);
            if (failure is not null)
            {
                return failure;
            }

            if (currencyNames.Length == 0)
            {
                return Results.Ok(new Dictionary<string, double>());
            }

            var col = await exchangeRateProvider.GetExchangeRatesAsync(cancellationToken).ConfigureAwait(false);
            var (exchangeRates, missingCurrencies) = currencyNames
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(CurrencyTypeName.Parse)
                .Distinct()
                .BiPartition(ValueTuple<PoeNinjaCurrencyExchangeRate?, string?> (c) =>
                    col.TryGetValue(leagueMode, c, out var exchangeRate) ? (exchangeRate, null) : (null, c.Value)
                );
            if (missingCurrencies.Count != 0)
            {
                return Results.BadRequest(
                    new
                    {
                        Error = "Invalid parameter value `currency`",
                        Message = $"No exchange rates found for currency {string.Join(", ", missingCurrencies)}",
                    }
                );
            }

            return Results.Ok(exchangeRates.ToDictionary(x => x.CurrencyTypeName, x => x.ChaosEquivalent));
        }
    )
    .CacheOutput(b =>
        b.Cache().Expire(TimeSpan.FromMinutes(30)).SetVaryByQuery("league", "from_currency", "to_currency")
    );

app
    .MapGet(
        "gem-profit",
        async (
            [FromServices] ProfitService profitService,
            [FromServices] PoeRepository poeRepository,
            [FromQuery(Name = "league")] string? league = null,
            [FromQuery(Name = "gem_name")] string? gemNameWildcard = null,
            [FromQuery(Name = "added_quality")] long addedQuality = 0,
            [FromQuery(Name = "min_sell_price_chaos")] double? minSellPriceChaos = null,
            [FromQuery(Name = "max_buy_price_chaos")] double? maxBuyPriceChaos = null,
            [FromQuery(Name = "min_experience_delta")] double? minExperienceDelta = null,
            [FromQuery(Name = "min_listing_count")] long minListingCount = 8,
            [FromQuery(Name = "disallowed_recipes")] string[]? disallowedRecipes = null,
            [FromQuery(Name = "items_count")] int itemsCount = 10,
            CancellationToken cancellationToken = default
        ) =>
        {
            var (leagueMode, failure) =
                await poeRepository.TryParseLeague(league, cancellationToken).ConfigureAwait(false);
            if (failure is not null)
            {
                return failure;
            }

            ProfitRequest request = new()
            {
                League = leagueMode,
                GemNameWildcard = gemNameWildcard,
                AddedQuality = addedQuality,
                MinSellPriceChaos = minSellPriceChaos,
                MaxBuyPriceChaos = maxBuyPriceChaos,
                MinExperienceDelta = minExperienceDelta,
                MinimumListingCount = minListingCount,
                DisallowedRecipes = disallowedRecipes is { Length: > 0 } ? disallowedRecipes.ToHashSet(StringComparer.OrdinalIgnoreCase) : null,
            };
            var result = await profitService.GetProfitAsync(request, cancellationToken).ConfigureAwait(false);

            return Results.Ok(itemsCount is <= 0 or >= 20000 ? result.ToAsyncEnumerable() : result.Take(itemsCount).ToAsyncEnumerable());
        }
    )
    .CacheOutput(b => b
        .Cache()
        .Expire(TimeSpan.FromMinutes(30))
        .SetVaryByQuery(
            "league",
            "gem_name",
            "added_quality",
            "min_sell_price_chaos",
            "max_buy_price_chaos",
            "min_experience_delta",
            "items_count"
        )
    );
app.Run();
