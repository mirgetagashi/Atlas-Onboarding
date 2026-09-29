using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Backoffice.Api.Dtos.Responses;
using Atlas.Common;

namespace Atlas.Backoffice.Api.UseCases.Services;

/// <summary>
/// The compliance officer's work: see the queue for their market, open a case, look at the documents, decide.
/// The system never makes the decision itself, every decision needs a named officer.
/// </summary>
public interface IReviewCaseService
{
    Task<Result<IReadOnlyList<ReviewCaseResponse>>> ListAsync(string? status, CancellationToken cancellationToken);

    Task<Result<ReviewCaseDetailsResponse>> GetAsync(string applicationId, CancellationToken cancellationToken);

    Task<Result<DocumentContent>> GetDocumentAsync(string applicationId, string documentType, CancellationToken cancellationToken);

    Task<Result<ReviewCaseResponse>> DecideAsync(string applicationId, ReviewDecisionRequest request, CancellationToken cancellationToken);

    /// <summary>Called when Onboarding refers an application. Safe to call twice for the same application.</summary>
    Task OpenAsync(string applicationId, string market, DateTimeOffset referredAt, CancellationToken cancellationToken);
}
