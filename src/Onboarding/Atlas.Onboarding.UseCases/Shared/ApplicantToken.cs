using System.Security.Cryptography;
using System.Text;

namespace Atlas.Onboarding.UseCases.Shared;

/// <summary>
/// The applicant has no bank login yet. When a draft is created they receive a random secret;
/// every later call sends it in the X-Applicant-Token header. Only its SHA-256 hash is stored,
/// so a database leak does not give access to applications.
/// </summary>
public static class ApplicantToken
{
    public const string HeaderName = "X-Applicant-Token";

    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Matches(string? presentedToken, string storedHash)
    {
        if (string.IsNullOrEmpty(presentedToken))
        {
            return false;
        }

        // Constant-time comparison, so response timing reveals nothing about the hash.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(presentedToken)),
            Encoding.ASCII.GetBytes(storedHash));
    }
}
