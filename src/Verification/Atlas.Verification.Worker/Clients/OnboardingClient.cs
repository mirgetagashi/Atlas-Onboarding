using Atlas.Verification.Worker.Clients.Models;

namespace Atlas.Verification.Worker.Clients;

/// <summary>Reads applicant data from Onboarding's internal API. The event itself carries no personal data.</summary>
public sealed class OnboardingClient
{
    private readonly HttpClient _http;

    public OnboardingClient(HttpClient http) => _http = http;

    public async Task<ApplicantSnapshot> GetApplicantAsync(string applicationId, CancellationToken cancellationToken) =>
        await _http.GetFromJsonAsync<ApplicantSnapshot>($"internal/applications/{Uri.EscapeDataString(applicationId)}", cancellationToken)
        ?? throw new InvalidOperationException($"Onboarding returned no data for {applicationId}.");
}
