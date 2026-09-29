using Atlas.Onboarding.UseCases.Dtos.Requests;
using Atlas.Onboarding.UseCases.Dtos.Responses;
using Atlas.Onboarding.UseCases.Services;
using Atlas.Onboarding.UseCases.Shared;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Onboarding.Api.Controllers;

/// <summary>
/// Called by the mobile app. Each action only reads the HTTP request, calls <see cref="IApplicantService"/>
/// and chooses the status code. The applicant is identified by the X-Applicant-Token header.
/// </summary>
[Route("applications")]
public sealed class ApplicationsController : ApiControllerBase
{
    private readonly IApplicantService _applicants;

    public ApplicationsController(IApplicantService applicants) => _applicants = applicants;

    [HttpPost]
    [ProducesResponseType<ApplicationCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateApplicationRequest request, CancellationToken cancellationToken) =>
        FromResult(
            await _applicants.CreateAsync(request, cancellationToken),
            created => CreatedAtAction(nameof(Get), new { applicationId = created.ApplicationId }, created));

    [HttpGet("{applicationId}")]
    [ProducesResponseType<ApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        string applicationId,
        [FromHeader(Name = ApplicantToken.HeaderName)] string? applicantToken,
        CancellationToken cancellationToken) =>
        FromResult(await _applicants.GetAsync(applicationId, applicantToken, cancellationToken), Ok);

    [HttpPut("{applicationId}/details")]
    [ProducesResponseType<ApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDetails(
        string applicationId,
        [FromHeader(Name = ApplicantToken.HeaderName)] string? applicantToken,
        ApplicantDetailsRequest request,
        CancellationToken cancellationToken) =>
        FromResult(await _applicants.UpdateDetailsAsync(applicationId, applicantToken, request, cancellationToken), Ok);

    /// <summary>The image is sent as the raw request body (image/jpeg or image/png), not as base64.</summary>
    [HttpPut("{applicationId}/documents/{documentType}")]
    [ProducesResponseType<ApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> UploadDocument(
        string applicationId,
        string documentType,
        [FromHeader(Name = ApplicantToken.HeaderName)] string? applicantToken,
        CancellationToken cancellationToken)
    {
        var upload = new DocumentUpload(Request.Body, Request.ContentType, Request.ContentLength);
        return FromResult(
            await _applicants.UploadDocumentAsync(applicationId, applicantToken, documentType, upload, cancellationToken),
            Ok);
    }

    /// <summary>202: the decision is made asynchronously (a sanctions match can need up to 48h of human review).</summary>
    [HttpPost("{applicationId}/submit")]
    [ProducesResponseType<ApplicationResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit(
        string applicationId,
        [FromHeader(Name = ApplicantToken.HeaderName)] string? applicantToken,
        SubmitApplicationRequest request,
        CancellationToken cancellationToken) =>
        FromResult(
            await _applicants.SubmitAsync(applicationId, applicantToken, request, cancellationToken),
            response => AcceptedAtAction(nameof(Get), new { applicationId }, response));
}
