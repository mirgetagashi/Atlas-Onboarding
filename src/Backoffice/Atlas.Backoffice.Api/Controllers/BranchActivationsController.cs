using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Backoffice.Api.UseCases.Services;
using Atlas.ServiceDefaults.Auth;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Backoffice.Api.Controllers;

/// <summary>Market MD: branch staff confirm the customer's wet signature.</summary>
[Route("branch-activations")]
[Authorize(Roles = AtlasRoles.BranchStaff)]
public sealed class BranchActivationsController : ApiControllerBase
{
    private readonly IBranchActivationService _branchActivations;

    public BranchActivationsController(IBranchActivationService branchActivations) => _branchActivations = branchActivations;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(BranchActivationRequest request, CancellationToken cancellationToken) =>
        FromResult(await _branchActivations.ConfirmAsync(request, cancellationToken), application => Ok(application));
}
