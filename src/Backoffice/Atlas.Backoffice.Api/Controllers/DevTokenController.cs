using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Backoffice.Api.Dtos.Responses;
using Atlas.Backoffice.Api.UseCases.Services;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Backoffice.Api.Controllers;

/// <summary>DEVELOPMENT ONLY login (returns 404 in any other environment).</summary>
[Route("dev/token")]
[AllowAnonymous]
public sealed class DevTokenController : ApiControllerBase
{
    private readonly IDevTokenService _devTokens;

    public DevTokenController(IDevTokenService devTokens) => _devTokens = devTokens;

    [HttpPost]
    [ProducesResponseType<DevTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Issue(DevTokenRequest request) => FromResult(_devTokens.Issue(request), Ok);
}
