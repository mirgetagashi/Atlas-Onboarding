using Atlas.Markets;

namespace Atlas.Onboarding.Domain;

/// <summary>
/// Public id of an application, e.g. "MB-3f2a...". The market prefix lets any service
/// route to the right market's storage without a central lookup table, and contains no personal data.
/// The national id is never used as a key (Annex B, note 2).
/// </summary>
public readonly record struct ApplicationId
{
    private const char Separator = '-';
    private const int GuidLength = 32;

    private ApplicationId(MarketCode market, Guid unique)
    {
        Market = market;
        Value = $"{market.Value}{Separator}{unique:N}";
    }

    public MarketCode Market { get; }

    public string Value { get; }

    public static ApplicationId New(MarketCode market) => new(market, Guid.NewGuid());

    public static bool TryParse(string? input, out ApplicationId id)
    {
        id = default;
        if (input is null || input.Length != 2 + 1 + GuidLength || input[2] != Separator)
        {
            return false;
        }

        if (!MarketCode.TryParse(input[..2], out var market) || !Guid.TryParseExact(input[3..], "N", out var unique))
        {
            return false;
        }

        id = new ApplicationId(market, unique);
        return true;
    }

    public static ApplicationId Parse(string input) =>
        TryParse(input, out var id) ? id : throw new FormatException($"'{input}' is not a valid application id.");

    public override string ToString() => Value;
}
