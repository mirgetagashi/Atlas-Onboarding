using Atlas.Verification.Worker.Clients.Models;

namespace Atlas.Verification.Worker.Clients;

/// <summary>IDNow: document authenticity + face match. Retries/timeouts come from the resilience handler in Program.cs.</summary>
public sealed class IdNowClient
{
    private readonly HttpClient _http;

    public IdNowClient(HttpClient http) => _http = http;

    public async Task<IdentityCheck> VerifyAsync(ProviderRequest request, CancellationToken cancellationToken)
    {
        var response = await ProviderCall.PostAsync<IdNowResponse>(_http, "idnow/v1/identifications", request, cancellationToken);
        return new IdentityCheck(response.Result == "SUCCESS", response.IdentificationId);
    }
}
