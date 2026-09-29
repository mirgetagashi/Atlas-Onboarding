using System.Text.Json;
using Atlas.Backoffice.Api.Domain;
using Atlas.Backoffice.Api.Dtos.Responses;
using Atlas.Common;

namespace Atlas.Backoffice.Api.UseCases.Abstractions;

/// <summary>
/// Onboarding's internal API, called on behalf of the logged-in staff member.
/// Onboarding stays the only owner of the application and its status; Backoffice asks, it does not change data itself.
/// </summary>
public interface IOnboardingGateway
{
    Task<Result<JsonElement>> GetApplicationAsync(string applicationId, CancellationToken cancellationToken);

    Task<Result<DocumentContent>> GetDocumentAsync(string applicationId, string documentType, CancellationToken cancellationToken);

    Task<Result<JsonElement>> RecordComplianceDecisionAsync(
        string applicationId,
        ReviewOutcome outcome,
        string reason,
        CancellationToken cancellationToken);

    Task<Result<JsonElement>> ConfirmBranchActivationAsync(
        string applicationId,
        string? branchId,
        bool wetSignatureConfirmed,
        CancellationToken cancellationToken);
}
