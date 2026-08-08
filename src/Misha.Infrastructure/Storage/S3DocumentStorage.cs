using Amazon.S3;
using Amazon.S3.Model;
using Misha.Application.Documents;

namespace Misha.Infrastructure.Storage;

public sealed class S3DocumentStorage(IAmazonS3 client, string bucketName) : IDocumentStorage
{
    public async Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Document storage S3 bucket is not configured.");

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        if (content is null)
            throw new ArgumentNullException(nameof(content));

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType
        };

        await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Document storage S3 bucket is not configured.");

        await client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = storageKey
        }, cancellationToken);
    }
}
