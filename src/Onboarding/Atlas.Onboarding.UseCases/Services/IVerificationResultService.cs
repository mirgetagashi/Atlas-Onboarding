using Atlas.Onboarding.Domain;

namespace Atlas.Onboarding.UseCases.Services;

/// <summary>Applies the result of the automated checks (IDNow + World-Check) to an application.</summary>
public interface IVerificationResultService
{
    /// <summary>Safe to call twice for the same application: the second call changes nothing.</summary>
    Task ApplyAsync(string applicationId, VerificationOutcome outcome, CancellationToken cancellationToken);
}
