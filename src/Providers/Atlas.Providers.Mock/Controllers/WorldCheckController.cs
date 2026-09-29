using Atlas.Providers.Mock.Filters;
using Atlas.Providers.Mock.Models;
using Atlas.Providers.Mock.Services;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Providers.Mock.Controllers;

/// <summary>Fake Refinitiv World-Check: sanctions and PEP screening.</summary>
[Route("worldcheck/v1/screenings")]
[ServiceFilter(typeof(ChaosFilter))]
public sealed class WorldCheckController : ApiControllerBase
{
    private readonly MockProviderRules _rules;

    public WorldCheckController(MockProviderRules rules) => _rules = rules;

    [HttpPost]
    [ProducesResponseType<ScreeningResponse>(StatusCodes.Status200OK)]
    public IActionResult Screen(ScreeningRequest request) => Ok(_rules.Screen(request));
}
