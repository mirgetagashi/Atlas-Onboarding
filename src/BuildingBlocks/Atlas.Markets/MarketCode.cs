namespace Atlas.Markets;

/// <summary>
/// An internal market code such as "MB". Only checks the shape (two letters);
/// whether the market is actually supported is answered by <see cref="IMarketCatalog"/>.
/// </summary>
public readonly record struct MarketCode
{
    private MarketCode(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse(string? input, out MarketCode code)
    {
        code = default;
        if (input is null)
        {
            return false;
        }

        var normalized = input.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || !normalized.All(c => c is >= 'A' and <= 'Z'))
        {
            return false;
        }

        code = new MarketCode(normalized);
        return true;
    }

    public static MarketCode Parse(string input) =>
        TryParse(input, out var code)
            ? code
            : throw new FormatException($"'{input}' is not a valid market code.");

    public override string ToString() => Value;
}
