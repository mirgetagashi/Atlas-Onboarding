using Atlas.Markets;
using Atlas.Onboarding.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atlas.Onboarding.Infrastructure.Outbox;

/// <summary>
/// Background loop: reads unpublished outbox rows from every market database and publishes them.
/// If RabbitMQ is down, rows simply wait and are retried on the next tick.
/// Delivery is at-least-once, so every consumer is written to tolerate duplicates.
/// </summary>
internal sealed class OutboxPublisher : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IMarketDbContextFactory _dbFactory;
    private readonly IMarketCatalog _markets;
    private readonly IBus _bus;
    private readonly TimeProvider _time;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IMarketDbContextFactory dbFactory,
        IMarketCatalog markets,
        IBus bus,
        TimeProvider time,
        ILogger<OutboxPublisher> logger)
    {
        _dbFactory = dbFactory;
        _markets = markets;
        _bus = bus;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var market in _markets.All)
            {
                try
                {
                    await PublishPendingAsync(market.Code, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Outbox publishing failed for market {Market}; will retry", market.Code.Value);
                }
            }
        }
    }

    private async Task PublishPendingAsync(MarketCode market, CancellationToken cancellationToken)
    {
        await using var db = _dbFactory.Create(market);

        var pending = await db.OutboxMessages
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                var integrationEvent = message.Deserialize();
                await _bus.Publish(integrationEvent, integrationEvent.GetType(), cancellationToken);
                message.MarkPublished(_time.GetUtcNow());
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                message.MarkFailed(ex.Message);
                _logger.LogWarning(ex, "Could not publish outbox message {MessageId} ({Type})", message.Id, message.Type);
                break; // keep order: do not publish later events before this one
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
