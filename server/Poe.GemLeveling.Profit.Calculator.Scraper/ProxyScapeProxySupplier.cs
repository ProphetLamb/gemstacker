using DotnetSpider.Proxy;
using Microsoft.Extensions.Options;

namespace Poe.GemLeveling.Profit.Calculator.Scraper;

public sealed class ProxyScapeProxySupplierOptions : IOptions<ProxyScapeProxySupplierOptions>
{
    public string ProxySupplierUrl { get; set; } = "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=500&country=all&ssl=any&anonymity=all";
    public ProxyScapeProxySupplierOptions Value => this;
}

public sealed class ProxyScapeProxySupplier : IProxySupplier
{
    private ProxyScapeProxySupplierOptions _options;
    private IHttpClientFactory _httpClientFactory;

    public ProxyScapeProxySupplier(IOptions<ProxyScapeProxySupplierOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<Uri>> GetProxiesAsync()
    {
        using var client = _httpClientFactory.CreateClient("ProxyScape");
        var response = await client.GetAsync(_options.ProxySupplierUrl);
        var content = await response.Content.ReadAsStringAsync();
        var proxies = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var adresses = proxies.Select(x => new Uri(x));
        return adresses;
    }
}
