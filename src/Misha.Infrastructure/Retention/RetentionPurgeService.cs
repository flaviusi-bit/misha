using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Misha.Application.Documents;
using Misha.Application.Retention;
using Misha.Domain.Applications;
using Misha.Infrastructure.Persistence;

namespace Misha.Infrastructure.Retention;

public sealed class RetentionPurgeService(
    MishaDbContext db,
    IDocumentStorage storage,
    IOptions<RetentionOptions> options,
    ILogger<RetentionPurgeService> logger) : IRetentionPurgeService
{
    public async Task<RetentionPurgeResult> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var policy = options.Value;
        if (!policy.Enabled)
            return new RetentionPurgeResult(0, 0, 0, policy.DryRun);

        Validate(policy);

        var now = DateTimeOffset.UtcNow;
        var documentCutoff = now.AddDays(-policy.DocumentRetentionDays);
        var applicantCutoff = now.AddDays(-policy.ApplicantRetentionDays);

        var documents = await db.DocumentArtifacts
            .Where(x => x.CreatedAtUtc < documentCutoff)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(policy.BatchSize)
            .ToListAsync(cancellationToken);

        var candidateApplicants = await db.Applicants
            .Where(x => x.CreatedAtUtc < applicantCutoff)
            .Where(x => !db.Applications.Any(a => a.ApplicantId == x.Id &&
                a.Status is not (ApplicationStatus.Approved or ApplicationStatus.Refused or ApplicationStatus.Cancelled)))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(policy.BatchSize)
            .ToListAsync(cancellationToken);

        if (policy.DryRun)
        {
            logger.LogInformation(
                "Retention dry-run found {Documents} documents and {Applicants} applicant profiles eligible for retention action.",
                documents.Count,
                candidateApplicants.Count);
            return new RetentionPurgeResult(documents.Count, 0, candidateApplicants.Count, true);
        }

        foreach (var document in documents)
        {
            await storage.DeleteAsync(document.StorageKey, cancellationToken);
            db.DocumentArtifacts.Remove(document);
        }

        var applicantIds = candidateApplicants.Select(x => x.Id).ToList();
        if (applicantIds.Count > 0)
        {
            var applicationIds = await db.Applications
                .Where(x => applicantIds.Contains(x.ApplicantId))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var passportDocuments = await db.PassportDocuments
                .Where(x => applicationIds.Contains(x.ApplicationId))
                .ToListAsync(cancellationToken);

            foreach (var passport in passportDocuments)
                passport.Anonymize();

            foreach (var applicant in candidateApplicants)
                applicant.Anonymize();
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Retention purge completed: {Documents} documents deleted and {Applicants} applicant profiles anonymized.",
            documents.Count,
            candidateApplicants.Count);

        return new RetentionPurgeResult(documents.Count, candidateApplicants.Count, candidateApplicants.Count, false);
    }

    private static void Validate(RetentionOptions options)
    {
        if (options.DocumentRetentionDays <= 0)
            throw new InvalidOperationException("Retention:DocumentRetentionDays must be greater than zero.");
        if (options.ApplicantRetentionDays <= 0)
            throw new InvalidOperationException("Retention:ApplicantRetentionDays must be greater than zero.");
        if (options.BatchSize <= 0)
            throw new InvalidOperationException("Retention:BatchSize must be greater than zero.");
    }
}
