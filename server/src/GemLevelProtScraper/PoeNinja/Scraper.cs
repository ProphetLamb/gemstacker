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
                ScrapeTradeLeague(scope, LeagueMode.League | LeagueMode.Softcore, stoppingToken),
                ScrapeTradeLeague(scope, LeagueMode.League | LeagueMode.Hardcore, stoppingToken),
                ScrapeTradeLeague(scope, LeagueMode.League | LeagueMode.HardcoreRuthless, stoppingToken),
                ScrapeTradeLeague(scope, LeagueMode.Standard | LeagueMode.Softcore, stoppingToken),
                ScrapeTradeLeague(scope, LeagueMode.Standard | LeagueMode.Hardcore, stoppingToken),
                ScrapeTradeLeague(scope, LeagueMode.Standard | LeagueMode.HardcoreRuthless, stoppingToken)
            ).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromHours(4), stoppingToken).ConfigureAwait(false);
            stoppingToken.ThrowIfCancellationRequested();
        }

        static async Task<PoeLeague> GetCurrentPcLeague(AsyncServiceScope scope, LeagueMode league, CancellationToken stoppingToken)
        {
            var poeRepository = scope.ServiceProvider.GetRequiredService<PoeRepository>();
            var currentSoftcoreTradePcLeague = await poeRepository.GetByModeAndRealmAsync(league, Realm.Pc, stoppingToken).ConfigureAwait(false);
            return currentSoftcoreTradePcLeague ?? throw new InvalidOperationException($"No league for mode '{league}' found");
        }
        static async Task ScrapeTradeLeague(AsyncServiceScope scope, LeagueMode leagueMode, CancellationToken stoppingToken)
        {
            var rootPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeNinjaList>>();
            var completedPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeNinjaListCompleted>>();
            var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();
            var league = await GetCurrentPcLeague(scope, leagueMode, stoppingToken).ConfigureAwait(false);
            await rootPublisher.PublishAsync(new(clock.UtcNow, league, "https://poe.ninja/api"), stoppingToken).ConfigureAwait(false);
            await completedPublisher.PublishAsync(new(clock.UtcNow, league), stoppingToken).ConfigureAwait(false);
        }
    }
}

internal sealed class PoeNinjaSpider(IHttpClientFactory httpClientFactory, IDataflowPublisher<PoeNinjaApiLeagueGemPrice> gemPublisher) : IDataflowHandler<PoeNinjaList>
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    public async ValueTask HandleAsync(PoeNinjaList root, CancellationToken cancellationToken = default)
    {
        var url = $"{root.ApiUrl}/data/itemoverview?league={root.League.Name}&type=SkillGem&language=en";
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var envelope = await JsonSerializer.DeserializeAsync<PoeNinjaApiGemPricesEnvelope>(content, _jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The poe.ninja API response is no PoeNinjaApiGemPricesEnvelope");
        await gemPublisher.PublishAllAsync(
            envelope.Lines.Select(gemPrice => new PoeNinjaApiLeagueGemPrice(root.League.Mode, default, gemPrice)),
            cancellationToken
        ).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaCleanup(PoeNinjaRepository repository) : IDataflowHandler<PoeNinjaList>, IDataflowHandler<PoeNinjaListCompleted>
{
    private readonly Dictionary<LeagueMode, DateTime> _startTimestamp = new();

    public ValueTask HandleAsync(PoeNinjaList message, CancellationToken cancellationToken = default)
    {
        lock (_startTimestamp)
        {
            _startTimestamp[message.League.Mode] = message.Timestamp.UtcDateTime;
        }
        return default;
    }

    public async ValueTask HandleAsync(PoeNinjaListCompleted message, CancellationToken cancellationToken = default)
    {
        var endTs = message.Timestamp.UtcDateTime;
        var mode = message.League.Mode;
        lock (_startTimestamp)
        {
            if (!_startTimestamp.TryGetValue(mode, out var startTs) || startTs > endTs)
            {
                return;
            }
            _ = _startTimestamp.Remove(mode);
        }
        var oldestTs = endTs.Add(TimeSpan.FromSeconds(-1));

        _ = await repository.RemoveOlderThanAsync(mode, oldestTs, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaSink : IDataflowHandler<PoeNinjaApiLeagueGemPrice>
{
    private readonly PoeNinjaRepository _poeNinjaRepository;

    public PoeNinjaSink(PoeNinjaRepository poeNinjaRepository)
    {
        _poeNinjaRepository = poeNinjaRepository ?? throw new ArgumentNullException(nameof(poeNinjaRepository));
    }

    public async ValueTask HandleAsync(PoeNinjaApiLeagueGemPrice newGem, CancellationToken cancellationToken = default)
    {
        await _poeNinjaRepository.AddOrUpdateAsync(newGem.League, newGem.Price, cancellationToken).ConfigureAwait(false);
    }
}
