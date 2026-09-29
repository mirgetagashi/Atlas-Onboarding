using Atlas.Markets;

namespace Atlas.Onboarding.UseCases.Dtos.Responses;

/// <summary>Lets the mobile app build its screens from configuration instead of market-specific code.</summary>
public sealed record MarketRequirementsResponse(
    string Market,
    ActivationMode Activation,
    IReadOnlyList<AcceptedIdentifierResponse> AcceptedIdentifiers,
    IReadOnlyList<string> RequiredDocuments,
    string TermsVersion);
