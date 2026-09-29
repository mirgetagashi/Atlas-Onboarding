using System.Text.Json;
using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Backoffice.Api.UseCases.Abstractions;
using Atlas.Common;

namespace Atlas.Backoffice.Api.UseCases.Services;

public sealed class BranchActivationService : IBranchActivationService
{
    private readonly IOnboardingGateway _onboarding;

    public BranchActivationService(IOnboardingGateway onboarding) => _onboarding = onboarding;

    public async Task<Result<JsonElement>> ConfirmAsync(BranchActivationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ApplicationId))
        {
            return Error.Validation("applicationId", "Application id is required.");
        }

        // Onboarding checks the staff member's market, the application's status and the signature
        // confirmation, and audits the activation against the staff member.
        return await _onboarding.ConfirmBranchActivationAsync(
            request.ApplicationId,
            request.BranchId,
            request.WetSignatureConfirmed,
            cancellationToken);
    }
}
