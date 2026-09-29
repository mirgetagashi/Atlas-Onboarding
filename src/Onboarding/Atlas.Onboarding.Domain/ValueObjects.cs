using Atlas.Markets;

namespace Atlas.Onboarding.Domain;

public sealed record PersonalDetails(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone);

/// <summary>
/// A national id or, where the market allows it (MF), a passport number.
/// Already validated against the market's rules before it gets here.
/// </summary>
public sealed record PersonalIdentifier(IdentifierType Type, string Value);

public enum ApplicationStatus
{
    /// <summary>Being filled in by the customer. Can be resumed at any time.</summary>
    Draft,

    /// <summary>Complete and waiting for automated identity verification and screening.</summary>
    Submitted,

    /// <summary>Screening found a possible sanctions/PEP match. A human must decide (up to 48h).</summary>
    PendingComplianceReview,

    /// <summary>All checks passed, but the market requires a branch visit and wet signature (MD).</summary>
    AwaitingBranchActivation,

    Approved,

    Rejected,
}

public enum RejectionReason
{
    IdentityNotVerified,
    RejectedByCompliance,
}

public enum DocumentType
{
    IdentityDocument,
    Selfie,
}

public enum ScreeningResult
{
    Clear,
    PossibleMatch,
}

/// <summary>What the automated checks returned. Screening is null when identity failed and screening was skipped.</summary>
public sealed record VerificationOutcome(
    bool IdentityVerified,
    string? IdentityReference,
    ScreeningResult? Screening,
    string? ScreeningReference);
