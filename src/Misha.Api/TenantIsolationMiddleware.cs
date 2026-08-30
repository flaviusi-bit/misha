using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Misha.Infrastructure.Persistence;
namespace Misha.Api;
public sealed class TenantIsolationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context,MishaDbContext db)
    {
        if(context.User.Identity?.IsAuthenticated!=true){await next(context);return;}
        if(IsCrossTenantAdmin(context.User)){await next(context);return;}
        var tenantId=context.User.FindFirstValue("client_id");
        if(string.IsNullOrWhiteSpace(tenantId)){context.Response.StatusCode=StatusCodes.Status403Forbidden;return;}
        if(TryGetGuid(context.Request.RouteValues,"id",out var id))
        {
            var path=context.Request.Path.Value??string.Empty;
            var allowed=path.StartsWith("/applicants/",StringComparison.OrdinalIgnoreCase)
                ? await db.Applicants.AsNoTracking().AnyAsync(x=>x.Id==id&&x.TenantId==tenantId,context.RequestAborted)
                : await db.Applications.AsNoTracking().AnyAsync(x=>x.Id==id&&x.TenantId==tenantId,context.RequestAborted);
            if(!allowed){context.Response.StatusCode=StatusCodes.Status404NotFound;return;}
        }
        await next(context);
    }
    private static bool IsCrossTenantAdmin(ClaimsPrincipal user)=>user.FindAll("cognito:groups").Any(c=>string.Equals(c.Value,"misha-admin",StringComparison.Ordinal));
    private static bool TryGetGuid(RouteValueDictionary values,string key,out Guid id)=>values.TryGetValue(key,out var value)&&Guid.TryParse(value?.ToString(),out id);
}
