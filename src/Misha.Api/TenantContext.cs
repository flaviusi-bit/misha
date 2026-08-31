using System.Security.Claims;
using Misha.Application.Tenants;

namespace Misha.Api;

public sealed class TenantContext(IHttpContextAccessor accessor,IConfiguration configuration) : ITenantContext
{
    public bool IsAdmin=>User?.FindAll("cognito:groups").Any(x=>string.Equals(x.Value,"misha-admin",StringComparison.Ordinal))==true;
    public Guid? TenantId{get{if(IsAdmin)return null;var clientId=User?.FindFirst("client_id")?.Value;var value=string.IsNullOrWhiteSpace(clientId)?null:configuration[$"Tenants:Clients:{clientId.Trim()}"];return Guid.TryParse(value,out var id)&&id!=Guid.Empty?id:null;}}
    private ClaimsPrincipal? User=>accessor.HttpContext?.User;
}
