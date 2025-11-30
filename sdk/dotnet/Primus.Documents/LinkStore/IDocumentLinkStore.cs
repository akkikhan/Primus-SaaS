using System.Collections.Concurrent;

namespace Primus.Documents.LinkStore;

/// <summary>
/// Interface for storing and retrieving tokenized PDF download links.
/// </summary>
public interface IDocumentLinkStore
{
    /// <summary>
    /// Stores a PDF and returns a download token.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="title">The document title.</param>
    /// <param name="pdfBytes">The PDF content.</param>
    /// <param name="ttl">Time-to-live for the link.</param>
    /// <returns>A unique download token.</returns>
    Task<string> StoreAsync(string tenantId, string title, byte[] pdfBytes, TimeSpan ttl);

    /// <summary>
    /// Retrieves a PDF by token.
    /// </summary>
    /// <param name="token">The download token.</param>
    /// <returns>The stored PDF info, or null if not found/expired.</returns>
    Task<StoredDocument?> RetrieveAsync(string token);

    /// <summary>
    /// Removes expired entries (cleanup).
    /// </summary>
    Task CleanupExpiredAsync();
}

/// <summary>
/// Represents a stored document awaiting download.
/// </summary>
public sealed class StoredDocument
{
    public string TenantId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public byte[] PdfBytes { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// In-memory implementation of document link store.
/// </summary>
internal sealed class InMemoryDocumentLinkStore : IDocumentLinkStore
{
    private readonly ConcurrentDictionary<string, StoredDocument> _store = new();

    public Task<string> StoreAsync(string tenantId, string title, byte[] pdfBytes, TimeSpan ttl)
    {
        var token = Guid.NewGuid().ToString("N");
        var doc = new StoredDocument
        {
            TenantId = tenantId,
            Title = title,
            PdfBytes = pdfBytes,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };

        _store[token] = doc;
        return Task.FromResult(token);
    }

    public Task<StoredDocument?> RetrieveAsync(string token)
    {
        if (_store.TryGetValue(token, out var doc))
        {
            if (doc.ExpiresAt > DateTime.UtcNow)
            {
                // Remove after retrieval (single-use)
                _store.TryRemove(token, out _);
                return Task.FromResult<StoredDocument?>(doc);
            }

            // Expired - remove it
            _store.TryRemove(token, out _);
        }

        return Task.FromResult<StoredDocument?>(null);
    }

    public Task CleanupExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _store
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _store.TryRemove(token, out _);
        }

        return Task.CompletedTask;
    }
}
