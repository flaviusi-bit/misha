using Microsoft.EntityFrameworkCore;
using MishaApplication = Misha.Domain.Applications.Application;
using Misha.Domain.Applications;
using Misha.Domain.Decisions;
using Misha.Domain.Documents;
using Misha.Domain.Etas;
using Misha.Domain.ManualReviews;
using Misha.Domain.Notifications;
using Misha.Domain.Payments;
using Misha.Domain.Watchlists;

namespace Misha.Infrastructure.Persistence;

public sealed class MishaDbContext(DbContextOptions<MishaDbContext> options) : DbContext(options)
{
    public DbSet<MishaApplication> Applications => Set<MishaApplication>();
    public DbSet<ApplicationLifecycleAudit> ApplicationLifecycleAudits => Set<ApplicationLifecycleAudit>();
    public DbSet<DecisionAudit> DecisionAudits => Set<DecisionAudit>();
    public DbSet<DocumentArtifact> DocumentArtifacts => Set<DocumentArtifact>();
    public DbSet<PassportDocument> PassportDocuments => Set<PassportDocument>();
    public DbSet<WatchlistCheck> WatchlistChecks => Set<WatchlistCheck>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Eta> Etas => Set<Eta>();
    public DbSet<EtaAudit> EtaAudits => Set<EtaAudit>();
    public DbSet<ManualReviewCase> ManualReviewCases => Set<ManualReviewCase>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var application = modelBuilder.Entity<MishaApplication>();
        application.ToTable("applications");
        application.HasKey(x => x.Id);
        application.Property(x => x.ApplicantReference).HasMaxLength(200).IsRequired();
        application.Property(x => x.IdempotencyKey).HasMaxLength(200);
        application.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        application.Property(x => x.CreatedAtUtc).IsRequired();
        application.Property(x => x.RefusalReason).HasMaxLength(1000);
        application.Property(x => x.Version).IsRowVersion();
        application.HasIndex(x => x.ApplicantReference);
        application.HasIndex(x => x.Status);
        application.HasIndex(x => x.IdempotencyKey).IsUnique();

