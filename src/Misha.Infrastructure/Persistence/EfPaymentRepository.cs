using Microsoft.EntityFrameworkCore;
using Misha.Application.Payments;
using Misha.Application.Tenants;
using Misha.Domain.Payments;

namespace Misha.Infrastructure.Persistence;

public sealed class EfPaymentRepository(MishaDbContext db, ITenantContext tenantContext) : IPaymentRepository
{
    public Task<Payment?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
        db.Payments
            .Where(x => x.ApplicationId == applicationId &&
                        (tenantContext.IsAdmin || db.Applications.Any(a =>
                            a.Id == x.ApplicationId && a.TenantId == tenantContext.TenantId)))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        db.Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
