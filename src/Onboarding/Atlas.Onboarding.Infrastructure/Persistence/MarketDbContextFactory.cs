using Atlas.Markets;
using Atlas.Onboarding.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Atlas.Onboarding.Infrastructure.Persistence;

/// <summary>Gives you a DbContext connected to one market's own database.</summary>
internal interface IMarketDbContextFactory
{
    OnboardingDbContext Create(MarketCode market);
}

internal sealed class MarketDbContextFactory : IMarketDbContextFactory
{
    private readonly IReadOnlyDictionary<MarketCode, DbContextOptions<OnboardingDbContext>> _optionsByMarket;

    public MarketDbContextFactory(IOptions<MarketStorageOptions> storage)
    {
        _optionsByMarket = storage.Value.Markets.ToDictionary(
            entry => MarketCode.Parse(entry.Key),
            entry => new DbContextOptionsBuilder<OnboardingDbContext>()
                .UseSqlServer(entry.Value.Database, sql => sql.EnableRetryOnFailure())
                .Options);
    }

    public OnboardingDbContext Create(MarketCode market) =>
        _optionsByMarket.TryGetValue(market, out var options)
            ? new OnboardingDbContext(options)
            : throw new InvalidOperationException($"No database is configured for market {market}.");
}
