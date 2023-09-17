using DotnetSpider;
using DotnetSpider.Downloader;
using DotnetSpider.Proxy;
using DotnetSpider.RabbitMQ;
using DotnetSpider.Scheduler;
using DotnetSpider.Scheduler.Component;
using Microsoft.Extensions.Hosting;
using Poe.GemLeveling.Profit.Calculator.Scraper;
using Poe.GemLeveling.Profit.Calculator.Scraper.Utility;

var builder = Builder.CreateBuilder<GemSpider>(o =>
{
    o.RequestedQueueCount = 1024 * 16;
    o.Speed = 0.5;
    o.RefreshProxy = 10;
});

builder
    .UseDownloader<HttpClientDownloader>()
    .UseProxy<ProxyScapeProxySupplier, DefaultProxyValidator>(o => { })
    .AddSingleton<GemDescriptorService, GemDescriptorService>()
    .UseRabbitMQ()
    .IgnoreServerCertificateError()
    .UseQueueDistinctBfsScheduler<HashSetDuplicateRemover>();

await builder.StartAsync();