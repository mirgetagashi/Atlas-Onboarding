using Atlas.Onboarding.Domain;

namespace Atlas.Onboarding.UseCases.Dtos.Responses;

/// <summary>What the mobile app sees. NextSteps tells it where to resume after a lost connection.</summary>
public sealed record ApplicationResponse(
    string ApplicationId,
    string Market,
    ApplicationStatus Status,
    RejectionReason? RejectionReason,
    IReadOnlyList<string> NextSteps,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? DecidedAt);
