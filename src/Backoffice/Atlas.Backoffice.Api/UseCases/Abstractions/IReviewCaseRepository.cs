using Atlas.Backoffice.Api.Domain;

namespace Atlas.Backoffice.Api.UseCases.Abstractions;

public interface IReviewCaseRepository
{
    Task<ReviewCase?> FindByApplicationIdAsync(string applicationId, CancellationToken cancellationToken);

    Task<bool> ExistsForApplicationAsync(string applicationId, CancellationToken cancellationToken);

    /// <summary>Cases of one market in one status, most urgent (earliest due) first.</summary>
    Task<IReadOnlyList<ReviewCase>> ListAsync(string market, ReviewCaseStatus status, CancellationToken cancellationToken);

    void Add(ReviewCase reviewCase);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
