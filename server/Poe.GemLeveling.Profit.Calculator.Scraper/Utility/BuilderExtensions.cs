using System;
using DotnetSpider;
using Microsoft.Extensions.DependencyInjection;

namespace Poe.GemLeveling.Profit.Calculator.Scraper.Utility;

public static class BuilderExtensions
{
    public static Builder AddSingleton<TService>(this Builder builder, Func<TService> implementationFactory)
    {
        builder.ConfigureServices((b, services) => services.Add(new ServiceDescriptor(typeof(TService), implementationFactory)));
        return builder;
    }
    public static Builder AddSingleton<TService, TImplementation>(this Builder builder)
    {
        builder.ConfigureServices((b, services) => services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton)));
        return builder;
    }
}
