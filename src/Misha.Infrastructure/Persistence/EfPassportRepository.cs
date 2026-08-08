using Microsoft.EntityFrameworkCore;
using Misha.Application.Documents;
using Misha.Domain.Documents;

namespace Misha.Infrastructure.Persistence;

public sealed class EfPassportRepository(MishaDbContext db) : IPassportRepository
{
    public Task<PassportDocument?> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken) =>
        db.PassportDocuments.SingleOrDefaultAsync(x => x.ApplicationId == applicationId, cancellationToken);

    public Task AddAsync(PassportDocument passport, CancellationToken cancellationToken)
    {
        db.PassportDocuments.Add(passport);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
