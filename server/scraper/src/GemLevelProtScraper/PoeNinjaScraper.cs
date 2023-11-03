
using System.Collections.Immutable;
using System.Text.Json;
using GemLevelProtScraper.Migrations;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ScrapeAAS;

namespace GemLevelProtScraper;

public sealed record PoeNinjaDatabaseSettings : IOptions<PoeNinjaDatabaseSettings>, IDatabaseMigratable
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public required string GemPriceCollectionName { get; init; }

    PoeNinjaDatabaseSettings IOptions<PoeNinjaDatabaseSettings>.Value => this;

    public DatabaseMigrationSettings GetMigrationSettings()
    {
        return new()
        {
            ConnectionString = ConnectionString,
            Database = new(DatabaseAlias.PoeDb, DatabaseName),
            MirgrationStateCollectionName = "DATABASE_MIGRATIONS"
        };
    }
}

internal sealed record PoeNinjaRoot(string GemPriceUrl);

internal sealed record PoeNinjaApiSparkLine(ImmutableArray<decimal?> Data, decimal TotalChange);

[BsonIgnoreExtraElements]
internal sealed record PoeNinjaApiGemPrice(
    string Name,
    string Icon,
    PoeNinjaApiSparkLine SparkLine,
    PoeNinjaApiSparkLine LowConfidenceSparkLine,
    bool Corrupted,
    long GemLevel,
    long GemQuality,
    decimal ChaosValue,
    decimal DivineValue,
    long ListingCount
);

internal sealed record PoeNinjaApiGemPricesEnvelope(
    ImmutableArray<PoeNinjaApiGemPrice> Lines
);

internal sealed class PoeNinjaScraper(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var rootPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeNinjaRoot>>();
            await rootPublisher.PublishAsync(new("https://poe.ninja/api/data/itemoverview?league=Crucible&type=SkillGem&language=en"), stoppingToken).ConfigureAwait(false);

            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}

internal sealed class PoeNinjaSpider(IHttpClientFactory httpClientFactory, IDataflowPublisher<PoeNinjaApiGemPrice> gemPublisher) : IDataflowHandler<PoeNinjaRoot>
{
    public async ValueTask HandleAsync(PoeNinjaRoot root, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(root.GemPriceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
        var envelope = await JsonSerializer.DeserializeAsync<PoeNinjaApiGemPricesEnvelope>(content, jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The poe.ninja API response is no PoeNinjaApiGemPricesEnvelope");
        await Task.WhenAll(
            envelope.Lines
                .Select(gemPrice => gemPublisher.PublishAsync(gemPrice))
                .SelectTruthy(task => !task.IsCompletedSuccessfully ? task.AsTask() : null)
        ).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaSink : IDataflowHandler<PoeNinjaApiGemPrice>
{
    private readonly IMongoCollection<PoeNinjaApiGemPrice> _gemPriceCollection;

    public PoeNinjaSink(IOptions<PoeNinjaDatabaseSettings> databaseSettingsOptions)
    {
        var databaseSettings = databaseSettingsOptions.Value;
        MongoClient mongoClient = new(databaseSettings.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseSettings.DatabaseName);
        _gemPriceCollection = mongoDatabase.GetCollection<PoeNinjaApiGemPrice>(databaseSettings.GemPriceCollectionName);
    }

    private async Task EnsureIndexCreated(CancellationToken cancellationToken)
    {
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPrice> builder = new();
        var combinedIndex = builder.Combine(
            builder.Hashed(nameof(PoeNinjaApiGemPrice.Name)),
            builder.Ascending(nameof(PoeNinjaApiGemPrice.GemLevel)),
            builder.Descending(nameof(PoeNinjaApiGemPrice.GemQuality))
        );
        CreateIndexModel<PoeNinjaApiGemPrice> model = new(combinedIndex, new()
        {
            Unique = true,
            Name = "GemIdentifier"
        });
        _ = await _gemPriceCollection.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask HandleAsync(PoeNinjaApiGemPrice newGemPrice, CancellationToken cancellationToken = default)
    {
        _ = await _gemPriceCollection.FindOneAndReplaceAsync(
            gemPrice
                => gemPrice.GemLevel == newGemPrice.GemLevel
                && gemPrice.GemQuality == newGemPrice.GemQuality
                && gemPrice.Name == newGemPrice.Name,
            newGemPrice,
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }
}
