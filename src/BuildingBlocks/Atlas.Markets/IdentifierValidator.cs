namespace Atlas.Markets;

public sealed record IdentifierCheck(bool IsValid, string? NormalizedValue, string? Error)
{
    public static IdentifierCheck Valid(string normalized) => new(true, normalized, null);

    public static IdentifierCheck Invalid(string error) => new(false, null, error);
}

/// <summary>Validates a personal identifier against the rules of one market.</summary>
public static class IdentifierValidator
{
    /// <summary>Removes spaces and dashes and upper-cases, so "0403 991-450016" and "0403991450016" are the same value.</summary>
    public static string Normalize(string raw) =>
        new string(raw.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();

    public static IdentifierCheck Validate(MarketDefinition market, IdentifierType type, string? rawValue)
    {
        var accepted = market.FindIdentifier(type);
        if (accepted is null)
        {
            var allowed = string.Join(", ", market.AcceptedIdentifiers.Select(i => i.Type));
            return IdentifierCheck.Invalid($"Market {market.Code} does not accept {type}. Accepted: {allowed}.");
        }

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return IdentifierCheck.Invalid("Identifier value is required.");
        }

        var normalized = Normalize(rawValue);
        return accepted.Scheme.IsValid(normalized)
            ? IdentifierCheck.Valid(normalized)
            : IdentifierCheck.Invalid($"Invalid {type} for market {market.Code}: expected {accepted.Scheme.Description}.");
    }
}
