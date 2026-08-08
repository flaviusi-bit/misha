using Microsoft.EntityFrameworkCore;
using MishaApplication = Misha.Domain.Applications.Application;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Infrastructure.Persistence;

public sealed class MishaDbContext(DbContextOptions<MishaDbContext> options) : DbContext(options)
{
    public DbSet<MishaApplication> Applications => Set<MishaApplication>();
    public DbSet<DocumentArtifact> DocumentArtifacts => Set<DocumentArtifact>();
    public DbSet<PassportDocument> PassportDocuments => Set<PassportDocument>();
    public DbSet<WatchlistCheck> WatchlistChecks => Set<WatchlistCheck>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var application = modelBuilder.Entity<MishaApplication>();
        application.ToTable("applications");
        application.HasKey(x => x.Id);
        application.Property(x => x.ApplicantReference).HasMaxLength(200).IsRequired();
        application.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        application.Property(x => x.CreatedAtUtc).IsRequired();
        application.Property(x => x.RefusalReason).HasMaxLength(1000);
        application.HasIndex(x => x.ApplicantReference);
        application.HasIndex(x => x.Status);

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
    }
}
