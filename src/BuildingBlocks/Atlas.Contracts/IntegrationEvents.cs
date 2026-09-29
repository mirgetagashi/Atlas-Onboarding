namespace Atlas.Contracts;

// Rule for every event in this file: it carries identifiers only, never personal data.
// Whoever needs the data asks the owning service for it, from the right market.

/// <summary>The customer submitted a complete application. Starts identity verification and screening.</summary>
public sealed record ApplicationSubmitted(
    string ApplicationId,
    string Market,
    DateTimeOffset SubmittedAt);

/// <summary>Result of the automated checks (IDNow + World-Check).</summary>
public sealed record VerificationCompleted(
    string ApplicationId,
    string Market,
    IdentityCheckOutcome Identity,
    string? IdentityReference,
    ScreeningOutcome Screening,
    string? ScreeningReference,
    DateTimeOffset CompletedAt);

public enum IdentityCheckOutcome
{
    Verified,
    Failed,
}

public enum ScreeningOutcome
{
    /// <summary>Screening was skipped because identity verification failed first.</summary>
    NotPerformed,
    Clear,
    PossibleMatch,
}

/// <summary>A possible sanctions/PEP match: a compliance officer in that market has to review it.</summary>
public sealed record ApplicationReferredForReview(
    string ApplicationId,
    string Market,
    DateTimeOffset ReferredAt);

/// <summary>Published on every status change. Nothing consumes it yet; a push-notification service would.</summary>
public sealed record ApplicationStatusChanged(
    string ApplicationId,
    string Market,
    string Status,
    DateTimeOffset ChangedAt);

/// <summary>Lookup used by the outbox to turn a stored type name back into a CLR type.</summary>
public static class IntegrationEventTypes
{
    private static readonly IReadOnlyDictionary<string, Type> ByName = new[]
    {
        typeof(ApplicationSubmitted),
        typeof(VerificationCompleted),
        typeof(ApplicationReferredForReview),
        typeof(ApplicationStatusChanged),
    }.ToDictionary(t => t.Name);

    public static bool TryResolve(string name, out Type type) => ByName.TryGetValue(name, out type!);
}
