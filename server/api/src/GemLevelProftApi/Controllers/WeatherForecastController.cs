using Microsoft.AspNetCore.Mvc;

namespace GemLevelProftApi.Controllers;

public sealed record GemProfitRequest(
    [property: FromQuery("gem_name")]
    string? GemName = "*",
    [property: FromQuery("min_sell_price_chaos")]
    long? MinSellPriceChaos = null,
    [property: FromQuery("max_buy_price_chaos")]
    long? MaxBuyPriceChaos = null,
    [property: FromQuery("min_experience_delta")]
    long? MinExperienceDelta = null,
    [property: FromQuery("items_offset")]
    long ItemsOffset = 0,
    [property: FromQuery("items_count")]
    long ItemsCount = 10
);

public sealed record GemProfitResponse(
    Dictionary<string, GemProfitLevelDelta> Data
);

public sealed record GemProfitLevelDelta(
    GemProfitLevelInfo Min,
    GemProfitLevelInfo Max,
    long GainMargin
);

public sealed record GemProfitLevelInfo(
    long Price,
    long Level,
    long Exp
);

[ApiController]
[Route("[controller]")]
public class GemProfitController : ControllerBase
{
    private readonly ILogger<GemProfitController> _logger;

    public GemProfitController(ILogger<GemProfitController> logger)
    {
        _logger = logger;
    }

    [HttpGet(), ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<GemProfitResponse>> Get([FromQuery] GemProfitRequest request)
    {
        return new(new());
    }
}
