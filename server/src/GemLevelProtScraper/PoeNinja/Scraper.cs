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
            var scope = serviceScopeFactory.CreateAsyncScope();
            await using var scope1 = scope.ConfigureAwait(false);
            await Task.WhenAll(
                // ReSharper disable once AccessToDisposedClosure
                LeagueModeHelper.WellknownLeagues.Select(league => ScrapeTradeLeague(scope, league, stoppingToken))
            ).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken).ConfigureAwait(false);
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
            await rootPublisher.PublishAsync(new(clock.UtcNow, league, "https://poe.ninja"), stoppingToken).ConfigureAwait(false);
            await completedPublisher.PublishAsync(new(clock.UtcNow, league), stoppingToken).ConfigureAwait(false);
        }
    }
}

internal sealed class PoeNinjaSkillGemSpider(IStaticPageLoader pageLoader, IDataflowPublisher<PoeNinjaApiGemPriceEnvelope> gemPublisher) : IDataflowHandler<PoeNinjaList>
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    public async ValueTask HandleAsync(PoeNinjaList root, CancellationToken cancellationToken = default)
    {
        Uri url = new($"{root.Url}/api/data/itemoverview?league={root.League.Name}&type=SkillGem&language=en");
        var response = await pageLoader.LoadAsync(url, cancellationToken).ConfigureAwait(false);
        var content = await response.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var content1 = content.ConfigureAwait(false);
        var envelope = await JsonSerializer.DeserializeAsync<PoeNinjaApiGemResponse>(content, _jsonOptions, cancellationToken).ConfigureAwait(false)
                       ?? throw new InvalidOperationException("The poe.ninja API response is no PoeNinjaApiGemPriceEnvelope");
        await gemPublisher.PublishAllAsync(
            envelope.Lines.Select(price => new PoeNinjaApiGemPriceEnvelope(root.League.Mode, default, price)),
            cancellationToken
        ).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaExchangeSpider(IStaticPageLoader pageLoader, IDataflowPublisher<PoeNinjaApiCurrencyPriceEnvelope> currencyPublisher) : IDataflowHandler<PoeNinjaList>
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    public async ValueTask HandleAsync(PoeNinjaList root, CancellationToken cancellationToken = default)
    {
        Uri url = new($"{root.Url}/poe1/api/economy/exchange/current/overview?league={root.League.Name}&type=Currency&language=en");
        var response = await pageLoader.LoadAsync(url, cancellationToken).ConfigureAwait(false);
        var content = await response.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var content1 = content.ConfigureAwait(false);
        var envelope = await JsonSerializer.DeserializeAsync<PoeNinjaApiExchangeResponse>(content, _jsonOptions, cancellationToken).ConfigureAwait(false)
                       ?? throw new InvalidOperationException("The poe.ninja API response is no PoeNinjaApiExchangeResponse");
        var itemByKey = envelope.Items.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        await currencyPublisher.PublishAllAsync(
            envelope.Lines.Select(line => new PoeNinjaApiCurrencyPriceEnvelope(root.League.Mode, default, LineToPrice(line))),
            cancellationToken
        ).ConfigureAwait(false);
        return;

        PoeNinjaApiCurrencyPrice LineToPrice(PoeNinjaApiExchangeLine line)
        {
            var item = itemByKey.GetValueOrDefault(line.Id);
            var rate = line.MaxVolumeCurrency == envelope.Core.Primary ? 1 : envelope.Core.Rates.GetValueOrDefault(line.MaxVolumeCurrency, 1);
            return new(item?.Name ?? "", null, null, null, null, line.PrimaryValue / rate, null, null, line.Id);
        }
    }
}

internal sealed class PoeNinjaIncursionTempleSpider(IStaticPageLoader pageLoader, IDataflowPublisher<PoeNinjaApiIncursionTemplePriceEnvelope> templePublisher) : IDataflowHandler<PoeNinjaList>
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    public async ValueTask HandleAsync(PoeNinjaList root, CancellationToken cancellationToken = default)
    {
        Uri url = new($"{root.Url}/poe1/api/economy/stash/current/item/overview?league={root.League.Mode}&type=IncursionTemple");
        var response = await pageLoader.LoadAsync(url, cancellationToken).ConfigureAwait(false);
        var content = await response.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var content1 = content.ConfigureAwait(false);
        var envelope = await JsonSerializer.DeserializeAsync<PoeNinjaApiGemResponse>(content, _jsonOptions, cancellationToken).ConfigureAwait(false)
                       ?? throw new InvalidOperationException("The poe.ninja API response is no PoeNinjaApiIncursionTemplePriceEnvelope");
        await templePublisher.PublishAllAsync(
            envelope.Lines.Select(price => new PoeNinjaApiIncursionTemplePriceEnvelope(root.League.Mode, default, price)),
            cancellationToken
        ).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaCleanup(PoeNinjaGemRepository gemRepository, PoeNinjaCurrencyRepository currencyRepository) : IDataflowHandler<PoeNinjaList>, IDataflowHandler<PoeNinjaListCompleted>
{
    private readonly Dictionary<LeagueMode, DateTime> _startTimestamp = [];

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
        DateTime startTs;
        lock (_startTimestamp)
        {
            if (!_startTimestamp.TryGetValue(mode, out startTs) || startTs > endTs)
            {
                return;
            }
            _ = _startTimestamp.Remove(mode);
        }
        var oldestTs = startTs.Add(TimeSpan.FromSeconds(-1));

        _ = await Task.WhenAll(
            gemRepository.RemoveOlderThanAsync(mode, oldestTs, cancellationToken),
            currencyRepository.RemoveOlderThanAsync(mode, oldestTs, cancellationToken)
        ).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaSink(PoeNinjaGemRepository gemRepository, PoeNinjaCurrencyRepository currencyRepository, PoeNinjaIncursionTempleRepository templeRepository) : IDataflowHandler<PoeNinjaApiGemPriceEnvelope>, IDataflowHandler<PoeNinjaApiCurrencyPriceEnvelope>, IDataflowHandler<PoeNinjaApiIncursionTemplePriceEnvelope>
{
    public async ValueTask HandleAsync(PoeNinjaApiGemPriceEnvelope newGem, CancellationToken cancellationToken = default)
    {
        await gemRepository.AddOrUpdateAsync(newGem.League, newGem.Price, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask HandleAsync(PoeNinjaApiCurrencyPriceEnvelope newCurrency, CancellationToken cancellationToken = default)
    {
        await currencyRepository.AddOrUpdateAsync(newCurrency.League, newCurrency.Price, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask HandleAsync(
        PoeNinjaApiIncursionTemplePriceEnvelope newTemple,
        CancellationToken cancellationToken = default
    )
    {
        await templeRepository.AddOrUpdateAsync(newTemple.League, newTemple.Price, cancellationToken).ConfigureAwait(false);
    }
}