        var lifecycleAudit = modelBuilder.Entity<ApplicationLifecycleAudit>();
        lifecycleAudit.ToTable("application_lifecycle_audits");
        lifecycleAudit.HasKey(x => x.Id);
        lifecycleAudit.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32);
        lifecycleAudit.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        lifecycleAudit.Property(x => x.Reason).HasMaxLength(1000);
        lifecycleAudit.Property(x => x.ActorReference).HasMaxLength(200).IsRequired();
        lifecycleAudit.Property(x => x.OccurredAtUtc).IsRequired();
        lifecycleAudit.HasIndex(x => new { x.ApplicationId, x.OccurredAtUtc });

        var audit = modelBuilder.Entity<DecisionAudit>();
        audit.ToTable("decision_audits");
        audit.HasKey(x => x.Id);
        audit.Property(x => x.PolicyVersion).HasMaxLength(50).IsRequired();
        audit.Property(x => x.PolicyDecision).HasMaxLength(32).IsRequired();
        audit.Property(x => x.Decision).HasMaxLength(32).IsRequired();
        audit.Property(x => x.ReasonsJson).HasColumnType("jsonb").IsRequired();
        audit.Property(x => x.ActorReference).HasMaxLength(200).IsRequired();
        audit.Property(x => x.CreatedAtUtc).IsRequired();
        audit.HasIndex(x => new { x.ApplicationId, x.CreatedAtUtc });

        var document = modelBuilder.Entity<DocumentArtifact>();
        document.ToTable("document_artifacts");
        document.HasKey(x => x.Id);
        document.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(32).IsRequired();
        document.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        document.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        document.Property(x => x.SizeBytes).IsRequired();
        document.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        document.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        document.Property(x => x.CreatedAtUtc).IsRequired();
        document.HasIndex(x => new { x.ApplicationId, x.CreatedAtUtc });
        document.HasIndex(x => x.Sha256);

        var passport = modelBuilder.Entity<PassportDocument>();
        passport.ToTable("passport_documents");
        passport.HasKey(x => x.Id);
        passport.Property(x => x.DocumentNumber).HasMaxLength(50).IsRequired();
        passport.Property(x => x.IssuingCountry).HasMaxLength(3).IsRequired();
        passport.Property(x => x.Surname).HasMaxLength(200).IsRequired();
        passport.Property(x => x.GivenNames).HasMaxLength(200).IsRequired();
        passport.Property(x => x.Nationality).HasMaxLength(3).IsRequired();
        passport.HasIndex(x => x.ApplicationId).IsUnique();
        passport.HasIndex(x => x.DocumentNumber);

        var watchlist = modelBuilder.Entity<WatchlistCheck>();
        watchlist.ToTable("watchlist_checks");
        watchlist.HasKey(x => x.Id);
        watchlist.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        watchlist.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32).IsRequired();
        watchlist.Property(x => x.MatchReference).HasMaxLength(200);
        watchlist.Property(x => x.ErrorMessage).HasMaxLength(1000);
        watchlist.HasIndex(x => new { x.ApplicationId, x.CreatedAtUtc });

        var payment = modelBuilder.Entity<Payment>();
        payment.ToTable("payments");
        payment.HasKey(x => x.Id);
        payment.Property(x => x.AmountMinor).IsRequired();
        payment.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        payment.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        payment.Property(x => x.Provider).HasMaxLength(100);
        payment.Property(x => x.ProviderReference).HasMaxLength(200);
        payment.Property(x => x.ActionUrl).HasMaxLength(1000);
        payment.Property(x => x.FailureReason).HasMaxLength(1000);
        payment.Property(x => x.CreatedAtUtc).IsRequired();
        payment.HasIndex(x => new { x.ApplicationId, x.CreatedAtUtc });
        payment.HasIndex(x => x.ProviderReference);

        var eta = modelBuilder.Entity<Eta>();
        eta.ToTable("etas");
        eta.HasKey(x => x.Id);
        eta.Property(x => x.EtaNumber).HasMaxLength(32).IsRequired();
        eta.Property(x => x.VerificationTokenHash).HasMaxLength(64).IsRequired();
        eta.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        eta.Property(x => x.IssuedAtUtc).IsRequired();
        eta.Property(x => x.ExpiresAtUtc).IsRequired();
        eta.Property(x => x.RevocationReason).HasMaxLength(1000);
        eta.HasIndex(x => x.ApplicationId).IsUnique();
        eta.HasIndex(x => x.EtaNumber).IsUnique();
        eta.HasIndex(x => x.VerificationTokenHash).IsUnique();

        var etaAudit = modelBuilder.Entity<EtaAudit>();
        etaAudit.ToTable("eta_audits");
        etaAudit.HasKey(x => x.Id);
        etaAudit.Property(x => x.EtaNumber).HasMaxLength(32);
        etaAudit.Property(x => x.EventType).HasConversion<string>().HasMaxLength(32).IsRequired();
        etaAudit.Property(x => x.Outcome).HasMaxLength(32).IsRequired();
        etaAudit.Property(x => x.ActorReference).HasMaxLength(200).IsRequired();
        etaAudit.Property(x => x.OccurredAtUtc).IsRequired();
        etaAudit.HasIndex(x => new { x.EtaId, x.OccurredAtUtc });
        etaAudit.HasIndex(x => new { x.ApplicationId, x.OccurredAtUtc });
        etaAudit.HasIndex(x => new { x.EventType, x.OccurredAtUtc });

        var manualReview = modelBuilder.Entity<ManualReviewCase>();
        manualReview.ToTable("manual_review_cases");
        manualReview.HasKey(x => x.Id);
        manualReview.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        manualReview.Property(x => x.Trigger).HasMaxLength(100).IsRequired();
        manualReview.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        manualReview.Property(x => x.AssignedToActorReference).HasMaxLength(200);
        manualReview.Property(x => x.Resolution).HasConversion<string>().HasMaxLength(32);
        manualReview.Property(x => x.ResolutionReason).HasMaxLength(2000);
        manualReview.Property(x => x.ResolvedByActorReference).HasMaxLength(200);
        manualReview.Property(x => x.CreatedAtUtc).IsRequired();
        manualReview.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        manualReview.HasIndex(x => new { x.ApplicationId, x.CreatedAtUtc });

        var notification = modelBuilder.Entity<Notification>();
        notification.ToTable("notifications");
        notification.HasKey(x => x.Id);
        notification.Property(x => x.RecipientReference).HasMaxLength(200).IsRequired();
        notification.Property(x => x.Channel).HasMaxLength(32).IsRequired();
        notification.Property(x => x.Template).HasMaxLength(100).IsRequired();
        notification.Property(x => x.Payload).HasColumnType("text").IsRequired();
        notification.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        notification.Property(x => x.LastError).HasMaxLength(2000);
        notification.Property(x => x.CreatedAtUtc).IsRequired();
        notification.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        notification.HasIndex(x => new { x.ApplicationId, x.CreatedAtUtc });

        var outbox = modelBuilder.Entity<OutboxMessage>();
        outbox.ToTable("outbox_messages");
        outbox.HasKey(x => x.Id);
        outbox.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        outbox.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        outbox.Property(x => x.OccurredAtUtc).IsRequired();
        outbox.Property(x => x.PublishedAtUtc);
        outbox.Property(x => x.AttemptCount).IsRequired();
        outbox.Property(x => x.LastAttemptAtUtc);
        outbox.Property(x => x.LastError).HasMaxLength(2000);
        outbox.HasIndex(x => new { x.PublishedAtUtc, x.OccurredAtUtc });
        outbox.HasIndex(x => new { x.AggregateId, x.OccurredAtUtc });
    }
}
