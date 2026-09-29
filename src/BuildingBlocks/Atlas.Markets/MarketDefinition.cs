namespace Atlas.Markets;

/// <summary>How an approved application becomes an active account (Annex B, "Activation").</summary>
public enum ActivationMode
{
    /// <summary>The account can be activated without the customer visiting a branch.</summary>
    Remote,

    /// <summary>The customer must attend a branch and give a wet signature first (MD).</summary>
    InBranch,
}

public enum IdentifierType
{
    NationalId,
    Passport,
}

/// <summary>One kind of identifier a market accepts, and the format rule it must follow.</summary>
public sealed record AcceptedIdentifier(IdentifierType Type, IIdentifierScheme Scheme);

/// <summary>Everything that differs between markets. Market differences are configuration, not code branches.</summary>
public sealed record MarketDefinition(
    MarketCode Code,
    ActivationMode Activation,
    IReadOnlyList<AcceptedIdentifier> AcceptedIdentifiers)
{
    public AcceptedIdentifier? FindIdentifier(IdentifierType type) =>
        AcceptedIdentifiers.FirstOrDefault(i => i.Type == type);
}
