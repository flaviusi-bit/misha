namespace Misha.Application.Retention;

public interface IRetentionPurgeService
{
    Task<RetentionPurgeResult> PurgeExpiredAsync(CancellationToken cancellationToken);
}

public sealed record RetentionPurgeResult(
    int DocumentsDeleted,
    int ApplicantsAnonymized,
    int ApplicantsEligible,
    bool DryRun);
