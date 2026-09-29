using Atlas.Markets;
using Atlas.Onboarding.Domain;

namespace Atlas.Onboarding.UseCases.Dtos.Responses;

/// <summary>Full view of an application for authorised staff and services. Every read of it is audited.</summary>
public sealed record StaffApplicationResponse(
    string ApplicationId,
    string Market,
    ApplicationStatus Status,
    RejectionReason? RejectionReason,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone,
    IdentifierType IdentifierType,
    string IdentifierValue,
    IReadOnlyList<DocumentResponse> Documents,
    string? IdentityVerificationReference,
    string? ScreeningReference,
    bool? ComplianceApproved,
    string? ReviewedBy,
    string? ReviewReason,
    DateTimeOffset? ReviewedAt,
    string? ActivatedBy,
    string? ActivationBranchId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? DecidedAt);
