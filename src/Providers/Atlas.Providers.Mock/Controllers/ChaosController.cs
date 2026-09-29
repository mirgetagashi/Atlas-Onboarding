using Atlas.Providers.Mock.Models;
using Atlas.Providers.Mock.Services;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Providers.Mock.Controllers;

/// <summary>Switch simulated failures on and off while the system runs, to watch the retries.</summary>
[Route("chaos")]
public sealed class ChaosController : ApiControllerBase
{
    private readonly ChaosSettings _chaos;

    public ChaosController(ChaosSettings chaos) => _chaos = chaos;

    [HttpGet]
    public IActionResult Get() => Ok(_chaos.Snapshot());

    [HttpPut]
    public IActionResult Update(ChaosUpdate update) => Ok(_chaos.Update(update));
}
