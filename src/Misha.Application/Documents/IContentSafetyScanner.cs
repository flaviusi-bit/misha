namespace Misha.Application.Documents;

public sealed record ContentSafetyResult(bool Allowed, string? Reason = null);

public interface IContentSafetyScanner
{
    Task<ContentSafetyResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
}
