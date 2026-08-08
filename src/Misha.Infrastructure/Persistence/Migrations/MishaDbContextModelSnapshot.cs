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
            b.Property<string>("ApplicantReference").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("SubmittedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("ProcessingStartedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("DecidedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("RefusalReason").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<int>("Status").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
            b.HasKey("Id");
            b.HasIndex("ApplicantReference");
            b.HasIndex("Status");
            b.ToTable("applications");
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
            b.Property<int>("DocumentType").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
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
            b.Property<int>("Decision").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
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
            b.Property<int>("Status").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Provider").HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("ProviderReference").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("FailureReason").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("CompletedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.HasIndex("ProviderReference");
            b.ToTable("payments");
        });
#pragma warning restore 612, 618
    }
}
