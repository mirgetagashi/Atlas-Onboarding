using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Abstractions;
using Atlas.Onboarding.UseCases.Auditing;
using Atlas.Onboarding.UseCases.Shared;
using Microsoft.Extensions.Logging;

namespace Atlas.Onboarding.UseCases.Services;

public sealed class VerificationResultService : IVerificationResultService
{
    private readonly IMarketCatalog _markets;
    private readonly IMarketUnitOfWorkFactory _unitOfWorkFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<VerificationResultService> _logger;

    public VerificationResultService(
        IMarketCatalog markets,
        IMarketUnitOfWorkFactory unitOfWorkFactory,
        TimeProvider time,
        ILogger<VerificationResultService> logger)
    {
        _markets = markets;
        _unitOfWorkFactory = unitOfWorkFactory;
        _time = time;
        _logger = logger;
    }

    public async Task ApplyAsync(string applicationId, VerificationOutcome outcome, CancellationToken cancellationToken)
    {
        if (!ApplicationLookup.TryResolve(applicationId, _markets, out var id, out var market))
        {
            _logger.LogError("Verification result for unknown application {ApplicationId}; ignored", applicationId);
            return;
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindApplicationAsync(id, cancellationToken);
        if (application is null)
        {
            _logger.LogError("Verification result for missing application {ApplicationId}; ignored", id.Value);
            return;
        }

        // A message can be delivered twice; if the result was already applied, there is nothing to do.
        if (application.Status != ApplicationStatus.Submitted)
        {
            _logger.LogInformation(
                "Duplicate verification result for {ApplicationId} (already {Status}); ignored",
                id.Value,
                application.Status);
            return;
        }

        var now = _time.GetUtcNow();
        application.RecordVerification(outcome, market.Activation, now);
        unitOfWork.Audit(id, Actor.Service("verification-result-consumer"), AuditAction.Update, "verification.recorded", now);

        // A concurrent update throws here; the message is retried and the status check above makes the retry safe.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Application {ApplicationId} is now {Status}", id.Value, application.Status);
    }
}
