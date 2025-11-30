using FluentAssertions;
using Primus.Documents.LinkStore;
using Xunit;

namespace Primus.Documents.Tests;

public class InMemoryDocumentLinkStoreTests
{
    private readonly IDocumentLinkStore _store;

    public InMemoryDocumentLinkStoreTests()
    {
        _store = new InMemoryDocumentLinkStore();
    }

    [Fact]
    public async Task StoreAsync_ReturnsUniqueToken()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        // Act
        var token1 = await _store.StoreAsync("tenant1", "Doc1", pdfBytes, TimeSpan.FromMinutes(10));
        var token2 = await _store.StoreAsync("tenant1", "Doc2", pdfBytes, TimeSpan.FromMinutes(10));

        // Assert
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public async Task RetrieveAsync_ValidToken_ReturnsDocument()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x01, 0x02, 0x03 };
        var token = await _store.StoreAsync("tenant-123", "My Document", pdfBytes, TimeSpan.FromMinutes(10));

        // Act
        var doc = await _store.RetrieveAsync(token);

        // Assert
        doc.Should().NotBeNull();
        doc!.TenantId.Should().Be("tenant-123");
        doc.Title.Should().Be("My Document");
        doc.PdfBytes.Should().BeEquivalentTo(pdfBytes);
    }

    [Fact]
    public async Task RetrieveAsync_InvalidToken_ReturnsNull()
    {
        // Act
        var doc = await _store.RetrieveAsync("invalid-token");

        // Assert
        doc.Should().BeNull();
    }

    [Fact]
    public async Task RetrieveAsync_SingleUse_SecondCallReturnsNull()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var token = await _store.StoreAsync("tenant", "Doc", pdfBytes, TimeSpan.FromMinutes(10));

        // Act
        var doc1 = await _store.RetrieveAsync(token);
        var doc2 = await _store.RetrieveAsync(token);

        // Assert
        doc1.Should().NotBeNull();
        doc2.Should().BeNull();
    }

    [Fact]
    public async Task RetrieveAsync_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var token = await _store.StoreAsync("tenant", "Doc", pdfBytes, TimeSpan.FromMilliseconds(1));
        
        // Wait for expiry
        await Task.Delay(50);

        // Act
        var doc = await _store.RetrieveAsync(token);

        // Assert
        doc.Should().BeNull();
    }

    [Fact]
    public async Task CleanupExpiredAsync_RemovesExpiredEntries()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var token1 = await _store.StoreAsync("tenant", "Expired", pdfBytes, TimeSpan.FromMilliseconds(1));
        var token2 = await _store.StoreAsync("tenant", "Valid", pdfBytes, TimeSpan.FromMinutes(10));
        
        await Task.Delay(50);

        // Act
        await _store.CleanupExpiredAsync();

        // Assert - token2 should still be valid
        var doc2 = await _store.RetrieveAsync(token2);
        doc2.Should().NotBeNull();
    }
}
