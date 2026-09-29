using Atlas.Contracts;
using Atlas.Onboarding.Domain;

namespace Atlas.Onboarding.Infrastructure.Outbox;

/// <summary>
/// Translates internal domain events into the public contracts other services consume.
/// Keeping them separate means the domain can change without breaking other services.
/// </summary>
internal static class IntegrationEventMapper
{
    public static object? ToIntegrationEvent(IDomainEvent domainEvent) => domainEvent switch
    {
        ApplicationSubmittedDomainEvent e =>
            new ApplicationSubmitted(e.ApplicationId.Value, e.ApplicationId.Market.Value, e.OccurredAt),

        ApplicationReferredForReviewDomainEvent e =>
            new ApplicationReferredForReview(e.ApplicationId.Value, e.ApplicationId.Market.Value, e.OccurredAt),

        ApplicationStatusChangedDomainEvent e =>
            new ApplicationStatusChanged(e.ApplicationId.Value, e.ApplicationId.Market.Value, e.Status.ToString(), e.OccurredAt),

        _ => null,
    };
}
