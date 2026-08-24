using Microsoft.EntityFrameworkCore;
using Misha.Application.Applicants;
using Misha.Domain.Applicants;

namespace Misha.Infrastructure.Persistence;

public sealed class EfApplicantRepository(MishaDbContext db) : IApplicantRepository
{
    public Task<Applicant?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Applicants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
