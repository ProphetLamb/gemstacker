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
    .AddTransient<PoeDbRepository>()
    .AddTransient<PoeNinjaRepository>()
    .AddTransient<ProfitService>()
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbSink>()
        .AddDataFlow<PoeLeaguesSpider>()
        .AddDataFlow<PoeSink>()
        .Use(ScrapeAASRole.ProxyProvider, s => s.AddWebShareProxyProvider(o => o.ApiKey = webShareKey))
    )
    .AddHttpContextAccessor()
    .AddMemoryCache()
    .AddOutputCache(o =>
    {
        o.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(10)));
        o.AddPolicy("expire30min", b => b.Cache().Expire(TimeSpan.FromMinutes(30)));
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.AllowTrailingCommas = true;
    o.SerializerOptions.PropertyNameCaseInsensitive = false;
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower;
    o.SerializerOptions.WriteIndented = true;
});

builder.Services
    .AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("token", o => o.ApiKey = apiKey);

var app = builder.Build();

app
    .MapGet("gem-profit", async (
        [FromServices] ProfitService profitService,
        [FromQuery(Name = "gem_name")] string gemNameWindcard = "*",
        [FromQuery(Name = "min_sell_price_chaos")] decimal? minSellPriceChaos = null,
        [FromQuery(Name = "max_buy_price_chaos")] decimal? maxBuyPriceChaos = null,
        [FromQuery(Name = "min_experience_delta")] decimal? minExperienceDelta = null,
        [FromQuery(Name = "min_listing_count")] int minListingCount = 4,
        CancellationToken cancellationToken = default
    ) =>
    {
        ProfitRequest request = new()
        {
            GemNameWindcard = gemNameWindcard,
            MinSellPriceChaos = minSellPriceChaos,
            MaxBuyPriceChaos = maxBuyPriceChaos,
            MinExperienceDelta = minExperienceDelta,
            MinimumListingCount = minListingCount
        };
        var data = await profitService.GetProfitAsync(request, cancellationToken).ConfigureAwait(false);
        return data;
    }); //.CacheOutput("expire30min")
app.Run();

