using System.Security.Claims;
using Misha.Application.Tenants;

namespace Misha.Api;

public sealed class TenantContext(
    IHttpContextAccessor accessor,
    ITenantResolver resolver) : ITenantContext
{
    private bool? _isAdmin;
    private Guid? _tenantId;
    private bool _tenantResolved;

    public bool IsAdmin
    {
        get
        {
            if (_isAdmin.HasValue)
                return _isAdmin.Value;

            _isAdmin = User?.FindAll("cognito:groups")
                .Any(x => string.Equals(x.Value, "misha-admin", StringComparison.Ordinal)) == true;

            return _isAdmin.Value;
        }
    }

    public Guid? TenantId
    {
        get
        {
            if (IsAdmin)
                return null;

            if (_tenantResolved)
                return _tenantId;

            _tenantResolved = true;
            var clientId = User?.FindFirst("client_id")?.Value;
            _tenantId = resolver.ResolveAsync(clientId, CancellationToken.None).GetAwaiter().GetResult();
            return _tenantId;
        }
    }

    private ClaimsPrincipal? User => accessor.HttpContext?.User;
}
