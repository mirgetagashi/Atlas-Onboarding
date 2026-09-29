using Atlas.Onboarding.UseCases.Dtos.Requests;
using Atlas.Onboarding.UseCases.Dtos.Responses;
using Atlas.Onboarding.UseCases.Services;
using Atlas.ServiceDefaults.Auth;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Onboarding.Api.Controllers;

/// <summary>
/// Internal endpoints: called by Backoffice (forwarding the logged-in officer's token) and by the
/// Verification worker (with its own service token). Never exposed to mobile.
/// Authorization by role happens here; market checks and auditing happen in <see cref="IStaffApplicationService"/>.
/// </summary>
[Route("internal/applications")]
[Authorize]
public sealed class InternalApplicationsController : ApiControllerBase
{
    private const string AnyStaffOrService = $"{AtlasRoles.ComplianceOfficer},{AtlasRoles.BranchStaff},{AtlasRoles.VerificationService}";

    private readonly IStaffApplicationService _staffApplications;

    public InternalApplicationsController(IStaffApplicationService staffApplications) => _staffApplications = staffApplications;

    [HttpGet("{applicationId}")]
    [Authorize(Roles = AnyStaffOrService)]
    [ProducesResponseType<StaffApplicationResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string applicationId, CancellationToken cancellationToken) =>
        FromResult(await _staffApplications.GetAsync(applicationId, cancellationToken), Ok);

    [HttpGet("{applicationId}/documents/{documentType}")]
    [Authorize(Roles = AtlasRoles.ComplianceOfficer)]
    public async Task<IActionResult> GetDocument(string applicationId, string documentType, CancellationToken cancellationToken) =>
        FromResult(
            await _staffApplications.GetDocumentAsync(applicationId, documentType, cancellationToken),
            document => File(document.Content, document.ContentType));

    [HttpPost("{applicationId}/compliance-decision")]
    [Authorize(Roles = AtlasRoles.ComplianceOfficer)]
    [ProducesResponseType<StaffApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordComplianceDecision(
        string applicationId,
        ComplianceDecisionRequest request,
        CancellationToken cancellationToken) =>
        FromResult(await _staffApplications.RecordComplianceDecisionAsync(applicationId, request, cancellationToken), Ok);

    [HttpPost("{applicationId}/branch-activation")]
    [Authorize(Roles = AtlasRoles.BranchStaff)]
    [ProducesResponseType<StaffApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmBranchActivation(
        string applicationId,
        BranchActivationRequest request,
        CancellationToken cancellationToken) =>
        FromResult(await _staffApplications.ConfirmBranchActivationAsync(applicationId, request, cancellationToken), Ok);
}
