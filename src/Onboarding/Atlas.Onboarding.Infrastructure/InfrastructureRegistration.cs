using Atlas.Markets;
using Atlas.Onboarding.Infrastructure.Outbox;
using Atlas.Onboarding.Infrastructure.Persistence;
using Atlas.Onboarding.Infrastructure.Storage;
using Atlas.Onboarding.UseCases.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atlas.Onboarding.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddOnboardingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MarketStorageOptions>(options =>
            configuration.GetSection(MarketStorageOptions.SectionName).Bind(options.Markets));

        services.AddSingleton<IMarketDbContextFactory, MarketDbContextFactory>();
        services.AddSingleton<IMarketUnitOfWorkFactory, MarketUnitOfWorkFactory>();
        services.AddSingleton<BlobDocumentStore>();
        services.AddSingleton<IDocumentStore>(provider => provider.GetRequiredService<BlobDocumentStore>());

        // Order matters: storage is prepared before the outbox starts polling it.
        services.AddHostedService<MarketStorageInitializer>();
        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}

/// <summary>
/// On startup: checks every market has storage configured, then creates the databases and blob containers.
/// EnsureCreated keeps the local setup to one command; production would use EF migrations instead.
/// </summary>
internal sealed class MarketStorageInitializer : IHostedService
{
    private const int MaxAttempts = 10;

    private readonly IMarketCatalog _markets;
    private readonly IOptions<MarketStorageOptions> _storage;
    private readonly IMarketDbContextFactory _dbFactory;
    private readonly BlobDocumentStore _documents;
    private readonly ILogger<MarketStorageInitializer> _logger;

    public MarketStorageInitializer(
        IMarketCatalog markets,
        IOptions<MarketStorageOptions> storage,
        IMarketDbContextFactory dbFactory,
        BlobDocumentStore documents,
        ILogger<MarketStorageInitializer> logger)
    {
        _markets = markets;
        _storage = storage;
        _dbFactory = dbFactory;
        _documents = documents;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var unconfigured = _markets.All
            .Where(m => !_storage.Value.Markets.ContainsKey(m.Code.Value))
            .Select(m => m.Code.Value)
            .ToList();
        if (unconfigured.Count > 0)
        {
            throw new InvalidOperationException($"No storage configured for markets: {string.Join(", ", unconfigured)}.");
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                foreach (var market in _markets.All)
                {
                    await using var db = _dbFactory.Create(market.Code);
                    await db.Database.EnsureCreatedAsync(cancellationToken);
                }

                await _documents.EnsureContainersExistAsync(cancellationToken);
                _logger.LogInformation("Storage ready for {MarketCount} markets", _markets.All.Count);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Storage not ready (attempt {Attempt}/{Max}), retrying", attempt, MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
