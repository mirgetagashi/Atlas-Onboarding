using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Dtos.Requests;
using Atlas.Onboarding.UseCases.Dtos.Responses;
using Atlas.Onboarding.UseCases.Shared;

namespace Atlas.Onboarding.UseCases.Mapping;

/// <summary>Maps domain objects to response DTOs.</summary>
public static class ApplicationMappings
{
    public static ApplicationResponse ToResponse(this Application application) => new(
        application.Id.Value,
        application.Market.Value,
        application.Status,
        application.RejectionReason,
        NextSteps(application),
        application.CreatedAt,
        application.SubmittedAt,
        application.DecidedAt);

    public static StaffApplicationResponse ToStaffResponse(this Application application) => new(
        application.Id.Value,
        application.Market.Value,
        application.Status,
        application.RejectionReason,
        application.Details.FirstName,
        application.Details.LastName,
        application.Details.DateOfBirth,
        application.Details.Email,
        application.Details.Phone,
        application.Identifier.Type,
        application.Identifier.Value,
        application.Documents.Select(d => new DocumentResponse(d.Type, d.ContentType, d.SizeBytes, d.UploadedAt)).ToList(),
        application.IdentityVerificationReference,
        application.ScreeningReference,
        application.ComplianceApproved,
        application.ReviewedBy,
        application.ReviewReason,
        application.ReviewedAt,
        application.ActivatedBy,
        application.ActivationBranchId,
        application.CreatedAt,
        application.SubmittedAt,
        application.DecidedAt);

    public static MarketRequirementsResponse ToRequirementsResponse(this MarketDefinition market, string termsVersion) => new(
        market.Code.Value,
        market.Activation,
        market.AcceptedIdentifiers.Select(i => new AcceptedIdentifierResponse(i.Type, i.Scheme.Description)).ToList(),
        Enum.GetValues<DocumentType>().Select(t => ApiNames.Of(t)).ToList(),
        termsVersion);

    public static ApplicantDetailsRequest ToDetailsRequest(this CreateApplicationRequest request) =>
        new(request.FirstName, request.LastName, request.DateOfBirth, request.Email, request.Phone, request.Identifier);

    /// <summary>Tells the app what to do next. This is what makes "save and resume" work after a tunnel.</summary>
    private static IReadOnlyList<string> NextSteps(Application application) => application.Status switch
    {
        ApplicationStatus.Draft => application.MissingDocuments()
            .Select(type => $"UPLOAD_{ApiNames.Of(type)}")
            .Append("ACCEPT_TERMS_AND_SUBMIT")
            .ToList(),
        ApplicationStatus.Submitted => new[] { "WAIT_FOR_VERIFICATION" },
        ApplicationStatus.PendingComplianceReview => new[] { "WAIT_FOR_COMPLIANCE_REVIEW" },
        ApplicationStatus.AwaitingBranchActivation => new[] { "VISIT_BRANCH_TO_SIGN" },
        _ => Array.Empty<string>(),
    };
}
