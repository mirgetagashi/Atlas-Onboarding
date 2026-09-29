using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Auditing;

namespace Atlas.Onboarding.Infrastructure.Auditing;

/// <summary>
/// One row of the access log (GC-2026-0814, point 2). Stored in the market's own database, next to
/// the record it describes, so it is retained for the same period.
/// </summary>
public sealed class AuditEntry
{
    public long Id { get; private set; }

    public string ApplicationId { get; private set; } = string.Empty;

    public ActorType ActorType { get; private set; }

    public string ActorId { get; private set; } = string.Empty;

    public AuditAction Action { get; private set; }

    /// <summary>What the access was for, e.g. "compliance.review".</summary>
    public string Purpose { get; private set; } = string.Empty;

    public DateTimeOffset At { get; private set; }

    public static AuditEntry Create(ApplicationId applicationId, Actor actor, AuditAction action, string purpose, DateTimeOffset at) => new()
    {
        ApplicationId = applicationId.Value,
        ActorType = actor.Type,
        ActorId = actor.Id,
        Action = action,
        Purpose = purpose,
        At = at,
    };
}
