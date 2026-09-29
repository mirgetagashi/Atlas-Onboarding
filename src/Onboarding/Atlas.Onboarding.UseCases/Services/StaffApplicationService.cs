using Atlas.Common;
using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Abstractions;
using Atlas.Onboarding.UseCases.Auditing;
using Atlas.Onboarding.UseCases.Dtos.Requests;
using Atlas.Onboarding.UseCases.Dtos.Responses;
using Atlas.Onboarding.UseCases.Mapping;
using Atlas.Onboarding.UseCases.Shared;

namespace Atlas.Onboarding.UseCases.Services;

/// <summary>
/// Staff and service access to applications. Staff only see their own market, and every read or
/// decision is written to the audit log against the named person or service.
/// </summary>
public sealed class StaffApplicationService : IStaffApplicationService
{
    private const int MaxReasonLength = 1000;

    private readonly IMarketCatalog _markets;
    private readonly IMarketUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IDocumentStore _documents;
    private readonly ICurrentStaff _staff;
    private readonly TimeProvider _time;

    public StaffApplicationService(
        IMarketCatalog markets,
        IMarketUnitOfWorkFactory unitOfWorkFactory,
        IDocumentStore documents,
        ICurrentStaff staff,
        TimeProvider time)
    {
        _markets = markets;
        _unitOfWorkFactory = unitOfWorkFactory;
        _documents = documents;
        _staff = staff;
        _time = time;
    }

    private Actor CurrentActor => new(_staff.IsService ? ActorType.Service : ActorType.Staff, _staff.Id);

    public async Task<Result<StaffApplicationResponse>> GetAsync(string applicationId, CancellationToken cancellationToken)
    {
        if (!ApplicationLookup.TryResolve(applicationId, _markets, out var id, out _))
        {
            return Error.NotFound();
        }

        if (!_staff.CanAccessMarket(id.Market))
        {
            return Error.Forbidden;
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindApplicationAsync(id, cancellationToken);
        if (application is null)
        {
            return Error.NotFound();
        }

        unitOfWork.Audit(id, CurrentActor, AuditAction.Read, ReadPurpose(), _time.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return application.ToStaffResponse();
    }

    public async Task<Result<DocumentContent>> GetDocumentAsync(string applicationId, string documentType, CancellationToken cancellationToken)
    {
        if (!ApiNames.TryParse<DocumentType>(documentType, out var type)
            || !ApplicationLookup.TryResolve(applicationId, _markets, out var id, out _))
        {
            return Error.NotFound();
        }

        if (!_staff.CanAccessMarket(id.Market))
        {
            return Error.Forbidden;
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindApplicationAsync(id, cancellationToken);
        var document = application?.Documents.SingleOrDefault(d => d.Type == type);
        if (document is null)
        {
            return Error.NotFound();
        }

        unitOfWork.Audit(id, CurrentActor, AuditAction.Read, $"compliance.view-{ApiNames.Of(type).ToLowerInvariant()}", _time.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var content = await _documents.OpenReadAsync(id.Market, document.StorageKey, cancellationToken);
        return new DocumentContent(content, document.ContentType);
    }

    public async Task<Result<StaffApplicationResponse>> RecordComplianceDecisionAsync(
        string applicationId,
        ComplianceDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Decision is null)
        {
            errors["decision"] = new[] { "Decision is required: APPROVE or REJECT." };
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > MaxReasonLength)
        {
            errors["reason"] = new[] { $"A reason (up to {MaxReasonLength} characters) is required for every compliance decision." };
        }

        if (errors.Count > 0)
        {
            return Error.Validation(errors);
        }

        if (!ApplicationLookup.TryResolve(applicationId, _markets, out var id, out var market))
        {
            return Error.NotFound();
        }

        if (!_staff.IsComplianceOfficer || !_staff.CanAccessMarket(id.Market))
        {
            return Error.Forbidden;
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindApplicationAsync(id, cancellationToken);
        if (application is null)
        {
            return Error.NotFound();
        }

        var approve = request.Decision == ComplianceDecision.Approve;

        // Idempotent: the same decision sent twice (e.g. Backoffice retrying) is fine; a different one is a conflict.
        if (application.ComplianceApproved is { } previous)
        {
            if (previous == approve)
            {
                return application.ToStaffResponse();
            }

            return Error.Conflict(
                "Already decided",
                $"This application was already {(previous ? "approved" : "rejected")} by {application.ReviewedBy}.");
        }

        // The domain throws (409) if the application is not waiting for review.
        var now = _time.GetUtcNow();
        application.RecordComplianceDecision(approve, _staff.Id, request.Reason!, market.Activation, now);
        unitOfWork.Audit(id, CurrentActor, AuditAction.Decide, "compliance.decision", now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return application.ToStaffResponse();
    }

    public async Task<Result<StaffApplicationResponse>> ConfirmBranchActivationAsync(
        string applicationId,
        BranchActivationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.BranchId))
        {
            errors["branchId"] = new[] { "Branch id is required." };
        }

        if (!request.WetSignatureConfirmed)
        {
            errors["wetSignatureConfirmed"] = new[] { "The customer's wet signature must be confirmed before activation." };
        }

        if (errors.Count > 0)
        {
            return Error.Validation(errors);
        }

        if (!ApplicationLookup.TryResolve(applicationId, _markets, out var id, out _))
        {
            return Error.NotFound();
        }

        if (!_staff.CanAccessMarket(id.Market))
        {
            return Error.Forbidden;
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindApplicationAsync(id, cancellationToken);
        if (application is null)
        {
            return Error.NotFound();
        }

        // Already activated, so just return the current state.
        if (application.ActivatedBy is not null)
        {
            return application.ToStaffResponse();
        }

        // The domain throws (409) if the application is not waiting for branch activation.
        var now = _time.GetUtcNow();
        application.ConfirmBranchActivation(_staff.Id, request.BranchId!.Trim(), now);
        unitOfWork.Audit(id, CurrentActor, AuditAction.Decide, "branch.activation", now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return application.ToStaffResponse();
    }

    private string ReadPurpose() =>
        _staff.IsService ? "verification.checks"
        : _staff.IsComplianceOfficer ? "compliance.review"
        : "branch.activation";
}
