using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Auditing;

namespace Atlas.Onboarding.UseCases.Abstractions;

/// <summary>
/// A unit of work on ONE market's storage. Everything added (application changes, audit rows,
/// outgoing events) is saved together by <see cref="SaveChangesAsync"/>, or not at all.
/// </summary>
public interface IMarketUnitOfWork : IAsyncDisposable
{
    Task<Application?> FindApplicationAsync(ApplicationId id, CancellationToken cancellationToken);

    void AddApplication(Application application);

    void Audit(ApplicationId applicationId, Actor actor, AuditAction action, string purpose, DateTimeOffset at);

    /// <summary>Throws <see cref="Shared.ConcurrencyConflictException"/> if another request changed the same application.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Opens a unit of work on the storage of the given market (data residency: one store per country).</summary>
public interface IMarketUnitOfWorkFactory
{
    IMarketUnitOfWork Create(MarketCode market);
}
