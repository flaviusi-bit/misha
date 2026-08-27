using System.Text;
using Misha.Application.Documents;

namespace Misha.Infrastructure.Documents;

/// <summary>
/// Deterministic local safety boundary for development and tests.
/// Production deployments can replace this implementation with a managed
/// malware scanning provider without changing DocumentService.
/// </summary>
public sealed class BasicContentSafetyScanner : IContentSafetyScanner
{
    private static readonly byte[] EicarSignature = Encoding.ASCII.GetBytes("EICAR-STANDARD-ANTIVIRUS-TEST-FILE");

    public async Task<ContentSafetyResult> ScanAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        if (content.CanSeek)
            content.Position = 0;

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        if (ContainsSequence(bytes, EicarSignature))
            return new ContentSafetyResult(false, "Document content was rejected by the malware safety boundary.");

        if (!HasExpectedSignature(bytes, contentType))
            return new ContentSafetyResult(false, "Document content does not match the declared content type.");

        if (content.CanSeek)
            content.Position = 0;

        return new ContentSafetyResult(true);
    }

    private static bool HasExpectedSignature(byte[] bytes, string contentType) => contentType.ToLowerInvariant() switch
    {
        "application/pdf" => bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
        "image/jpeg" => bytes.Length >= 3 && bytes.AsSpan(0, 3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),
        "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        _ => true
    };

    private static bool ContainsSequence(byte[] source, byte[] sequence)
    {
        if (sequence.Length == 0 || source.Length < sequence.Length)
            return false;

        for (var i = 0; i <= source.Length - sequence.Length; i++)
        {
            if (source.AsSpan(i, sequence.Length).SequenceEqual(sequence))
                return true;
        }

        return false;
    }
}
