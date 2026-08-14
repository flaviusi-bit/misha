using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Misha.Infrastructure.Persistence;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MishaDbContext))]
partial class MishaDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("Misha.Domain.Applications.Application", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<uint>("Version").IsRowVersion().HasColumnName("xmin");
            b.Property<string>("ApplicantReference").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("IdempotencyKey").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("SubmittedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("ProcessingStartedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("DecidedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("RefusalReason").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<int>("Status").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
            b.HasKey("Id");
            b.HasIndex("ApplicantReference");
            b.HasIndex("IdempotencyKey").IsUnique();
            b.HasIndex("Status");
            b.ToTable("applications");
        });

        modelBuilder.Entity("Misha.Domain.Applications.ApplicationLifecycleAudit", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<int?>("FromStatus").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<int>("ToStatus").HasConversion<string>().IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Reason").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<string>("ActorReference").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTimeOffset>("OccurredAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "OccurredAtUtc");
            b.ToTable("application_lifecycle_audits");
        });

        modelBuilder.Entity("Misha.Domain.Decisions.DecisionAudit", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<string>("PolicyVersion").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("PolicyDecision").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Decision").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("ReasonsJson").IsRequired().HasColumnType("jsonb");
            b.Property<string>("ActorReference").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.ToTable("decision_audits");
        });

        modelBuilder.Entity("Misha.Domain.Documents.DocumentArtifact", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("ContentType").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("FileName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<string>("Sha256").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            b.Property<long>("SizeBytes").HasColumnType("bigint");
            b.Property<string>("StorageKey").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<int>("DocumentType").HasConversion<string>().IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.HasIndex("Sha256");
            b.ToTable("document_artifacts");
        });

        modelBuilder.Entity("Misha.Domain.Documents.PassportDocument", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<DateOnly>("DateOfBirth").HasColumnType("date");
            b.Property<DateOnly>("ExpiryDate").HasColumnType("date");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("DocumentNumber").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("IssuingCountry").IsRequired().HasMaxLength(3).HasColumnType("character varying(3)");
            b.Property<string>("Surname").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("GivenNames").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("Nationality").IsRequired().HasMaxLength(3).HasColumnType("character varying(3)");
            b.HasKey("Id");
            b.HasIndex("ApplicationId").IsUnique();
            b.HasIndex("DocumentNumber");
            b.ToTable("passport_documents");
        });

        modelBuilder.Entity("Misha.Domain.Watchlists.WatchlistCheck", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<string>("Provider").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<int>("Decision").HasConversion<string>().IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("MatchReference").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("ErrorMessage").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("CheckedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.ToTable("watchlist_checks");
        });

        modelBuilder.Entity("Misha.Domain.Payments.Payment", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<long>("AmountMinor").HasColumnType("bigint");
            b.Property<string>("Currency").IsRequired().HasMaxLength(3).HasColumnType("character varying(3)");
            b.Property<int>("Status").HasConversion<string>().IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Provider").HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("ProviderReference").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("ActionUrl").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<string>("FailureReason").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("CompletedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.HasIndex("ProviderReference");
            b.ToTable("payments");
        });

        modelBuilder.Entity("Misha.Domain.Etas.Eta", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<string>("EtaNumber").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("VerificationTokenHash").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            b.Property<int>("Status").HasConversion<string>().IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<DateTimeOffset>("IssuedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset>("ExpiresAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("RevokedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("RevocationReason").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.HasKey("Id");
            b.HasIndex("ApplicationId").IsUnique();
            b.HasIndex("EtaNumber").IsUnique();
            b.HasIndex("VerificationTokenHash").IsUnique();
            b.ToTable("etas");
        });

        modelBuilder.Entity("Misha.Domain.Etas.EtaAudit", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid?>("EtaId").HasColumnType("uuid");
            b.Property<Guid?>("ApplicationId").HasColumnType("uuid");
            b.Property<string>("EtaNumber").HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<int>("EventType").HasConversion<string>().IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Outcome").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("ActorReference").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTimeOffset>("OccurredAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("EtaId", "OccurredAtUtc");
            b.HasIndex("ApplicationId", "OccurredAtUtc");
            b.HasIndex("EventType", "OccurredAtUtc");
            b.ToTable("eta_audits");
        });

        modelBuilder.Entity("Misha.Domain.ManualReviews.ManualReviewCase", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<int>("Status").HasConversion<string>().IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Trigger").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("Reason").IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("AssignedToActorReference").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTimeOffset?>("AssignedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<int?>("Resolution").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("ResolutionReason").HasMaxLength(2000).HasColumnType("character varying(2000)");
            b.Property<string>("ResolvedByActorReference").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTimeOffset?>("ResolvedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.HasIndex("Status", "CreatedAtUtc");
            b.ToTable("manual_review_cases");
        });

        modelBuilder.Entity("Misha.Domain.Notifications.Notification", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<string>("RecipientReference").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("Channel").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Template").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("Payload").IsRequired().HasColumnType("text");
            b.Property<int>("Status").HasConversion<string>().IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<int>("Attempts").HasColumnType("integer");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("SentAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("LastAttemptAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("LastError").HasMaxLength(2000).HasColumnType("character varying(2000)");
            b.HasKey("Id");
            b.HasIndex("Status", "CreatedAtUtc");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.ToTable("notifications");
        });

#pragma warning restore 612, 618
    }
}
