using Atlas.Onboarding.Domain;

namespace Atlas.Onboarding.UseCases.Auditing;

public enum ActorType
{
    Applicant,
    Staff,
    Service,
}

public enum AuditAction
{
    Create,
    Read,
    Update,
    Decide,
}

/// <summary>Who did something: a named person or a named service.</summary>
public sealed record Actor(ActorType Type, string Id)
{
    public static Actor Applicant(ApplicationId applicationId) => new(ActorType.Applicant, applicationId.Value);

    public static Actor Service(string serviceName) => new(ActorType.Service, serviceName);
}
