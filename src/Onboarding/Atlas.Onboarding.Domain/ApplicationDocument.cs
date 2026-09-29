namespace Atlas.Onboarding.Domain;

/// <summary>
/// Metadata of an uploaded document. The bytes live in blob storage of the same market;
/// the database only keeps where to find them.
/// </summary>
public sealed class ApplicationDocument
{
    private ApplicationDocument()
    {
        // For EF Core.
    }

    internal ApplicationDocument(DocumentType type, string storageKey, string contentType, long sizeBytes, DateTimeOffset uploadedAt)
    {
        Type = type;
        Replace(storageKey, contentType, sizeBytes, uploadedAt);
    }

    public DocumentType Type { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    /// <summary>A new photo of the same document replaces the previous one (customer retook the picture).</summary>
    internal void Replace(string storageKey, string contentType, long sizeBytes, DateTimeOffset uploadedAt)
    {
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = uploadedAt;
    }
}
