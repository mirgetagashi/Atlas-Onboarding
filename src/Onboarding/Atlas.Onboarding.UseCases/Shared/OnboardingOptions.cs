namespace Atlas.Onboarding.UseCases.Shared;

public sealed class OnboardingOptions
{
    public const string SectionName = "Onboarding";

    /// <summary>The terms the customer must accept. An older version is refused, so consent is always to the current text.</summary>
    public string CurrentTermsVersion { get; set; } = "2026-09";

    /// <summary>Assumption: retail accounts are for adults only (minors are not mentioned in the requirements).</summary>
    public int MinimumAgeYears { get; set; } = 18;

    public long MaxDocumentBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedDocumentContentTypes { get; set; } = { "image/jpeg", "image/png" };
}
