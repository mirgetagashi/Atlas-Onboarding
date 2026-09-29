using System.Text.Json;
using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Common;

namespace Atlas.Backoffice.Api.UseCases.Services;

/// <summary>Market MD (Annex B): branch staff confirm the customer's wet signature, which activates the account.</summary>
public interface IBranchActivationService
{
    Task<Result<JsonElement>> ConfirmAsync(BranchActivationRequest request, CancellationToken cancellationToken);
}
