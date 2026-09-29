using Atlas.Onboarding.Domain;
using Atlas.Onboarding.Infrastructure.Auditing;
using Atlas.Onboarding.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Onboarding.Infrastructure.Persistence;

/// <summary>
/// One instance always talks to exactly one market's database (see <see cref="IMarketDbContextFactory"/>).
/// </summary>
public sealed class OnboardingDbContext : DbContext
{
    public OnboardingDbContext(DbContextOptions<OnboardingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Application> Applications => Set<Application>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException("Use SaveChangesAsync.");

    /// <summary>
    /// Domain events are written to the outbox table in the SAME transaction as the state change.
    /// Either both are saved or neither: no "status changed but the event was lost".
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        MoveDomainEventsToOutbox();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OnboardingDbContext).Assembly);

    private void MoveDomainEventsToOutbox()
    {
        var applications = ChangeTracker.Entries<Application>()
            .Select(entry => entry.Entity)
            .Where(application => application.DomainEvents.Count > 0)
            .ToList();

        foreach (var application in applications)
        {
            foreach (var domainEvent in application.DomainEvents)
            {
                var integrationEvent = IntegrationEventMapper.ToIntegrationEvent(domainEvent);
                if (integrationEvent is not null)
                {
                    OutboxMessages.Add(OutboxMessage.From(integrationEvent, domainEvent.OccurredAt));
                }
            }

            application.ClearDomainEvents();
        }
    }
}
