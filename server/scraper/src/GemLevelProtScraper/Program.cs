using GemLevelProtScraper;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using MongoDB.Bson.Serialization;
using MongoDB.Migration;
using ScrapeAAS;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());

var apiKey = builder.Configuration["Authentication:ApiKey"];

builder.Services
    .Configure<PoeNinjaDatabaseSettings>(builder.Configuration.GetSection("Database:PoeNinjaDatabaseSettings"))
    .Configure<PoeDbDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDbDatabaseSettings"))
    .AddMigrations()
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbSink>()
    )
    .AddHttpContextAccessor();

builder.Services.AddControllers();

builder.Services
    .AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("token", o => o.ApiKey = apiKey);

var app = builder.Build();

app
    .MapGet("gem-profit", () =>
    {
        ;
    });
app.Run();
