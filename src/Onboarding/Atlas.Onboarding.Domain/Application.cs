using Atlas.Markets;

namespace Atlas.Onboarding.Domain;

/// <summary>
/// A customer's onboarding application. This is the only place where the status is allowed to change.
/// </summary>
/// <remarks>
/// Flow: Draft, then Submitted after the customer submits. After the automated checks it becomes
/// Rejected (identity failed), PendingComplianceReview (possible sanctions match) or, if everything is
/// clear, Approved. In markets that require a branch visit (MD) it goes to AwaitingBranchActivation first
/// and is approved once the branch confirms the signature.
/// </remarks>
public sealed class Application
{
    private static readonly DocumentType[] RequiredDocuments = { DocumentType.IdentityDocument, DocumentType.Selfie };

    private readonly List<ApplicationDocument> _documents = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    private Application()
    {
        // For EF Core.
    }

    private Application(ApplicationId id, PersonalDetails details, PersonalIdentifier identifier, string applicantTokenHash, DateTimeOffset now)
    {
        Id = id;
        Market = id.Market;
        Details = details;
        Identifier = identifier;
        ApplicantTokenHash = applicantTokenHash;
        Status = ApplicationStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public ApplicationId Id { get; private set; }

    public MarketCode Market { get; private set; }

    public PersonalDetails Details { get; private set; } = null!;

    public PersonalIdentifier Identifier { get; private set; } = null!;

    /// <summary>SHA-256 of the secret handed to the applicant's device. The secret itself is never stored.</summary>
    public string ApplicantTokenHash { get; private set; } = string.Empty;

    public ApplicationStatus Status { get; private set; }

    public RejectionReason? RejectionReason { get; private set; }

    public string? TermsVersion { get; private set; }

    public string? IdentityVerificationReference { get; private set; }

    public string? ScreeningReference { get; private set; }

    public bool? ComplianceApproved { get; private set; }

    public string? ReviewedBy { get; private set; }

    public string? ReviewReason { get; private set; }

    public string? ActivatedBy { get; private set; }

    public string? ActivationBranchId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public IReadOnlyCollection<ApplicationDocument> Documents => _documents.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public bool IsEditable => Status == ApplicationStatus.Draft;

    public static Application Start(
        ApplicationId id,
        PersonalDetails details,
        PersonalIdentifier identifier,
        string applicantTokenHash,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(applicantTokenHash))
        {
            throw new ArgumentException("An applicant token hash is required.", nameof(applicantTokenHash));
        }

        return new Application(id, details, identifier, applicantTokenHash, now);
    }

    public void UpdateDetails(PersonalDetails details, PersonalIdentifier identifier, DateTimeOffset now)
    {
        EnsureStatus(ApplicationStatus.Draft, "change personal details");
        Details = details;
        Identifier = identifier;
        UpdatedAt = now;
    }

    public void AttachDocument(DocumentType type, string storageKey, string contentType, long sizeBytes, DateTimeOffset now)
    {
        EnsureStatus(ApplicationStatus.Draft, "upload documents");

        var existing = _documents.SingleOrDefault(d => d.Type == type);
        if (existing is null)
        {
            _documents.Add(new ApplicationDocument(type, storageKey, contentType, sizeBytes, now));
        }
        else
        {
            existing.Replace(storageKey, contentType, sizeBytes, now);
        }

        UpdatedAt = now;
    }

    public IReadOnlyList<DocumentType> MissingDocuments() =>
        RequiredDocuments.Where(required => _documents.All(d => d.Type != required)).ToList();

    public void Submit(string termsVersion, DateTimeOffset now)
    {
        EnsureStatus(ApplicationStatus.Draft, "submit");

        if (string.IsNullOrWhiteSpace(termsVersion))
        {
            throw new DomainException("terms_not_accepted", "The terms must be accepted before submitting.");
        }

        var missing = MissingDocuments();
        if (missing.Count > 0)
        {
            throw new DomainException("documents_missing", $"Missing documents: {string.Join(", ", missing)}.");
        }

        TermsVersion = termsVersion;
        SubmittedAt = now;
        ChangeStatus(ApplicationStatus.Submitted, now);
        Raise(new ApplicationSubmittedDomainEvent(Id, now));
    }

    public void RecordVerification(VerificationOutcome outcome, ActivationMode activation, DateTimeOffset now)
    {
        EnsureStatus(ApplicationStatus.Submitted, "record verification results");

        IdentityVerificationReference = outcome.IdentityReference;
        ScreeningReference = outcome.ScreeningReference;

        if (!outcome.IdentityVerified)
        {
            Reject(Domain.RejectionReason.IdentityNotVerified, now);
            return;
        }

        switch (outcome.Screening)
        {
            case ScreeningResult.PossibleMatch:
                // Compliance (GC-2026-0814, point 3): a possible match is never decided automatically, an officer has to review it.
                ChangeStatus(ApplicationStatus.PendingComplianceReview, now);
                Raise(new ApplicationReferredForReviewDomainEvent(Id, now));
                break;

            case ScreeningResult.Clear:
                CompleteChecks(activation, now);
                break;

            default:
                throw new DomainException("screening_missing", "Identity was verified but no screening result was provided.");
        }
    }

    public void RecordComplianceDecision(bool approved, string officerId, string reason, ActivationMode activation, DateTimeOffset now)
    {
        EnsureStatus(ApplicationStatus.PendingComplianceReview, "record a compliance decision");

        if (string.IsNullOrWhiteSpace(officerId))
        {
            throw new DomainException("officer_required", "A compliance decision must be attributable to an officer.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("reason_required", "A compliance decision needs a reason.");
        }

        ComplianceApproved = approved;
        ReviewedBy = officerId;
        ReviewReason = reason.Trim();
        ReviewedAt = now;

        if (approved)
        {
            CompleteChecks(activation, now);
        }
        else
        {
            Reject(Domain.RejectionReason.RejectedByCompliance, now);
        }
    }

    public void ConfirmBranchActivation(string staffId, string branchId, DateTimeOffset now)
    {
        EnsureStatus(ApplicationStatus.AwaitingBranchActivation, "confirm branch activation");

        if (string.IsNullOrWhiteSpace(staffId) || string.IsNullOrWhiteSpace(branchId))
        {
            throw new DomainException("activation_incomplete", "Branch activation needs the staff member and the branch.");
        }

        ActivatedBy = staffId;
        ActivationBranchId = branchId;
        Approve(now);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void CompleteChecks(ActivationMode activation, DateTimeOffset now)
    {
        if (activation == ActivationMode.InBranch)
        {
            // Annex B, MD: activation needs the customer in a branch. No exemption may be assumed.
            ChangeStatus(ApplicationStatus.AwaitingBranchActivation, now);
        }
        else
        {
            Approve(now);
        }
    }

    private void Approve(DateTimeOffset now)
    {
        DecidedAt = now;
        ChangeStatus(ApplicationStatus.Approved, now);
    }

    private void Reject(RejectionReason reason, DateTimeOffset now)
    {
        RejectionReason = reason;
        DecidedAt = now;
        ChangeStatus(ApplicationStatus.Rejected, now);
    }

    private void ChangeStatus(ApplicationStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
        Raise(new ApplicationStatusChangedDomainEvent(Id, status, now));
    }

    private void EnsureStatus(ApplicationStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidStateTransitionException(action, Status);
        }
    }

    private void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
