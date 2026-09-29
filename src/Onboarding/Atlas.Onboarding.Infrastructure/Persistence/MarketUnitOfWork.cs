using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.Infrastructure.Auditing;
using Atlas.Onboarding.UseCases.Abstractions;
using Atlas.Onboarding.UseCases.Auditing;
using Atlas.Onboarding.UseCases.Shared;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Onboarding.Infrastructure.Persistence;

/// <summary>EF Core implementation of the use cases' unit of work, bound to one market's database.</summary>
internal sealed class MarketUnitOfWork : IMarketUnitOfWork
{
    private readonly OnboardingDbContext _db;

    public MarketUnitOfWork(OnboardingDbContext db) => _db = db;

    public Task<Application?> FindApplicationAsync(ApplicationId id, CancellationToken cancellationToken) =>
        _db.Applications.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public void AddApplication(Application application) => _db.Applications.Add(application);

    public void Audit(ApplicationId applicationId, Actor actor, AuditAction action, string purpose, DateTimeOffset at) =>
        _db.AuditEntries.Add(AuditEntry.Create(applicationId, actor, action, purpose, at));

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Translate the EF-specific exception, so the layers above do not depend on EF Core.
            throw new ConcurrencyConflictException(ex);
        }
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}

internal sealed class MarketUnitOfWorkFactory : IMarketUnitOfWorkFactory
{
    private readonly IMarketDbContextFactory _dbFactory;

    public MarketUnitOfWorkFactory(IMarketDbContextFactory dbFactory) => _dbFactory = dbFactory;

    public IMarketUnitOfWork Create(MarketCode market) => new MarketUnitOfWork(_dbFactory.Create(market));
}
