using Atlas.Providers.Mock.Filters;
using Atlas.Providers.Mock.Models;
using Atlas.Providers.Mock.Services;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Providers.Mock.Controllers;

/// <summary>Fake IDNow: document authenticity + face match.</summary>
[Route("idnow/v1/identifications")]
[ServiceFilter(typeof(ChaosFilter))]
public sealed class IdNowController : ApiControllerBase
{
    private readonly MockProviderRules _rules;

    public IdNowController(MockProviderRules rules) => _rules = rules;

    [HttpPost]
    [ProducesResponseType<IdentificationResponse>(StatusCodes.Status200OK)]
    public IActionResult Identify(IdentificationRequest request) => Ok(_rules.Identify(request));
}
