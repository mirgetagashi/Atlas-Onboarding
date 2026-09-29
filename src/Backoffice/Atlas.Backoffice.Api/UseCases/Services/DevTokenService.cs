using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Backoffice.Api.Dtos.Responses;
using Atlas.Common;
using Atlas.ServiceDefaults.Auth;

namespace Atlas.Backoffice.Api.UseCases.Services;

/// <summary>
/// DEVELOPMENT ONLY: in any other environment it answers "not found", as if it did not exist.
/// Lets reviewers log in as a named compliance officer or branch employee of one market.
/// </summary>
public sealed class DevTokenService : IDevTokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);
    private static readonly string[] StaffRoles = { AtlasRoles.ComplianceOfficer, AtlasRoles.BranchStaff };

    private readonly DevTokenIssuer _issuer;
    private readonly IHostEnvironment _environment;

    public DevTokenService(DevTokenIssuer issuer, IHostEnvironment environment)
    {
        _issuer = issuer;
        _environment = environment;
    }

    public Result<DevTokenResponse> Issue(DevTokenRequest request)
    {
        if (!_environment.IsDevelopment())
        {
            return Error.NotFound();
        }

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            errors["userId"] = new[] { "A named user is required, e.g. \"officer.anna\"." };
        }

        var role = StaffRoles.FirstOrDefault(r => string.Equals(r, request.Role, StringComparison.OrdinalIgnoreCase));
        if (role is null)
        {
            errors["role"] = new[] { $"Role must be one of: {string.Join(", ", StaffRoles)}." };
        }

        if (string.IsNullOrWhiteSpace(request.Market) || request.Market.Trim().Length != 2)
        {
            errors["market"] = new[] { "Staff work for one market, e.g. \"MB\"." };
        }

        if (errors.Count > 0)
        {
            return Error.Validation(errors);
        }

        var token = _issuer.Issue(request.UserId!.Trim(), role!, request.Market!.Trim().ToUpperInvariant(), Lifetime);
        return new DevTokenResponse(token, (int)Lifetime.TotalSeconds);
    }
}
