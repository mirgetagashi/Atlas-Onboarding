using System.Net.Http.Headers;

namespace Atlas.Backoffice.Api.Infrastructure.Onboarding;

/// <summary>
/// Copies the caller's "Authorization: Bearer ..." header onto calls to Onboarding, so Onboarding
/// authorises and audits the real person, not a shared "Backoffice" credential.
/// </summary>
internal sealed class ForwardUserTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContext;

    public ForwardUserTokenHandler(IHttpContextAccessor httpContext) => _httpContext = httpContext;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authorization = _httpContext.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization) && AuthenticationHeaderValue.TryParse(authorization, out var header))
        {
            request.Headers.Authorization = header;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
