namespace Atlas.Onboarding.UseCases.Shared;

/// <summary>Two requests changed the same application at the same time; the later one lost and should retry.</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Exception innerException)
        : base("The application was changed by another request. Reload and retry.", innerException)
    {
    }
}
