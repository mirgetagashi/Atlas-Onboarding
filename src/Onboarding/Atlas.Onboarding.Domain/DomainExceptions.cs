namespace Atlas.Onboarding.Domain;

/// <summary>A business rule was broken. <see cref="Code"/> is stable and machine-readable for clients.</summary>
public class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

/// <summary>The action is not allowed in the application's current status (e.g. uploading after submit).</summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string action, ApplicationStatus currentStatus)
        : base("invalid_status", $"Cannot {action} while the application is {currentStatus}.")
    {
        CurrentStatus = currentStatus;
    }

    public ApplicationStatus CurrentStatus { get; }
}
