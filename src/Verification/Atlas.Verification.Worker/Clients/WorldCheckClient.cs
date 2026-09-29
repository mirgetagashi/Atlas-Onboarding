using Atlas.Verification.Worker.Clients.Models;

namespace Atlas.Verification.Worker.Clients;

/// <summary>Refinitiv World-Check: sanctions and PEP screening.</summary>
public sealed class WorldCheckClient
{
    private readonly HttpClient _http;

    public WorldCheckClient(HttpClient http) => _http = http;

    public async Task<SanctionsScreening> ScreenAsync(ProviderRequest request, CancellationToken cancellationToken)
    {
        var response = await ProviderCall.PostAsync<WorldCheckResponse>(_http, "worldcheck/v1/screenings", request, cancellationToken);
        return new SanctionsScreening(response.Result == "POSSIBLE_MATCH", response.CaseId);
    }
}
