namespace Atlas.Onboarding.UseCases.Dtos.Responses;

/// <summary>The only response that ever contains the applicant token. The app must store it securely.</summary>
public sealed record ApplicationCreatedResponse(string ApplicationId, string ApplicantToken, ApplicationResponse Application);
