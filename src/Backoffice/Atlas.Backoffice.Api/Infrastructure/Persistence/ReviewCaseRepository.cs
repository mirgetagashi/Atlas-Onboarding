using Atlas.Backoffice.Api.Domain;
using Atlas.Backoffice.Api.UseCases.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Backoffice.Api.Infrastructure.Persistence;

internal sealed class ReviewCaseRepository : IReviewCaseRepository
{
    private readonly BackofficeDbContext _db;

    public ReviewCaseRepository(BackofficeDbContext db) => _db = db;

    public Task<ReviewCase?> FindByApplicationIdAsync(string applicationId, CancellationToken cancellationToken) =>
        _db.ReviewCases.SingleOrDefaultAsync(c => c.ApplicationId == applicationId, cancellationToken);

    public Task<bool> ExistsForApplicationAsync(string applicationId, CancellationToken cancellationToken) =>
        _db.ReviewCases.AnyAsync(c => c.ApplicationId == applicationId, cancellationToken);

    public async Task<IReadOnlyList<ReviewCase>> ListAsync(string market, ReviewCaseStatus status, CancellationToken cancellationToken) =>
        await _db.ReviewCases
            .AsNoTracking()
            .Where(c => c.Market == market && c.Status == status)
            .OrderBy(c => c.DueBy)
            .ToListAsync(cancellationToken);

    public void Add(ReviewCase reviewCase) => _db.ReviewCases.Add(reviewCase);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
