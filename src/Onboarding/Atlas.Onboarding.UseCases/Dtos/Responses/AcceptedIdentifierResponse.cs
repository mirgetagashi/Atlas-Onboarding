using Atlas.Markets;

namespace Atlas.Onboarding.UseCases.Dtos.Responses;

public sealed record AcceptedIdentifierResponse(IdentifierType Type, string Format);
