namespace Atlas.Backoffice.Api.Domain;

public enum ReviewCaseStatus
{
    Open,
    Decided,
}

public enum ReviewOutcome
{
    Approve,
    Reject,
}

/// <summary>
/// A compliance officer's work item for one possible sanctions/PEP match.
/// Holds no personal data (only ids, market, timestamps, officer), so this database does not need to be
/// split per market. The customer's data is always read live from Onboarding, in its own market.
/// </summary>
public sealed class ReviewCase
{
    /// <summary>Compliance: "may take up to 48 hours". Used to show officers what is overdue.</summary>
    public static readonly TimeSpan ReviewTarget = TimeSpan.FromHours(48);

    private ReviewCase()
    {
        // For EF Core.
    }

    public Guid Id { get; private set; }

    public string ApplicationId { get; private set; } = string.Empty;

    public string Market { get; private set; } = string.Empty;

    public ReviewCaseStatus Status { get; private set; }

    public DateTimeOffset ReferredAt { get; private set; }

    public DateTimeOffset DueBy { get; private set; }

    public ReviewOutcome? Outcome { get; private set; }

    public string? DecidedBy { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public static ReviewCase Open(string applicationId, string market, DateTimeOffset referredAt) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        Market = market.ToUpperInvariant(),
        Status = ReviewCaseStatus.Open,
        ReferredAt = referredAt,
        DueBy = referredAt + ReviewTarget,
    };

    public bool IsOverdue(DateTimeOffset now) => Status == ReviewCaseStatus.Open && now > DueBy;

    /// <summary>Recording the same decision twice is harmless; changing a decision is not allowed.</summary>
    public void RecordDecision(ReviewOutcome outcome, string officerId, DateTimeOffset now)
    {
        if (Status == ReviewCaseStatus.Decided)
        {
            if (Outcome == outcome)
            {
                return;
            }

            throw new InvalidOperationException($"Case {Id} was already decided as {Outcome}.");
        }

        if (string.IsNullOrWhiteSpace(officerId))
        {
            throw new ArgumentException("The deciding officer is required.", nameof(officerId));
        }

        Status = ReviewCaseStatus.Decided;
        Outcome = outcome;
        DecidedBy = officerId;
        DecidedAt = now;
    }
}
