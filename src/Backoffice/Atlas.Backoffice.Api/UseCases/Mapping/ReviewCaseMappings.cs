using Atlas.Backoffice.Api.Domain;
using Atlas.Backoffice.Api.Dtos.Responses;

namespace Atlas.Backoffice.Api.UseCases.Mapping;

public static class ReviewCaseMappings
{
    public static ReviewCaseResponse ToResponse(this ReviewCase reviewCase, DateTimeOffset now) => new(
        reviewCase.ApplicationId,
        reviewCase.Market,
        reviewCase.Status,
        reviewCase.ReferredAt,
        reviewCase.DueBy,
        reviewCase.IsOverdue(now),
        reviewCase.Outcome,
        reviewCase.DecidedBy,
        reviewCase.DecidedAt);
}
