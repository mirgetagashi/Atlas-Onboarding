namespace Atlas.Onboarding.Domain;

/// <summary>Something that happened inside the aggregate. Infrastructure turns these into outbox messages.</summary>
public interface IDomainEvent
{
    ApplicationId ApplicationId { get; }

    DateTimeOffset OccurredAt { get; }
}

public sealed record ApplicationSubmittedDomainEvent(ApplicationId ApplicationId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ApplicationReferredForReviewDomainEvent(ApplicationId ApplicationId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ApplicationStatusChangedDomainEvent(
    ApplicationId ApplicationId,
    ApplicationStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
