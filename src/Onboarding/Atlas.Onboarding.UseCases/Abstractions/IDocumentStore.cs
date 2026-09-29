using Atlas.Markets;
using Atlas.Onboarding.Domain;

namespace Atlas.Onboarding.UseCases.Abstractions;

/// <summary>Stores identity documents and selfies in the storage of the application's own market.</summary>
public interface IDocumentStore
{
    /// <summary>Streams the content into storage and returns the key to find it again.</summary>
    Task<string> SaveAsync(ApplicationId applicationId, DocumentType type, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(MarketCode market, string storageKey, CancellationToken cancellationToken);
}
