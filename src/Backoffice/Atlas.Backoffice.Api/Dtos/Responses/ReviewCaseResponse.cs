using Atlas.Backoffice.Api.Domain;

namespace Atlas.Backoffice.Api.Dtos.Responses;

public sealed record ReviewCaseResponse(
    string ApplicationId,
    string Market,
    ReviewCaseStatus Status,
    DateTimeOffset ReferredAt,
    DateTimeOffset DueBy,
    bool IsOverdue,
    ReviewOutcome? Outcome,
    string? DecidedBy,
    DateTimeOffset? DecidedAt);
