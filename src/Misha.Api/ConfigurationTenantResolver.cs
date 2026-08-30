using Misha.Application.Tenants;
namespace Misha.Api;
public sealed class ConfigurationTenantResolver(IConfiguration configuration):ITenantResolver
{
    public Task<Guid?> ResolveAsync(string? clientId,CancellationToken cancellationToken){cancellationToken.ThrowIfCancellationRequested();var value=string.IsNullOrWhiteSpace(clientId)?null:configuration[$"Tenants:Clients:{clientId.Trim()}"];return Task.FromResult(Guid.TryParse(value,out var id)&&id!=Guid.Empty?id:null);}
}
