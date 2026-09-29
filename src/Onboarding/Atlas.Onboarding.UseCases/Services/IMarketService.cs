using Atlas.Common;
using Atlas.Onboarding.UseCases.Dtos.Responses;

namespace Atlas.Onboarding.UseCases.Services;

/// <summary>Market rules for the mobile app (which ids are accepted, whether a branch visit is needed).</summary>
public interface IMarketService
{
    IReadOnlyList<MarketRequirementsResponse> GetAll();

    Result<MarketRequirementsResponse> GetRequirements(string market);
}
