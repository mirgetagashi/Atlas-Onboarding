using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Abstractions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Atlas.Onboarding.Infrastructure.Storage;

/// <summary>Azure Blob Storage implementation: one container per market, so documents never leave their country.</summary>
internal sealed class BlobDocumentStore : IDocumentStore
{
    private readonly IReadOnlyDictionary<MarketCode, BlobContainerClient> _containers;

    public BlobDocumentStore(IOptions<MarketStorageOptions> storage)
    {
        _containers = storage.Value.Markets.ToDictionary(
            entry => MarketCode.Parse(entry.Key),
            entry => new BlobServiceClient(entry.Value.BlobConnectionString).GetBlobContainerClient(entry.Value.BlobContainer));
    }

    public async Task<string> SaveAsync(
        ApplicationId applicationId,
        DocumentType type,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        // A new key per upload: nothing is ever overwritten, so a retaken photo cannot corrupt the previous one.
        var storageKey = $"{applicationId.Value}/{type}-{Guid.NewGuid():N}";
        var blob = ContainerFor(applicationId.Market).GetBlobClient(storageKey);

        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return storageKey;
    }

    public Task<Stream> OpenReadAsync(MarketCode market, string storageKey, CancellationToken cancellationToken) =>
        ContainerFor(market).GetBlobClient(storageKey).OpenReadAsync(new BlobOpenReadOptions(allowModifications: false), cancellationToken);

    public async Task EnsureContainersExistAsync(CancellationToken cancellationToken)
    {
        foreach (var container in _containers.Values)
        {
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }
    }

    private BlobContainerClient ContainerFor(MarketCode market) =>
        _containers.TryGetValue(market, out var container)
            ? container
            : throw new InvalidOperationException($"No blob container is configured for market {market}.");
}
