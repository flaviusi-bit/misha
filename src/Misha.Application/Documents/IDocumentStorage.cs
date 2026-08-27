namespace Misha.Application.Documents;

public interface IDocumentStorage
{
    Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);

    Uri CreatePreSignedUploadUrl(string storageKey, string contentType, TimeSpan lifetime);

    Uri CreatePreSignedDownloadUrl(string storageKey, TimeSpan lifetime);
}
