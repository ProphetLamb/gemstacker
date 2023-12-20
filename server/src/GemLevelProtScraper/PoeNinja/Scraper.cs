using System.Text.Json;
using System.Text.Json.Serialization;
using GemLevelProtScraper.Poe;
using MongoDB.Migration;
using ScrapeAAS;

namespace GemLevelProtScraper.PoeNinja;

internal sealed class PoeNinjaScraper(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await Task.WhenAll(
                ScrapeTradeLeague(scope, LeaugeMode.League | LeaugeMode.Softcore, stoppingToken),
                ScrapeTradeLeague(scope, LeaugeMode.League | LeaugeMode.Hardcore, stoppingToken),
                ScrapeTradeLeague(scope, LeaugeMode.League | LeaugeMode.HardcoreRuthless, stoppingToken),
                ScrapeTradeLeague(scope, LeaugeMode.Standard | LeaugeMode.Softcore, stoppingToken),
                ScrapeTradeLeague(scope, LeaugeMode.Standard | LeaugeMode.Hardcore, stoppingToken),
                ScrapeTradeLeague(scope, LeaugeMode.Standard | LeaugeMode.HardcoreRuthless, stoppingToken)
            ).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromHours(4), stoppingToken).ConfigureAwait(false);
            stoppingToken.ThrowIfCancellationRequested();
        }

        static async Task<PoeLeauge> GetCurrentPcLeauge(AsyncServiceScope scope, LeaugeMode league, CancellationToken stoppingToken)
        {
            var poeRepository = scope.ServiceProvider.GetRequiredService<PoeRepository>();
            var currentSoftcoreTradePcLeauge = await poeRepository.GetByModeAndRealmAsync(league, Realm.Pc, stoppingToken).ConfigureAwait(false);
            return currentSoftcoreTradePcLeauge ?? throw new InvalidOperationException($"No leauge for mode '{league}' found");
        }
        static async Task ScrapeTradeLeague(AsyncServiceScope scope, LeaugeMode leagueMode, CancellationToken stoppingToken)
        {
            var rootPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeNinjaList>>();
            var completedPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeNinjaListCompleted>>();
            var league = await GetCurrentPcLeauge(scope, leagueMode, stoppingToken).ConfigureAwait(false);
            await rootPublisher.PublishAsync(new(league.Mode, $"https://poe.ninja/api/data/itemoverview?league={league.Name}&type=SkillGem&language=en"), stoppingToken).ConfigureAwait(false);
            await completedPublisher.PublishAsync(new(league.Mode), stoppingToken).ConfigureAwait(false);
        }
    }
}

internal sealed class PoeNinjaSpider(IHttpClientFactory httpClientFactory, IDataflowPublisher<PoeNinjaApiLeaugeGemPrice> gemPublisher) : IDataflowHandler<PoeNinjaList>
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    public async ValueTask HandleAsync(PoeNinjaList root, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(root.GemPriceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var envelope = await JsonSerializer.DeserializeAsync<PoeNinjaApiGemPricesEnvelope>(content, _jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The poe.ninja API response is no PoeNinjaApiGemPricesEnvelope");
        await Task.WhenAll(
            envelope.Lines
                .Select(gemPrice => gemPublisher.PublishAsync(new(root.League, default, gemPrice)))
                .SelectTruthy(task => task.IsCompletedSuccessfully ? null : task.AsTask())
        ).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaSink(PoeNinjaRepository repository) : IDataflowHandler<PoeNinjaApiLeaugeGemPrice>
{
    public async ValueTask HandleAsync(PoeNinjaApiLeaugeGemPrice newGem, CancellationToken cancellationToken = default)
    {
        _ = await repository.AddOrUpdateAsync(newGem.Leauge, newGem.Price, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaCleanup(PoeNinjaRepository repository, ISystemClock clock) : IDataflowHandler<PoeNinjaList>, IDataflowHandler<PoeNinjaListCompleted>
{
    private readonly Dictionary<LeaugeMode, DateTime> _startTimestamp = new();

    public ValueTask HandleAsync(PoeNinjaList message, CancellationToken cancellationToken = default)
    {
        _startTimestamp[message.League] = clock.UtcNow.UtcDateTime;
        return default;
    }

    public async ValueTask HandleAsync(PoeNinjaListCompleted message, CancellationToken cancellationToken = default)
    {
        var endTs = clock.UtcNow.UtcDateTime;
        if (!_startTimestamp.TryGetValue(message.League, out var startTs) || startTs > endTs)
        {
            return;
        }
        _ = _startTimestamp.Remove(message.League);

        var oldestTs = endTs.Add(TimeSpan.FromSeconds(-1));

        _ = await repository.RemoveOlderThanAsync(oldestTs, cancellationToken).ConfigureAwait(false);
    }
}
