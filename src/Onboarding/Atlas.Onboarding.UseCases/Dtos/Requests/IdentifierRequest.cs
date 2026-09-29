using Atlas.Markets;

namespace Atlas.Onboarding.UseCases.Dtos.Requests;

/// <summary>A national id, or a passport number where the market allows it (MF).</summary>
public sealed record IdentifierRequest(IdentifierType? Type, string? Value);
