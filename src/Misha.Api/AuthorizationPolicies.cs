using System.Security.Claims;
using Misha.Application.Tenants;

namespace Misha.Api;

public static class AuthorizationPolicies
{
    public const string ApiRead="api.read",ApiWrite="api.write",DecisionRead="decision.read",DecisionWrite="decision.write",ReviewRead="review.read",ReviewWrite="review.write";
    private const string ScopeClaim="scope",GroupClaim="cognito:groups";
    private static readonly string[] ReadGroups=["misha-admin","misha-operator","misha-reviewer","misha-auditor"];
    private static readonly string[] WriteGroups=["misha-admin","misha-operator"];
    public static void Add(IServiceCollection services,string apiIdentifier)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext,TenantContext>();
        services.AddScoped<ITenantResolver,ConfigurationTenantResolver>();
        services.AddAuthorization(options=>
        {
            options.AddPolicy(ApiRead,p=>p.RequireAuthenticatedUser().RequireAssertion(c=>HasScope(c.User,apiIdentifier,"read")&&IsInAnyGroup(c.User,ReadGroups)));
            options.AddPolicy(ApiWrite,p=>p.RequireAuthenticatedUser().RequireAssertion(c=>HasScope(c.User,apiIdentifier,"write")&&IsInAnyGroup(c.User,WriteGroups)));
            options.AddPolicy(DecisionRead,p=>p.RequireAuthenticatedUser().RequireAssertion(c=>HasScope(c.User,apiIdentifier,"decision.read")&&IsInAnyGroup(c.User,ReadGroups)));
            options.AddPolicy(DecisionWrite,p=>p.RequireAuthenticatedUser().RequireAssertion(c=>HasScope(c.User,apiIdentifier,"decision.write")&&IsInAnyGroup(c.User,WriteGroups)));
            options.AddPolicy(ReviewRead,p=>p.RequireAuthenticatedUser().RequireAssertion(c=>HasScope(c.User,apiIdentifier,"review.read")&&IsInAnyGroup(c.User,"misha-admin","misha-reviewer","misha-auditor")));
            options.AddPolicy(ReviewWrite,p=>p.RequireAuthenticatedUser().RequireAssertion(c=>HasScope(c.User,apiIdentifier,"review.write")&&IsInAnyGroup(c.User,"misha-admin","misha-reviewer")));
        });
    }
    private static bool HasScope(ClaimsPrincipal user,string apiIdentifier,string scope){var expected=$"{apiIdentifier.TrimEnd('/')}/{scope}";return user.FindAll(ScopeClaim).SelectMany(c=>c.Value.Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)).Any(v=>string.Equals(v,expected,StringComparison.Ordinal));}
    private static bool IsInAnyGroup(ClaimsPrincipal user,params string[] groups)=>groups.Any(g=>user.FindAll(GroupClaim).Any(c=>string.Equals(c.Value,g,StringComparison.Ordinal)));
}
