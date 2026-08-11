using Amazon.S3;
using Misha.Infrastructure.Storage;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class S3DocumentStorageTests
{
    [Fact]
    public async Task UploadAsync_rejects_empty_storage_key_before_network_call()
    {
        await using var client = CreateClient();
        var storage = new S3DocumentStorage(client, "misha-test-documents");
        await using var content = new MemoryStream([1, 2, 3]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.UploadAsync("", content, "application/pdf", CancellationToken.None));

        Assert.Contains("Storage key", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAsync_rejects_path_traversal_key_before_network_call()
    {
        await using var client = CreateClient();
        var storage = new S3DocumentStorage(client, "misha-test-documents");
        await using var content = new MemoryStream([1, 2, 3]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.UploadAsync("applications/../documents/file.pdf", content, "application/pdf", CancellationToken.None));

        Assert.Contains("invalid path segment", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_rejects_header_injection_content_type_before_network_call()
    {
        await using var client = CreateClient();
        var storage = new S3DocumentStorage(client, "misha-test-documents");
        await using var content = new MemoryStream([1, 2, 3]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.UploadAsync("applications/test/documents/file.pdf", content, "application/pdf\r\nX-Injected: true", CancellationToken.None));

        Assert.Contains("content type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_rejects_missing_bucket_configuration_before_network_call()
    {
        await using var client = CreateClient();
        var storage = new S3DocumentStorage(client, "");
        await using var content = new MemoryStream([1, 2, 3]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.UploadAsync("applications/test/documents/file.pdf", content, "application/pdf", CancellationToken.None));

        Assert.Contains("bucket", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static AmazonS3Client CreateClient() =>
        new(new AmazonS3Config
        {
            ServiceURL = "http://127.0.0.1:1",
            ForcePathStyle = true
        });
}
