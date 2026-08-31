using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Misha.Infrastructure.Persistence;

public sealed class MishaDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MishaDbContext>
{
    public MishaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MishaDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=misha_design_time;Username=misha;Password=misha")
            .Options;

        return new MishaDbContext(options);
    }
}
