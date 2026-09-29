using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Backoffice.Api.Dtos.Responses;
using Atlas.Backoffice.Api.UseCases.Services;
using Atlas.ServiceDefaults.Auth;
using Atlas.ServiceDefaults.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Backoffice.Api.Controllers;

/// <summary>Compliance review. Each action only calls <see cref="IReviewCaseService"/> and picks the status code.</summary>
[Route("review-cases")]
[Authorize(Roles = AtlasRoles.ComplianceOfficer)]
public sealed class ReviewCasesController : ApiControllerBase
{
    private readonly IReviewCaseService _reviewCases;

    public ReviewCasesController(IReviewCaseService reviewCases) => _reviewCases = reviewCases;

    /// <summary>Cases of the officer's own market, most urgent first. ?status=OPEN|DECIDED (default OPEN).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReviewCaseResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken) =>
        FromResult(await _reviewCases.ListAsync(status, cancellationToken), Ok);

    [HttpGet("{applicationId}")]
    [ProducesResponseType<ReviewCaseDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string applicationId, CancellationToken cancellationToken) =>
        FromResult(await _reviewCases.GetAsync(applicationId, cancellationToken), Ok);

    [HttpGet("{applicationId}/documents/{documentType}")]
    public async Task<IActionResult> GetDocument(string applicationId, string documentType, CancellationToken cancellationToken) =>
        FromResult(
            await _reviewCases.GetDocumentAsync(applicationId, documentType, cancellationToken),
            document => File(document.Content, document.ContentType));

    [HttpPost("{applicationId}/decision")]
    [ProducesResponseType<ReviewCaseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Decide(string applicationId, ReviewDecisionRequest request, CancellationToken cancellationToken) =>
        FromResult(await _reviewCases.DecideAsync(applicationId, request, cancellationToken), Ok);
}
