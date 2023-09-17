using System;
using System.Collections.Immutable;
using System.Text.Json;
using DotnetSpider.Proxy;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Poe.GemLeveling.Profit.Calculator.Scraper;

public readonly record struct GemDescriptor(string Name, long DropLevel, string ItemClassesName, string? IconPath, string? SkillIconPath, ImmutableArray<string> GemTags);

public sealed record GemDescriptorsResult(ImmutableDictionary<string, GemDescriptor> Descriptors);

public sealed class GemDescriptorService
{
    private readonly HttpMessageHandlerBuilder _httpHandlerBuilder;
    private readonly IProxyService _proxyService;
    private readonly PoedbOptions _options;
    private readonly ILogger<GemDescriptorService> _logger;

    public GemDescriptorService(HttpMessageHandlerBuilder httpHandlerBuilder, IProxyService proxyService, IOptions<PoedbOptions> options, ILogger<GemDescriptorService> logger)
    {
        _httpHandlerBuilder = httpHandlerBuilder;
        _proxyService = proxyService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GemDescriptorsResult> GetDescriptors(CancellationToken cancellationToken = default)
    {
        using HttpClient httpClient = new(await GetEphemeralProxyMessageHandler());
        var response = await httpClient.GetAsync($"{_options.PoedbApiUrl}/api/ActiveSkills", cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var gemBriefResponse = JsonSerializer.Deserialize<GemBriefResponse>(content);
        if (gemBriefResponse.Data.IsDefaultOrEmpty)
        {
            _logger.LogError("GemBriefResponse is empty");
            throw new InvalidOperationException("GemBriefResponse is empty");
        }

        var descriptors = ImmutableDictionary.Create<string, GemDescriptor>();
        foreach (var item in gemBriefResponse.Data)
        {
            if (!long.TryParse(item.DropLevel, out _) || string.IsNullOrEmpty(item.Name) || string.IsNullOrEmpty(item.ItemClassesName))
            {
                _logger.LogWarning("GemBriefResponseItem is invalid: {Item}", item);
                continue;
            }
            GemDescriptor descriptor = new(
                item.Name,
                long.Parse(item.DropLevel),
                item.ItemClassesName,
                item.IconPath,
                item.SkillIconPath,
                item.GemTags.IsDefaultOrEmpty
                    ? ImmutableArray<string>.Empty
                    : item.GemTags.Where(tag => !string.IsNullOrEmpty(tag)).ToImmutableArray()!
            );
            var url = $"{_options.PoedbApiUrl}/us/{NormalizeGemName(descriptor.Name)}";
            if (!descriptors.TryAdd(url, descriptor))
            {
                _logger.LogWarning("GemBriefResponseItem is name {Url} duplicated: {Item}", url, item);
            }
        }
        return new(descriptors);

        string NormalizeGemName(string name)
        {
            return name.Replace(" ", "_");
        }
    }

    private async Task<HttpMessageHandler> GetEphemeralProxyMessageHandler()
    {
        var proxy = await _proxyService.GetAsync(30);
        _httpHandlerBuilder.Name = $"DotnetSpider_Proxy_{proxy}";
        return _httpHandlerBuilder.Build();
    }

    readonly record struct GemBriefResponse(string Size, ImmutableArray<GemBriefResponseItem> Data, long Status);
    readonly record struct GemBriefResponseItem(string Name, string DropLevel, string ItemClassesName, string? IconPath, string? SkillIconPath, ImmutableArray<string?> GemTags);
}
