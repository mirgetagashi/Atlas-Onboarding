using Atlas.Onboarding.UseCases.Dtos.Responses;
using Atlas.Onboarding.UseCases.Services;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Onboarding.Api.Controllers;

/// <summary>Market rules, so the mobile app can build its screens from configuration.</summary>
[Route("markets")]
public sealed class MarketsController : ApiControllerBase
{
    private readonly IMarketService _markets;

    public MarketsController(IMarketService markets) => _markets = markets;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MarketRequirementsResponse>>(StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(_markets.GetAll());

    [HttpGet("{market}/requirements")]
    [ProducesResponseType<MarketRequirementsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetRequirements(string market) =>
        FromResult(_markets.GetRequirements(market), Ok);
}
