using System.Security.Claims;
using Atlas.Backoffice.Api.UseCases.Abstractions;
using Atlas.ServiceDefaults.Auth;

namespace Atlas.Backoffice.Api.Infrastructure.Security;

/// <summary>Reads the logged-in staff member from the request's JWT, for the UseCases layer.</summary>
internal sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;

    public HttpCurrentUser(IHttpContextAccessor httpContext) => _httpContext = httpContext;

    public string Id => User.ActorId();

    public string? Market => User.MarketClaim();

    public bool CanAccessMarket(string market) => User.CanAccessMarket(market);

    private ClaimsPrincipal User =>
        _httpContext.HttpContext?.User ?? throw new InvalidOperationException("No HTTP request in progress.");
}
