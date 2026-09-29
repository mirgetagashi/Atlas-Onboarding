using Atlas.Providers.Mock.Models;

namespace Atlas.Providers.Mock.Services;

/// <summary>
/// Fixed rules so every path can be tested on purpose:
/// last name "Forged" fails the identity check, a last name containing "Match" gives a possible
/// sanctions match, anything else passes both checks.
/// </summary>
public sealed class MockProviderRules
{
    private readonly ILogger<MockProviderRules> _logger;

    public MockProviderRules(ILogger<MockProviderRules> logger) => _logger = logger;

    public IdentificationResponse Identify(IdentificationRequest request)
    {
        var result = string.Equals(request.LastName, "Forged", StringComparison.OrdinalIgnoreCase)
            ? "FRAUD_SUSPECTED"
            : "SUCCESS";

        _logger.LogInformation("IDNow mock: {Reference} -> {Result}", request.Reference, result);
        return new IdentificationResponse($"idn_{Guid.NewGuid():N}", result);
    }

    public ScreeningResponse Screen(ScreeningRequest request)
    {
        var result = request.LastName.Contains("Match", StringComparison.OrdinalIgnoreCase)
            ? "POSSIBLE_MATCH"
            : "NO_MATCH";

        _logger.LogInformation("World-Check mock: {Reference} -> {Result}", request.Reference, result);
        return new ScreeningResponse($"wc_{Guid.NewGuid():N}", result);
    }
}
