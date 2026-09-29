namespace Atlas.Onboarding.UseCases.Dtos.Requests;

public sealed record ComplianceDecisionRequest(ComplianceDecision? Decision, string? Reason);
