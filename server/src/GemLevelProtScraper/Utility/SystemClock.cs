

namespace GemLevelProtScraper;

public class SystemClock : Microsoft.AspNetCore.Authentication.ISystemClock, Microsoft.Extensions.Internal.ISystemClock, Microsoft.Owin.Infrastructure.ISystemClock, MongoDB.Migration.ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public static class SystemClockExtensions
{
    public static IServiceCollection AddSystemClock(this IServiceCollection services)
    {
        return services
            .AddTransient<Microsoft.AspNetCore.Authentication.ISystemClock, SystemClock>()
            .AddTransient<Microsoft.Extensions.Internal.ISystemClock, SystemClock>()
            .AddTransient<Microsoft.Owin.Infrastructure.ISystemClock, SystemClock>()
            .AddTransient<MongoDB.Migration.ISystemClock, SystemClock>();
    }
}
