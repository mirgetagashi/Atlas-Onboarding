using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Atlas.Markets;

public interface IMarketCatalog
{
    IReadOnlyCollection<MarketDefinition> All { get; }

    bool TryGet(MarketCode code, [NotNullWhen(true)] out MarketDefinition? market);
}

public sealed class MarketCatalog : IMarketCatalog
{
    public const string SectionName = "Markets";

    private readonly IReadOnlyDictionary<MarketCode, MarketDefinition> _markets;

    public MarketCatalog(IEnumerable<MarketDefinition> markets)
    {
        _markets = markets.ToDictionary(m => m.Code);
        if (_markets.Count == 0)
        {
            throw new InvalidOperationException("No markets are configured.");
        }
    }

    public IReadOnlyCollection<MarketDefinition> All => _markets.Values.ToList();

    public bool TryGet(MarketCode code, [NotNullWhen(true)] out MarketDefinition? market) =>
        _markets.TryGetValue(code, out market);

    /// <summary>Builds the catalog from the "Markets" section. Fails fast on any configuration mistake.</summary>
    public static MarketCatalog FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName).Get<Dictionary<string, MarketOptions>>()
            ?? throw new InvalidOperationException($"Configuration section '{SectionName}' is missing.");

        var definitions = section.Select(entry =>
        {
            var code = MarketCode.Parse(entry.Key);
            if (entry.Value.Identifiers.Count == 0)
            {
                throw new InvalidOperationException($"Market {code} accepts no identifiers.");
            }

            var identifiers = entry.Value.Identifiers
                .Select(i => new AcceptedIdentifier(i.Type, IdentifierSchemes.Get(i.Scheme)))
                .ToList();

            return new MarketDefinition(code, entry.Value.Activation, identifiers);
        });

        return new MarketCatalog(definitions);
    }

    internal sealed class MarketOptions
    {
        public ActivationMode Activation { get; set; }

        public List<IdentifierOptions> Identifiers { get; set; } = new();
    }

    internal sealed class IdentifierOptions
    {
        public IdentifierType Type { get; set; }

        public string Scheme { get; set; } = string.Empty;
    }
}
