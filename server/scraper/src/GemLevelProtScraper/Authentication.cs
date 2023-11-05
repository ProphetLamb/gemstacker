using System.Collections.Immutable;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GemLevelProtScraper;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions, IOptions<ApiKeyAuthenticationOptions>
{
    public string? ApiKey { get; set; }

    public ApiKeyAuthenticationOptions Value => this;
}

public sealed partial class ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder, clock)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization;
        var authorizationResults = authorizationHeader
            .SelectTruthy(s => s)
            .Select(TryAuthenticate)
            .ToImmutableArray();
        if (authorizationResults.FirstOrDefault(a => a.Succeeded) is { } success)
        {
            return Task.FromResult(success);
        }
        if (authorizationResults.FirstOrDefault(a => a.Failure is { }) is { } failure)
        {
            return Task.FromResult(failure);
        }
        return Task.FromResult(AuthenticateResult.NoResult());

        AuthenticateResult TryAuthenticate(string authorizationHeader)
        {
            if (MatchAuthenticationHeader().Match(authorizationHeader) is not { Success: true } match)
            {
                return AuthenticateResult.NoResult();
            }

            var bytes = Convert.FromBase64String(match.Groups[1].Value);
            var text = Encoding.UTF8.GetString(bytes);
            if (!text.AsSpan().Equals(Options.ApiKey, StringComparison.Ordinal))
            {
                return AuthenticateResult.Fail("The requested token is not a valid api key");
            }

            ClaimsIdentity identity = new("api-authentication");
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, "token");

            return AuthenticateResult.Success(ticket);
        }
    }

    [GeneratedRegex(@"token\s+(.+)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex MatchAuthenticationHeader();
}
