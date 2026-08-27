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
        ValidateConfiguration();
        ValidateStorageKey(storageKey);

        if (content is null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(contentType) || contentType.Contains('\r') || contentType.Contains('\n'))
            throw new ArgumentException("A valid content type is required.", nameof(contentType));

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType.Trim()
        };

        await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        ValidateStorageKey(storageKey);

        await client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = storageKey
            },
            cancellationToken);
    }

    public Uri CreatePreSignedUploadUrl(string storageKey, string contentType, TimeSpan lifetime)
    {
        ValidateConfiguration();
        ValidateStorageKey(storageKey);
        ValidateLifetime(lifetime);
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 127 || contentType.Contains('\r') || contentType.Contains('\n'))
            throw new ArgumentException("A valid content type is required.", nameof(contentType));

        return new Uri(client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(lifetime),
            ContentType = contentType.Trim()
        }));
    }

    public Uri CreatePreSignedDownloadUrl(string storageKey, TimeSpan lifetime)
    {
        ValidateConfiguration();
        ValidateStorageKey(storageKey);
        ValidateLifetime(lifetime);

        return new Uri(client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime)
        }));
    }

    private void ValidateConfiguration()
    {
        if (client is null)
            throw new InvalidOperationException("Document storage S3 client is not configured.");

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Document storage S3 bucket is not configured.");
    }

    private static void ValidateLifetime(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(15))
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Pre-signed URL lifetime must be greater than zero and no more than 15 minutes.");
    }

    private static void ValidateStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        if (storageKey.Length > 1024)
            throw new ArgumentException("Storage key must not exceed 1024 characters.", nameof(storageKey));

        if (storageKey[0] == '/' || storageKey.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Storage key contains an invalid path segment.", nameof(storageKey));

        if (storageKey.Any(char.IsControl))
            throw new ArgumentException("Storage key contains control characters.", nameof(storageKey));
    }
}
