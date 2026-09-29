using Atlas.Backoffice.Api.UseCases.Services;
using Atlas.Contracts;
using MassTransit;

namespace Atlas.Backoffice.Api.Infrastructure.Messaging;

/// <summary>Entry point for the ApplicationReferredForReview event: opens a review case through the UseCases layer.</summary>
public sealed class ApplicationReferredForReviewConsumer : IConsumer<ApplicationReferredForReview>
{
    private readonly IReviewCaseService _reviewCases;

    public ApplicationReferredForReviewConsumer(IReviewCaseService reviewCases) => _reviewCases = reviewCases;

    public Task Consume(ConsumeContext<ApplicationReferredForReview> context) =>
        _reviewCases.OpenAsync(
            context.Message.ApplicationId,
            context.Message.Market,
            context.Message.ReferredAt,
            context.CancellationToken);
}
