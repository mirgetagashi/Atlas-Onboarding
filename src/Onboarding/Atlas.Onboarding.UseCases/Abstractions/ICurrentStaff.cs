using Atlas.Markets;

namespace Atlas.Onboarding.UseCases.Abstractions;

/// <summary>
/// The authenticated staff member or service making the current request.
/// Implemented by the API from the JWT; the use cases never see HTTP or claims directly.
/// </summary>
public interface ICurrentStaff
{
    string Id { get; }

    bool IsService { get; }

    bool IsComplianceOfficer { get; }

    /// <summary>Staff may only touch records of their own market. Services are not market-scoped.</summary>
    bool CanAccessMarket(MarketCode market);
}
