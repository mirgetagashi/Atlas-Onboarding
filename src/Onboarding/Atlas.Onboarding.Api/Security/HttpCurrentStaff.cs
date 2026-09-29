using System.Security.Claims;
using Atlas.Markets;
using Atlas.Onboarding.UseCases.Abstractions;
using Atlas.ServiceDefaults.Auth;

namespace Atlas.Onboarding.Api.Security;

/// <summary>Reads the current staff member or service from the request's JWT, for the UseCases layer.</summary>
internal sealed class HttpCurrentStaff : ICurrentStaff
{
    private readonly IHttpContextAccessor _httpContext;

    public HttpCurrentStaff(IHttpContextAccessor httpContext) => _httpContext = httpContext;

    public string Id => User.ActorId();

    public bool IsService => User.IsInRole(AtlasRoles.VerificationService);

    public bool IsComplianceOfficer => User.IsInRole(AtlasRoles.ComplianceOfficer);

    public bool CanAccessMarket(MarketCode market) => User.CanAccessMarket(market.Value);

    private ClaimsPrincipal User =>
        _httpContext.HttpContext?.User ?? throw new InvalidOperationException("No HTTP request in progress.");
}
