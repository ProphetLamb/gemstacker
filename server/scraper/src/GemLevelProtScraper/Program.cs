using GemLevelProtScraper;
using GemLevelProtScraper.Migrations;
using MongoDB.Bson.Serialization;
using ScrapeAAS;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());

builder.Services
    .Configure<PoeNinjaDatabaseSettings>(builder.Configuration.GetSection("Database:PoeNinjaDatabaseSettings"))
    .Configure<PoeDbDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDbDatabaseSettings"))
    .AddMigrations()
    // .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbSink>()
    );

var app = builder.Build();
app.Run();
