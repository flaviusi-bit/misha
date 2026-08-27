using Misha.Application.Documents;
using Misha.Domain.Documents;

namespace Misha.Api;

public static class DocumentTransferEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/applications/{id:guid}/documents/presigned-upload", async (
            Guid id,
            PresignedUploadRequest request,
            DocumentService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.CreatePreSignedUploadAsync(id, request.DocumentType, request.FileName, request.ContentType, ct);
                return Results.Ok(new PresignedUrlResponse(result.StorageKey, result.Url, DateTimeOffset.UtcNow.AddMinutes(10)));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ApiWrite);

        app.MapGet("/applications/{id:guid}/documents/{documentId:guid}/presigned-download", async (
            Guid id,
            Guid documentId,
            DocumentService service,
            CancellationToken ct) =>
        {
            try
            {
                var url = await service.CreatePreSignedDownloadAsync(id, documentId, ct);
                return Results.Ok(new PresignedUrlResponse(null, url, DateTimeOffset.UtcNow.AddMinutes(10)));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ApiRead);
    }
}

public sealed record PresignedUploadRequest(DocumentType DocumentType, string FileName, string ContentType);
public sealed record PresignedUrlResponse(string? StorageKey, Uri Url, DateTimeOffset ExpiresAtUtc);
