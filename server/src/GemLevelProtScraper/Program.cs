using System.Text.Json.Serialization;
using GemLevelProtScraper;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization;
using MongoDB.Migration;
using ScrapeAAS;
using Yoh.Text.Json.NamingPolicies;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());

var apiKey = builder.Configuration["Authentication:ApiKey"];
var webShareKey = builder.Configuration["Authentication:WebShareApiKey"];

builder.Services
    .Configure<PoeNinjaDatabaseSettings>(builder.Configuration.GetSection("Database:PoeNinjaDatabaseSettings"))
    .Configure<PoeDbDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDbDatabaseSettings"))
    .Configure<PoeDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDatabaseSettings"))
    .AddMigrations()
    .AddSystemClock()
    .AddSingleton<SignalTaskStorage>()
    .AddTransient<PoeDbRepository>()
    .AddTransient<PoeNinjaRepository>()
    .AddTransient<PoeRepository>()
    .AddTransient<ProfitService>()
    .AddHostedService<PoeNinjaScraper>()
    // .AddHostedService<PoeDbScraper>()
    .AddHostedService<PoeScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .WithLongLivingServiceLifetime(ServiceLifetime.Singleton)
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaCleanup>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbSink>()
        .AddDataFlow<PoeLeaguesSpider>()
        .AddDataFlow<PoeSink>()
        .AddDataFlow<DataflowSignal<PoeLeagueListCompleted>>()
        .Use(ScrapeAASRole.ProxyProvider, s => s.AddWebShareProxyProvider(o => o.ApiKey = webShareKey))
    )
    .AddHttpContextAccessor()
    .AddMemoryCache()
    .AddOutputCache(o =>
    {
        o.AddBasePolicy(b => b.Cache().Expire(TimeSpan.FromSeconds(10)));
        o.AddPolicy("expire30min", b => b.Cache().Expire(TimeSpan.FromMinutes(30)));
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.AllowTrailingCommas = true;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower;
    o.SerializerOptions.WriteIndented = true;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services
    .AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("token", o => o.ApiKey = apiKey);

var app = builder.Build();

app
    .MapGet("gem-profit", async (
        [FromServices] ProfitService profitService,
        [FromServices] PoeRepository poeRepository,
        [FromQuery(Name = "league")] string league = "",
        [FromQuery(Name = "gem_name")] string gemNameWindcard = "*",
        [FromQuery(Name = "min_sell_price_chaos")] decimal? minSellPriceChaos = null,
        [FromQuery(Name = "max_buy_price_chaos")] decimal? maxBuyPriceChaos = null,
        [FromQuery(Name = "min_experience_delta")] decimal? minExperienceDelta = null,
        [FromQuery(Name = "min_listing_count")] int minListingCount = 4,
        CancellationToken cancellationToken = default
    ) =>
    {
        var baseLeague = await poeRepository.GetByModeAndRealmAsync(LeagueMode.League, Realm.Pc, cancellationToken).ConfigureAwait(false);
        if (baseLeague is null)
        {
            return Results.BadRequest(new
            {
                Error = "Invalid parameter value `league`",
                Message = $"Failed to parse the league value `{league}`"
            });
        }

        if (!LeagueModeHelper.TryParse(league, baseLeague.Name, out var leagueMode))
        {
            return Results.BadRequest(new
            {
                Error = "Invalid parameter value `league`",
                Message = $"Failed to parse the league value `{league}`"
            });
        }

        ProfitRequest request = new()
        {
            League = leagueMode,
            GemNameWindcard = gemNameWindcard,
            MinSellPriceChaos = minSellPriceChaos,
            MaxBuyPriceChaos = maxBuyPriceChaos,
            MinExperienceDelta = minExperienceDelta,
            MinimumListingCount = minListingCount
        };
        var data = await profitService.GetProfitAsync(request, cancellationToken).ConfigureAwait(false);
        return Results.Ok(data);
    }); //.CacheOutput("expire30min")
app.Run();

