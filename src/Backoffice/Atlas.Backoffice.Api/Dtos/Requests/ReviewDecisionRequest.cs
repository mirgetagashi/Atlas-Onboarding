using Atlas.Backoffice.Api.Domain;

namespace Atlas.Backoffice.Api.Dtos.Requests;

public sealed record ReviewDecisionRequest(ReviewOutcome? Decision, string? Reason);
