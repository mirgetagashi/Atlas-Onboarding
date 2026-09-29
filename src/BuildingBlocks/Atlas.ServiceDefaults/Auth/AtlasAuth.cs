using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.ServiceDefaults.Auth;

/// <summary>Claim names used in every Atlas token.</summary>
public static class AtlasClaims
{
    public const string Subject = "sub";
    public const string Role = "role";

    /// <summary>The market a staff member works in. Officers only see their own market (GC-2026-0814, point 3).</summary>
    public const string Market = "market";
}

public static class AtlasRoles
{
    public const string ComplianceOfficer = "ComplianceOfficer";
    public const string BranchStaff = "BranchStaff";
    public const string VerificationService = "VerificationService";

    public static readonly IReadOnlyList<string> All = new[] { ComplianceOfficer, BranchStaff, VerificationService };
}

public sealed class AtlasAuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "atlas-dev-idp";

    public string Audience { get; set; } = "atlas";

    /// <summary>Development only. In production tokens come from the bank's identity provider.</summary>
    public string SigningKey { get; set; } = string.Empty;
}

public static class AtlasAuthExtensions
{
    /// <summary>Validates staff/service JWTs and registers one authorization policy per role.</summary>
    public static IServiceCollection AddAtlasJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var options = services.AddAtlasTokenIssuer(configuration);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MapInboundClaims = false; // keep "sub" and "role" as they are
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = DevTokenIssuer.CreateKey(options.SigningKey),
                    NameClaimType = AtlasClaims.Subject,
                    RoleClaimType = AtlasClaims.Role,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization(authorization =>
        {
            foreach (var role in AtlasRoles.All)
            {
                authorization.AddPolicy(role, policy => policy.RequireRole(role));
            }
        });

        return services;
    }

    /// <summary>Registers <see cref="DevTokenIssuer"/> (used by the Backoffice dev login and the worker's service token).</summary>
    public static AtlasAuthOptions AddAtlasTokenIssuer(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(AtlasAuthOptions.SectionName);
        var options = section.Get<AtlasAuthOptions>() ?? new AtlasAuthOptions();
        if (options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Auth:SigningKey must be at least 32 characters.");
        }

        services.Configure<AtlasAuthOptions>(section);
        services.AddSingleton<DevTokenIssuer>();
        return options;
    }

    public static string ActorId(this ClaimsPrincipal user) =>
        user.FindFirst(AtlasClaims.Subject)?.Value
        ?? throw new InvalidOperationException("Authenticated principal has no subject claim.");

    public static string? MarketClaim(this ClaimsPrincipal user) => user.FindFirst(AtlasClaims.Market)?.Value;

    /// <summary>Staff may only touch records of their own market. Services are not market-scoped.</summary>
    public static bool CanAccessMarket(this ClaimsPrincipal user, string market) =>
        user.IsInRole(AtlasRoles.VerificationService)
        || string.Equals(user.MarketClaim(), market, StringComparison.OrdinalIgnoreCase);
}

internal static class SigningKeyEncoding
{
    public static byte[] GetBytes(string key) => Encoding.UTF8.GetBytes(key);
}
