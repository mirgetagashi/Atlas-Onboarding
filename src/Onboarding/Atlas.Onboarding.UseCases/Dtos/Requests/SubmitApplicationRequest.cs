namespace Atlas.Onboarding.UseCases.Dtos.Requests;

public sealed record SubmitApplicationRequest(bool TermsAccepted, string? TermsVersion);
