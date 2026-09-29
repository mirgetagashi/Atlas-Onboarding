using Atlas.Common;
using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Abstractions;
using Atlas.Onboarding.UseCases.Auditing;
using Atlas.Onboarding.UseCases.Dtos.Requests;
using Atlas.Onboarding.UseCases.Dtos.Responses;
using Atlas.Onboarding.UseCases.Mapping;
using Atlas.Onboarding.UseCases.Shared;
using Atlas.Onboarding.UseCases.Validation;
using Microsoft.Extensions.Options;

namespace Atlas.Onboarding.UseCases.Services;

/// <summary>
/// The mobile flow, split into steps instead of one big POST: create a draft with the personal
/// details, upload the two photos, then submit. The app can call Get at any time to see the status
/// and the next steps, which is how it resumes after losing the connection.
/// </summary>
public sealed class ApplicantService : IApplicantService
{
    private readonly IMarketCatalog _markets;
    private readonly IMarketUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IDocumentStore _documents;
    private readonly OnboardingOptions _options;
    private readonly TimeProvider _time;

    public ApplicantService(
        IMarketCatalog markets,
        IMarketUnitOfWorkFactory unitOfWorkFactory,
        IDocumentStore documents,
        IOptions<OnboardingOptions> options,
        TimeProvider time)
    {
        _markets = markets;
        _unitOfWorkFactory = unitOfWorkFactory;
        _documents = documents;
        _options = options.Value;
        _time = time;
    }

