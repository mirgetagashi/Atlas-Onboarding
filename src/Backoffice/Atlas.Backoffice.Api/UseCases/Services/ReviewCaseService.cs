using Atlas.Backoffice.Api.Domain;
using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Backoffice.Api.Dtos.Responses;
using Atlas.Backoffice.Api.UseCases.Abstractions;
using Atlas.Backoffice.Api.UseCases.Mapping;
using Atlas.Common;

namespace Atlas.Backoffice.Api.UseCases.Services;

public sealed class ReviewCaseService : IReviewCaseService
{
    private readonly IReviewCaseRepository _cases;
    private readonly IOnboardingGateway _onboarding;
    private readonly ICurrentUser _user;
    private readonly TimeProvider _time;
    private readonly ILogger<ReviewCaseService> _logger;

    public ReviewCaseService(
        IReviewCaseRepository cases,
        IOnboardingGateway onboarding,
        ICurrentUser user,
        TimeProvider time,
        ILogger<ReviewCaseService> logger)
    {
        _cases = cases;
        _onboarding = onboarding;
        _user = user;
        _time = time;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ReviewCaseResponse>>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var wanted = ReviewCaseStatus.Open;
        if (status is not null && !Enum.TryParse(status, ignoreCase: true, out wanted))
        {
            return Error.Validation("status", "Use OPEN or DECIDED.");
        }

        if (_user.Market is not { } market)
        {
            return Error.Forbidden;
        }

        var cases = await _cases.ListAsync(market, wanted, cancellationToken);
        var now = _time.GetUtcNow();
        return Result<IReadOnlyList<ReviewCaseResponse>>.Success(cases.Select(c => c.ToResponse(now)).ToList());
    }

    public async Task<Result<ReviewCaseDetailsResponse>> GetAsync(string applicationId, CancellationToken cancellationToken)
    {
        var reviewCase = await _cases.FindByApplicationIdAsync(applicationId, cancellationToken);
        if (reviewCase is null)
        {
            return Error.NotFound();
        }

        if (!_user.CanAccessMarket(reviewCase.Market))
        {
            return Error.Forbidden;
        }

        // Onboarding audits this read against the officer, because the officer's own token is forwarded.
        var application = await _onboarding.GetApplicationAsync(applicationId, cancellationToken);
        if (!application.IsSuccess)
        {
            return application.Error!;
        }

        return new ReviewCaseDetailsResponse(reviewCase.ToResponse(_time.GetUtcNow()), application.Value);
    }

    public async Task<Result<DocumentContent>> GetDocumentAsync(string applicationId, string documentType, CancellationToken cancellationToken)
    {
        var reviewCase = await _cases.FindByApplicationIdAsync(applicationId, cancellationToken);
        if (reviewCase is null)
        {
            return Error.NotFound();
        }

        if (!_user.CanAccessMarket(reviewCase.Market))
        {
            return Error.Forbidden;
        }

        return await _onboarding.GetDocumentAsync(applicationId, documentType, cancellationToken);
    }

    public async Task<Result<ReviewCaseResponse>> DecideAsync(string applicationId, ReviewDecisionRequest request, CancellationToken cancellationToken)
    {
        if (request.Decision is not { } outcome || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Error.Validation("decision", "Decision (APPROVE or REJECT) and a reason are both required.");
        }

        var reviewCase = await _cases.FindByApplicationIdAsync(applicationId, cancellationToken);
        if (reviewCase is null)
        {
            return Error.NotFound();
        }

        if (!_user.CanAccessMarket(reviewCase.Market))
        {
            return Error.Forbidden;
        }

        if (reviewCase.Status == ReviewCaseStatus.Decided && reviewCase.Outcome != outcome)
        {
            return Error.Conflict(
                "Already decided",
                $"This case was already decided ({reviewCase.Outcome}) by {reviewCase.DecidedBy}.");
        }

        // 1) Onboarding applies the decision (it is the only owner of the application's status).
        //    That call is idempotent, so retrying after a failure in step 2 is safe.
        var applied = await _onboarding.RecordComplianceDecisionAsync(applicationId, outcome, request.Reason, cancellationToken);
        if (!applied.IsSuccess)
        {
            return applied.Error!;
        }

        // 2) Close the case in the officer's queue.
        var now = _time.GetUtcNow();
        reviewCase.RecordDecision(outcome, _user.Id, now);
        await _cases.SaveChangesAsync(cancellationToken);

        return reviewCase.ToResponse(now);
    }

    public async Task OpenAsync(string applicationId, string market, DateTimeOffset referredAt, CancellationToken cancellationToken)
    {
        if (await _cases.ExistsForApplicationAsync(applicationId, cancellationToken))
        {
            _logger.LogInformation("Review case for {ApplicationId} already exists; duplicate event ignored", applicationId);
            return;
        }

        _cases.Add(ReviewCase.Open(applicationId, market, referredAt));

        // If two copies race past the check above, the unique index rejects the second one; the message
        // is retried, and then the check above finds the case and returns.
        await _cases.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Opened review case for {ApplicationId} in {Market}", applicationId, market);
    }
}
