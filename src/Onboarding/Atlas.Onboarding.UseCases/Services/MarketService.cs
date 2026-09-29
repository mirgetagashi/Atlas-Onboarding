using Atlas.Common;
using Atlas.Markets;
using Atlas.Onboarding.UseCases.Dtos.Responses;
using Atlas.Onboarding.UseCases.Mapping;
using Atlas.Onboarding.UseCases.Shared;
using Microsoft.Extensions.Options;

namespace Atlas.Onboarding.UseCases.Services;

public sealed class MarketService : IMarketService
{
    private readonly IMarketCatalog _markets;
    private readonly OnboardingOptions _options;

    public MarketService(IMarketCatalog markets, IOptions<OnboardingOptions> options)
    {
        _markets = markets;
        _options = options.Value;
    }

    public IReadOnlyList<MarketRequirementsResponse> GetAll() =>
        _markets.All
            .OrderBy(m => m.Code.Value)
            .Select(m => m.ToRequirementsResponse(_options.CurrentTermsVersion))
            .ToList();

    public Result<MarketRequirementsResponse> GetRequirements(string market)
    {
        if (!MarketCode.TryParse(market, out var code) || !_markets.TryGet(code, out var definition))
        {
            return Error.NotFound($"Unknown market '{market}'.");
        }

        return definition.ToRequirementsResponse(_options.CurrentTermsVersion);
    }
}
