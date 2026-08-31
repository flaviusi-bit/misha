namespace Misha.Application.Tenants;

public interface ITenantResolver
{
    Task<Guid?> ResolveAsync(string? clientId, CancellationToken cancellationToken);
}
