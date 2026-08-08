using Microsoft.EntityFrameworkCore;
using MishaApplication = Misha.Domain.Applications.Application;

namespace Misha.Infrastructure.Persistence;

public sealed class MishaDbContext(DbContextOptions<MishaDbContext> options) : DbContext(options)
{
    public DbSet<MishaApplication> Applications => Set<MishaApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var application = modelBuilder.Entity<MishaApplication>();

        application.ToTable("applications");
        application.HasKey(x => x.Id);
        application.Property(x => x.ApplicantReference).HasMaxLength(200).IsRequired();
        application.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        application.Property(x => x.CreatedAtUtc).IsRequired();
        application.HasIndex(x => x.ApplicantReference);
    }
}
