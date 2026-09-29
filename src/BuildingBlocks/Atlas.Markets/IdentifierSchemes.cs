using System.Text.RegularExpressions;

namespace Atlas.Markets;

/// <summary>A format rule for a personal identifier. Values are normalized before checking.</summary>
public interface IIdentifierScheme
{
    string Name { get; }

    /// <summary>Human-readable format, returned to mobile so it can show a hint.</summary>
    string Description { get; }

    bool IsValid(string normalizedValue);
}

internal sealed class RegexIdentifierScheme : IIdentifierScheme
{
    private readonly Regex _pattern;

    public RegexIdentifierScheme(string name, string pattern, string description)
    {
        Name = name;
        Description = description;
        _pattern = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    }

    public string Name { get; }

    public string Description { get; }

    public bool IsValid(string normalizedValue) => _pattern.IsMatch(normalizedValue);
}

/// <summary>
/// The known identifier formats. Only what Annex B actually states is enforced:
/// check-digit algorithms are not given for any market, so none is invented here
/// (see OPEN-QUESTIONS.md). Adding one later means adding a scheme, not touching callers.
/// </summary>
public static class IdentifierSchemes
{
    public static readonly IIdentifierScheme Digits13 =
        new RegexIdentifierScheme("Digits13", "^[0-9]{13}$", "13 digits");

    public static readonly IIdentifierScheme Digits10 =
        new RegexIdentifierScheme("Digits10", "^[0-9]{10}$", "10 digits");

    public static readonly IIdentifierScheme UnifiedCitizenId9 =
        new RegexIdentifierScheme("UnifiedCitizenId9", "^[A-Z0-9]{8}[A-Z]$", "9 alphanumeric characters, a letter in position 9");

    /// <summary>Annex B gives no format for the MF Personal Number, so only a loose sanity check is applied.</summary>
    public static readonly IIdentifierScheme UnspecifiedNationalId =
        new RegexIdentifierScheme("UnspecifiedNationalId", "^[A-Z0-9]{5,20}$", "5 to 20 letters or digits");

    public static readonly IIdentifierScheme Passport =
        new RegexIdentifierScheme("Passport", "^[A-Z0-9]{6,9}$", "passport number, 6 to 9 letters or digits");

    private static readonly IReadOnlyDictionary<string, IIdentifierScheme> ByName =
        new[] { Digits13, Digits10, UnifiedCitizenId9, UnspecifiedNationalId, Passport }
            .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

    public static IIdentifierScheme Get(string name) =>
        ByName.TryGetValue(name, out var scheme)
            ? scheme
            : throw new InvalidOperationException($"Unknown identifier scheme '{name}'. Known: {string.Join(", ", ByName.Keys)}.");
}
