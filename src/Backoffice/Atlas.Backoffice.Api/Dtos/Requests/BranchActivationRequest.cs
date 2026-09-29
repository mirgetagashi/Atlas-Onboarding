namespace Atlas.Backoffice.Api.Dtos.Requests;

public sealed record BranchActivationRequest(string? ApplicationId, string? BranchId, bool WetSignatureConfirmed);
