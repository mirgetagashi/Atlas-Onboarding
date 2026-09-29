namespace Atlas.Onboarding.UseCases.Dtos.Requests;

public sealed record BranchActivationRequest(string? BranchId, bool WetSignatureConfirmed);
