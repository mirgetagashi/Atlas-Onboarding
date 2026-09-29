using System.Net.Http.Headers;
using Atlas.ServiceDefaults.Auth;

namespace Atlas.Verification.Worker.Clients;

/// <summary>
/// Adds this worker's own service token to every call to Onboarding. Its identity
/// ("svc-verification-worker") is what appears in the audit log, not a shared credential.
/// </summary>
public sealed class ServiceTokenHandler : DelegatingHandler
{
    private const string ServiceName = "svc-verification-worker";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(1);

    private readonly DevTokenIssuer _issuer;
    private readonly TimeProvider _time;
    private readonly object _lock = new();
    private string? _token;
    private DateTimeOffset _expiresAt;

    public ServiceTokenHandler(DevTokenIssuer issuer, TimeProvider time)
    {
        _issuer = issuer;
        _time = time;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CurrentToken());
        return base.SendAsync(request, cancellationToken);
    }

    private string CurrentToken()
    {
        lock (_lock)
        {
            var now = _time.GetUtcNow();
            if (_token is null || now >= _expiresAt - RenewBefore)
            {
                _token = _issuer.Issue(ServiceName, AtlasRoles.VerificationService, market: null, TokenLifetime);
                _expiresAt = now + TokenLifetime;
            }

            return _token;
        }
    }
}
