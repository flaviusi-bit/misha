using System.Security.Claims;
using Misha.Application.Tenants;

namespace Misha.Api;

public sealed class TenantContext(
    IHttpContextAccessor accessor,
    ITenantResolver resolver) : ITenantContext
{
    public bool IsAdmin =>
        User?.FindAll("cognito:groups")
            .Any(x => string.Equals(x.Value, "misha-admin", StringComparison.Ordinal)) == true;

    public Guid? TenantId =>
        IsAdmin ? null : ResolveTenantId();

    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    private Guid? ResolveTenantId()
    {
        var clientId = User?.FindFirst("client_id")?.Value;
        return resolver.ResolveAsync(clientId, CancellationToken.None).GetAwaiter().GetResult();
    }
}
