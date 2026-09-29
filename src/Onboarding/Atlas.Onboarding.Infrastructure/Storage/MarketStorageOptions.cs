namespace Atlas.Onboarding.Infrastructure.Storage;

/// <summary>
/// Where one market's data lives. Each market has its own database and its own blob container,
/// so customer data never leaves its country (GC-2026-0814, point 1). Locally all six point at the same
/// SQL Server and Azurite; in production each entry would point at infrastructure inside that country.
/// </summary>
public sealed class MarketStorageEntry
{
    public string Database { get; set; } = string.Empty;

    public string BlobConnectionString { get; set; } = string.Empty;

    public string BlobContainer { get; set; } = string.Empty;
}

public sealed class MarketStorageOptions
{
    public const string SectionName = "MarketStorage";

    public Dictionary<string, MarketStorageEntry> Markets { get; } = new(StringComparer.OrdinalIgnoreCase);
}
