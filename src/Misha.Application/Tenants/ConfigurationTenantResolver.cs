using Microsoft.Extensions.Configuration;

namespace Misha.Application.Tenants;

public sealed class ConfigurationTenantResolver(IConfiguration configuration) : ITenantResolver
{
    public Task<Guid?> ResolveAsync(string? clientId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(clientId)) return Task.FromResult<Guid?>(null);
        var value = configuration[$"Tenants:Clients:{clientId.Trim()}"];
        return Task.FromResult(Guid.TryParse(value, out var tenantId) && tenantId != Guid.Empty ? tenantId : null);
    }
}
