using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Misha.Application.Tenants;

public sealed class TenantContext(IHttpContextAccessor httpContextAccessor,IConfiguration configuration) : ITenantContext
{
    public bool IsAdmin => User?.FindAll("cognito:groups").Any(x=>string.Equals(x.Value,"misha-admin",StringComparison.Ordinal))==true;
    public Guid? TenantId
    {
        get
        {
            if(IsAdmin) return null;
            var clientId=User?.FindFirst("client_id")?.Value;
            var value=string.IsNullOrWhiteSpace(clientId)?null:configuration[$"Tenants:Clients:{clientId.Trim()}"];
            return Guid.TryParse(value,out var tenantId)&&tenantId!=Guid.Empty?tenantId:null;
        }
    }
    private ClaimsPrincipal? User=>httpContextAccessor.HttpContext?.User;
}
