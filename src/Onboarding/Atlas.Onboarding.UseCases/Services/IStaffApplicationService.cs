using Atlas.Common;
using Atlas.Onboarding.UseCases.Dtos.Requests;
using Atlas.Onboarding.UseCases.Dtos.Responses;

namespace Atlas.Onboarding.UseCases.Services;

/// <summary>
/// What staff (compliance officers, branch employees) and internal services do with an application.
/// Every call is tied to the current staff member and written to the audit log.
/// </summary>
public interface IStaffApplicationService
{
    Task<Result<StaffApplicationResponse>> GetAsync(string applicationId, CancellationToken cancellationToken);

    Task<Result<DocumentContent>> GetDocumentAsync(string applicationId, string documentType, CancellationToken cancellationToken);

    Task<Result<StaffApplicationResponse>> RecordComplianceDecisionAsync(
        string applicationId,
        ComplianceDecisionRequest request,
        CancellationToken cancellationToken);

    Task<Result<StaffApplicationResponse>> ConfirmBranchActivationAsync(
        string applicationId,
        BranchActivationRequest request,
        CancellationToken cancellationToken);
}