    public async Task<Result<ApplicationCreatedResponse>> CreateAsync(CreateApplicationRequest request, CancellationToken cancellationToken)
    {
        if (!MarketCode.TryParse(request.Market, out var marketCode) || !_markets.TryGet(marketCode, out var market))
        {
            var supported = string.Join(", ", _markets.All.Select(m => m.Code.Value).OrderBy(c => c));
            return Error.Validation("market", $"Unknown market. Supported: {supported}.");
        }

        var now = _time.GetUtcNow();
        var validation = ApplicantDetailsValidator.Validate(request.ToDetailsRequest(), market, Today(now), _options.MinimumAgeYears);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.Errors);
        }

        var token = ApplicantToken.Generate();
        var application = Application.Start(
            ApplicationId.New(market.Code),
            validation.Details!,
            validation.Identifier!,
            ApplicantToken.Hash(token),
            now);

        await using var unitOfWork = _unitOfWorkFactory.Create(market.Code);
        unitOfWork.AddApplication(application);
        unitOfWork.Audit(application.Id, Actor.Applicant(application.Id), AuditAction.Create, "applicant.create", now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApplicationCreatedResponse(application.Id.Value, token, application.ToResponse());
    }

    public async Task<Result<ApplicationResponse>> GetAsync(string applicationId, string? applicantToken, CancellationToken cancellationToken)
    {
        if (!ApplicationLookup.TryResolve(applicationId, _markets, out var id, out _))
        {
            return Error.NotFound();
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindForApplicantAsync(id, applicantToken, cancellationToken);
        if (application is null)
        {
            return Error.NotFound();
        }

        unitOfWork.Audit(id, Actor.Applicant(id), AuditAction.Read, "applicant.view", _time.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return application.ToResponse();
    }

    public async Task<Result<ApplicationResponse>> UpdateDetailsAsync(
        string applicationId,
        string? applicantToken,
        ApplicantDetailsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ApplicationLookup.TryResolve(applicationId, _markets, out var id, out var market))
        {
            return Error.NotFound();
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindForApplicantAsync(id, applicantToken, cancellationToken);
        if (application is null)
        {
            return Error.NotFound();
        }

        var now = _time.GetUtcNow();
        var validation = ApplicantDetailsValidator.Validate(request, market, Today(now), _options.MinimumAgeYears);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.Errors);
        }

        application.UpdateDetails(validation.Details!, validation.Identifier!, now);
        unitOfWork.Audit(id, Actor.Applicant(id), AuditAction.Update, "applicant.update-details", now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return application.ToResponse();
    }

    public async Task<Result<ApplicationResponse>> UploadDocumentAsync(
        string applicationId,
        string? applicantToken,
        string documentType,
        DocumentUpload upload,
        CancellationToken cancellationToken)
    {
        if (!ApiNames.TryParse<DocumentType>(documentType, out var type))
        {
            var allowed = string.Join(", ", Enum.GetValues<DocumentType>().Select(t => ApiNames.Of(t)));
            return Error.Validation("documentType", $"Unknown document type. Allowed: {allowed}.");
        }

        var uploadError = CheckUpload(upload, out var contentType, out var sizeBytes);
        if (uploadError is not null)
        {
            return uploadError;
        }

        if (!ApplicationLookup.TryResolve(applicationId, _markets, out var id, out _))
        {
            return Error.NotFound();
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindForApplicantAsync(id, applicantToken, cancellationToken);
        if (application is null)
        {
            return Error.NotFound();
        }

        // Checked before uploading, so nothing is written to storage for an already submitted application.
        if (!application.IsEditable)
        {
            return Error.Conflict(
                "Action not allowed in the current status",
                $"Cannot upload documents while the application is {application.Status}.",
                "invalid_status");
        }

        var storageKey = await _documents.SaveAsync(id, type, upload.Content, contentType, cancellationToken);

        var now = _time.GetUtcNow();
        application.AttachDocument(type, storageKey, contentType, sizeBytes, now);
        unitOfWork.Audit(id, Actor.Applicant(id), AuditAction.Update, "applicant.upload-document", now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return application.ToResponse();
    }

    public async Task<Result<ApplicationResponse>> SubmitAsync(
        string applicationId,
        string? applicantToken,
        SubmitApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ApplicationLookup.TryResolve(applicationId, _markets, out var id, out _))
        {
            return Error.NotFound();
        }

        await using var unitOfWork = _unitOfWorkFactory.Create(id.Market);
        var application = await unitOfWork.FindForApplicantAsync(id, applicantToken, cancellationToken);
        if (application is null)
        {
            return Error.NotFound();
        }

        // Idempotent: if the phone lost the first response and submits again, it just gets the current status.
        if (application.Status != ApplicationStatus.Draft)
        {
            return application.ToResponse();
        }

        var currentTerms = _options.CurrentTermsVersion;
        if (!request.TermsAccepted || request.TermsVersion != currentTerms)
        {
            return Error.Validation("terms", $"The current terms (version {currentTerms}) must be accepted.");
        }

        // The domain throws (422) if a document is missing.
        var now = _time.GetUtcNow();
        application.Submit(currentTerms, now);
        unitOfWork.Audit(id, Actor.Applicant(id), AuditAction.Update, "applicant.submit", now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return application.ToResponse();
    }

    private Error? CheckUpload(DocumentUpload upload, out string contentType, out long sizeBytes)
    {
        contentType = upload.ContentType?.Split(';')[0].Trim().ToLowerInvariant() ?? string.Empty;
        sizeBytes = upload.ContentLength ?? 0;

        if (!_options.AllowedDocumentContentTypes.Contains(contentType))
        {
            return new Error(
                ErrorType.UnsupportedMediaType,
                "Unsupported image format",
                $"Send the image as one of: {string.Join(", ", _options.AllowedDocumentContentTypes)}.");
        }

        if (sizeBytes <= 0)
        {
            return new Error(ErrorType.LengthRequired, "Content-Length is required");
        }

        if (sizeBytes > _options.MaxDocumentBytes)
        {
            return new Error(
                ErrorType.PayloadTooLarge,
                "Image too large",
                $"Maximum size is {_options.MaxDocumentBytes / (1024 * 1024)} MB.");
        }

        return null;
    }

    private static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);
}
