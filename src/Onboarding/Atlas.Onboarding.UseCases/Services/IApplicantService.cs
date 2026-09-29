using Atlas.Common;
using Atlas.Onboarding.UseCases.Dtos.Requests;
using Atlas.Onboarding.UseCases.Dtos.Responses;

namespace Atlas.Onboarding.UseCases.Services;

/// <summary>
/// Everything the applicant does from the mobile app. Except for creation, every call must present
/// the applicant token; a wrong token gives "not found", so the application's existence is not revealed.
/// </summary>
public interface IApplicantService
{
    Task<Result<ApplicationCreatedResponse>> CreateAsync(CreateApplicationRequest request, CancellationToken cancellationToken);

    Task<Result<ApplicationResponse>> GetAsync(string applicationId, string? applicantToken, CancellationToken cancellationToken);

    Task<Result<ApplicationResponse>> UpdateDetailsAsync(
        string applicationId,
        string? applicantToken,
        ApplicantDetailsRequest request,
        CancellationToken cancellationToken);

    Task<Result<ApplicationResponse>> UploadDocumentAsync(
        string applicationId,
        string? applicantToken,
        string documentType,
        DocumentUpload upload,
        CancellationToken cancellationToken);

    Task<Result<ApplicationResponse>> SubmitAsync(
        string applicationId,
        string? applicantToken,
        SubmitApplicationRequest request,
        CancellationToken cancellationToken);
}
