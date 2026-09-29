using Atlas.Contracts;
using Atlas.Verification.Worker.Clients;
using Atlas.Verification.Worker.Clients.Models;

namespace Atlas.Verification.Worker.Services;

/// <summary>
/// 1) IDNow identity check, 2) World-Check screening (only if identity passed).
/// This service reports facts; it never decides the application's status. Onboarding applies the rules.
/// If a provider is down the exception propagates, the message is retried later, and the
/// application simply stays "Submitted" meanwhile: an outage never rejects a customer.
/// </summary>
public sealed class VerificationService : IVerificationService
{
    private const string SubmittedStatus = "SUBMITTED";

    private readonly OnboardingClient _onboarding;
    private readonly IdNowClient _idNow;
    private readonly WorldCheckClient _worldCheck;
    private readonly TimeProvider _time;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        OnboardingClient onboarding,
        IdNowClient idNow,
        WorldCheckClient worldCheck,
        TimeProvider time,
        ILogger<VerificationService> logger)
    {
        _onboarding = onboarding;
        _idNow = idNow;
        _worldCheck = worldCheck;
        _time = time;
        _logger = logger;
    }

    public async Task<VerificationCompleted?> VerifyAsync(ApplicationSubmitted submitted, CancellationToken cancellationToken)
    {
        var applicant = await _onboarding.GetApplicantAsync(submitted.ApplicationId, cancellationToken);
        if (applicant.Status != SubmittedStatus)
        {
            _logger.LogInformation("Application {ApplicationId} is {Status}; checks already done", applicant.ApplicationId, applicant.Status);
            return null;
        }

        var request = new ProviderRequest(applicant.ApplicationId, applicant.FirstName, applicant.LastName, applicant.DateOfBirth, applicant.Market);

        var identity = await _idNow.VerifyAsync(request, cancellationToken);
        if (!identity.Verified)
        {
            _logger.LogInformation("Identity not verified for {ApplicationId}", applicant.ApplicationId);
            return new VerificationCompleted(
                applicant.ApplicationId,
                applicant.Market,
                IdentityCheckOutcome.Failed,
                identity.Reference,
                ScreeningOutcome.NotPerformed,
                ScreeningReference: null,
                _time.GetUtcNow());
        }

        var screening = await _worldCheck.ScreenAsync(request, cancellationToken);
        _logger.LogInformation(
            "Checks done for {ApplicationId}: identity verified, screening {Screening}",
            applicant.ApplicationId,
            screening.PossibleMatch ? "POSSIBLE_MATCH" : "CLEAR");

        return new VerificationCompleted(
            applicant.ApplicationId,
            applicant.Market,
            IdentityCheckOutcome.Verified,
            identity.Reference,
            screening.PossibleMatch ? ScreeningOutcome.PossibleMatch : ScreeningOutcome.Clear,
            screening.Reference,
            _time.GetUtcNow());
    }
}
