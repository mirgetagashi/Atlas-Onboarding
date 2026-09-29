using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.ServiceDefaults.Auth;

/// <summary>
/// Stands in for the bank's identity provider during local development, so every
/// action is still made by a named person or service (GC-2026-0814, point 2), never a shared credential.
/// </summary>
public sealed class DevTokenIssuer
{
    private readonly AtlasAuthOptions _options;
    private readonly TimeProvider _time;

    public DevTokenIssuer(IOptions<AtlasAuthOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
    }

    public static SymmetricSecurityKey CreateKey(string signingKey) => new(SigningKeyEncoding.GetBytes(signingKey));

    public string Issue(string subject, string role, string? market, TimeSpan lifetime)
    {
        var claims = new Dictionary<string, object>
        {
            [AtlasClaims.Subject] = subject,
            [AtlasClaims.Role] = role,
        };
        if (market is not null)
        {
            claims[AtlasClaims.Market] = market;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(lifetime),
            SigningCredentials = new SigningCredentials(CreateKey(_options.SigningKey), SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
