using Atlas.Markets;
using Atlas.Onboarding.Domain;

namespace Atlas.Onboarding.Domain.Tests;

/// <summary>
/// The status rules are where a mistake would be most expensive (a sanctions match auto-rejected,
/// an MD account opened without a signature), so they are tested one by one.
/// </summary>
public sealed class ApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);
    private static readonly VerificationOutcome Clear = new(true, "idn_1", ScreeningResult.Clear, "wc_1");
    private static readonly VerificationOutcome PossibleMatch = new(true, "idn_1", ScreeningResult.PossibleMatch, "wc_1");
    private static readonly VerificationOutcome IdentityFailed = new(false, "idn_1", null, null);

    // ---------- Draft ----------

    [Fact]
    public void A_new_application_is_a_draft_missing_both_documents()
    {
        var application = NewDraft();

        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Equal(new[] { DocumentType.IdentityDocument, DocumentType.Selfie }, application.MissingDocuments());
    }

    [Fact]
    public void Uploading_the_same_document_type_again_replaces_it()
    {
        var application = NewDraft();
        application.AttachDocument(DocumentType.Selfie, "key-1", "image/jpeg", 100, Now);
        application.AttachDocument(DocumentType.Selfie, "key-2", "image/png", 200, Now);

        var selfie = Assert.Single(application.Documents);
        Assert.Equal("key-2", selfie.StorageKey);
    }

    [Fact]
    public void Cannot_submit_without_both_documents()
    {
        var application = NewDraft();
        application.AttachDocument(DocumentType.Selfie, "key", "image/jpeg", 100, Now);

        var error = Assert.Throws<DomainException>(() => application.Submit("2026-09", Now));
        Assert.Equal("documents_missing", error.Code);
        Assert.Equal(ApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public void Submitting_raises_the_event_that_starts_verification()
    {
        var application = Submitted();

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Contains(application.DomainEvents, e => e is ApplicationSubmittedDomainEvent);
    }

    [Fact]
    public void Nothing_can_be_changed_after_submitting()
    {
        var application = Submitted();

        Assert.Throws<InvalidStateTransitionException>(() =>
            application.AttachDocument(DocumentType.Selfie, "key", "image/jpeg", 100, Now));
        Assert.Throws<InvalidStateTransitionException>(() =>
            application.UpdateDetails(Details(), Identifier(), Now));
        Assert.Throws<InvalidStateTransitionException>(() =>
            application.Submit("2026-09", Now));
    }

    // ---------- Automated checks ----------

    [Theory]
    [InlineData(ActivationMode.Remote)]
    [InlineData(ActivationMode.InBranch)]
    public void Failed_identity_verification_rejects_the_application(ActivationMode activation)
    {
        var application = Submitted();

        application.RecordVerification(IdentityFailed, activation, Now);

        Assert.Equal(ApplicationStatus.Rejected, application.Status);
        Assert.Equal(RejectionReason.IdentityNotVerified, application.RejectionReason);
    }

    [Theory]
    [InlineData(ActivationMode.Remote)]
    [InlineData(ActivationMode.InBranch)]
    public void A_possible_sanctions_match_goes_to_a_human_and_is_never_rejected_automatically(ActivationMode activation)
    {
        var application = Submitted();

        application.RecordVerification(PossibleMatch, activation, Now);

        Assert.Equal(ApplicationStatus.PendingComplianceReview, application.Status);
        Assert.Null(application.RejectionReason);
        Assert.Null(application.DecidedAt);
        Assert.Contains(application.DomainEvents, e => e is ApplicationReferredForReviewDomainEvent);
    }

    [Fact]
    public void Clear_checks_in_a_remote_market_approve_the_application()
    {
        var application = Submitted();

        application.RecordVerification(Clear, ActivationMode.Remote, Now);

        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Equal(Now, application.DecidedAt);
    }

    [Fact]
    public void Clear_checks_in_MD_wait_for_the_branch_visit_instead_of_approving()
    {
        var application = Submitted();

        application.RecordVerification(Clear, ActivationMode.InBranch, Now);

        Assert.Equal(ApplicationStatus.AwaitingBranchActivation, application.Status);
        Assert.Null(application.DecidedAt);
    }

    [Fact]
    public void Verification_results_are_only_accepted_once()
    {
        var application = Submitted();
        application.RecordVerification(Clear, ActivationMode.Remote, Now);

        Assert.Throws<InvalidStateTransitionException>(() =>
            application.RecordVerification(Clear, ActivationMode.Remote, Now));
    }

    // ---------- Compliance review ----------

    [Fact]
    public void An_officer_can_reject_a_referred_application()
    {
        var application = Referred();

        application.RecordComplianceDecision(false, "officer.anna", "Confirmed match on sanctions list", ActivationMode.Remote, Now);

        Assert.Equal(ApplicationStatus.Rejected, application.Status);
        Assert.Equal(RejectionReason.RejectedByCompliance, application.RejectionReason);
        Assert.Equal("officer.anna", application.ReviewedBy);
        Assert.False(application.ComplianceApproved);
    }

    [Fact]
    public void An_officer_approval_in_a_remote_market_approves_the_application()
    {
        var application = Referred();

        application.RecordComplianceDecision(true, "officer.anna", "False positive: different date of birth", ActivationMode.Remote, Now);

        Assert.Equal(ApplicationStatus.Approved, application.Status);
    }

    [Fact]
    public void An_officer_approval_in_MD_still_requires_the_branch_visit()
    {
        var application = Referred();

        application.RecordComplianceDecision(true, "officer.anna", "False positive", ActivationMode.InBranch, Now);

        Assert.Equal(ApplicationStatus.AwaitingBranchActivation, application.Status);
    }

    [Theory]
    [InlineData("", "a reason")]
    [InlineData("officer.anna", "")]
    [InlineData("officer.anna", "   ")]
    public void A_compliance_decision_needs_an_officer_and_a_reason(string officer, string reason)
    {
        var application = Referred();

        Assert.Throws<DomainException>(() =>
            application.RecordComplianceDecision(true, officer, reason, ActivationMode.Remote, Now));
        Assert.Equal(ApplicationStatus.PendingComplianceReview, application.Status);
    }

    [Fact]
    public void A_compliance_decision_is_only_possible_for_a_referred_application()
    {
        var application = Submitted();

        Assert.Throws<InvalidStateTransitionException>(() =>
            application.RecordComplianceDecision(true, "officer.anna", "reason", ActivationMode.Remote, Now));
    }

    // ---------- Branch activation (MD) ----------

    [Fact]
    public void Branch_staff_confirmation_activates_an_MD_application()
    {
        var application = Submitted();
        application.RecordVerification(Clear, ActivationMode.InBranch, Now);

        application.ConfirmBranchActivation("branch.ben", "MD-PRISHTINA-01", Now);

        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Equal("branch.ben", application.ActivatedBy);
    }

    [Fact]
    public void Branch_activation_is_refused_unless_the_application_is_waiting_for_it()
    {
        var application = Submitted();
        application.RecordVerification(Clear, ActivationMode.Remote, Now); // already approved remotely

        Assert.Throws<InvalidStateTransitionException>(() =>
            application.ConfirmBranchActivation("branch.ben", "MD-01", Now));
    }

    // ---------- Application id ----------

    [Fact]
    public void Application_id_carries_its_market_and_round_trips()
    {
        var id = ApplicationId.New(MarketCode.Parse("MD"));

        Assert.StartsWith("MD-", id.Value);
        Assert.True(ApplicationId.TryParse(id.Value, out var parsed));
        Assert.Equal(id, parsed);
        Assert.Equal("MD", parsed.Market.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("MB-not-a-guid")]
    [InlineData("0403991450016")]
    [InlineData("MB_0123456789abcdef0123456789abcdef")]
    public void Malformed_application_ids_are_refused(string? input) =>
        Assert.False(ApplicationId.TryParse(input, out _));

    // ---------- Helpers ----------

    private static PersonalDetails Details() =>
        new("Arta", "Krasniqi", new DateOnly(1991, 3, 4), "arta@example.com", "+38344123456");

    private static PersonalIdentifier Identifier() => new(IdentifierType.NationalId, "0403991450016");

    private static Application NewDraft() =>
        Application.Start(ApplicationId.New(MarketCode.Parse("MB")), Details(), Identifier(), "token-hash", Now);

    private static Application Submitted()
    {
        var application = NewDraft();
        application.AttachDocument(DocumentType.IdentityDocument, "id-key", "image/jpeg", 100, Now);
        application.AttachDocument(DocumentType.Selfie, "selfie-key", "image/jpeg", 100, Now);
        application.Submit("2026-09", Now);
        return application;
    }

    private static Application Referred()
    {
        var application = Submitted();
        application.RecordVerification(PossibleMatch, ActivationMode.Remote, Now);
        return application;
    }
}
