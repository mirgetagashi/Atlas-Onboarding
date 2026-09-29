using Atlas.Verification.Worker.Clients.Models;

namespace Atlas.Verification.Worker.Clients;

internal static class ProviderCall
{
    /// <summary>
    /// Sends the application id as an Idempotency-Key, so a retried call does not open a second
    /// case at the provider (the real IDNow / World-Check contracts would need to confirm they honour it).
    /// </summary>
    public static async Task<T> PostAsync<T>(HttpClient http, string path, ProviderRequest body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", body.Reference);

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException($"Empty response from {path}.");
    }
}
