using System.Security.Claims;
using Misha.Application.Tenants;

namespace Misha.Api;

public static class AuthorizationPolicies
{
    public const string ApiRead = "api.read";
    public const string ApiWrite = "api.write";
    public const string DecisionRead = "decision.read";
    public const string DecisionWrite = "decision.write";
    public const string ReviewRead = "review.read";
    public const string ReviewWrite = "review.write";

    private const string ScopeClaim = "scope";
    private const string GroupClaim = "cognito:groups";

    private static readonly string[] ReadGroups =
        ["misha-admin", "misha-operator", "misha-reviewer", "misha-auditor"];

    private static readonly string[] WriteGroups =
        ["misha-admin", "misha-operator"];

    public static void Add(IServiceCollection services, string apiIdentifier)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantResolver, ConfigurationTenantResolver>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, apiIdentifier, "read") &&
                        IsInAnyGroup(context.User, ReadGroups)));

            options.AddPolicy(ApiWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, apiIdentifier, "write") &&
                        IsInAnyGroup(context.User, WriteGroups)));

            options.AddPolicy(DecisionRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, apiIdentifier, "decision.read") &&
                        IsInAnyGroup(context.User, ReadGroups)));

            options.AddPolicy(DecisionWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, apiIdentifier, "decision.write") &&
                        IsInAnyGroup(context.User, WriteGroups)));

            options.AddPolicy(ReviewRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, apiIdentifier, "review.read") &&
                        IsInAnyGroup(context.User, "misha-admin", "misha-reviewer", "misha-auditor")));

            options.AddPolicy(ReviewWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, apiIdentifier, "review.write") &&
                        IsInAnyGroup(context.User, "misha-admin", "misha-reviewer")));
        });
    }

    private static bool HasScope(ClaimsPrincipal user, string apiIdentifier, string scope)
    {
        var expected = $"{apiIdentifier.TrimEnd('/')}/{scope}";
        return user.FindAll(ScopeClaim)
            .SelectMany(claim => claim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(value => string.Equals(value, expected, StringComparison.Ordinal));
    }

    private static bool IsInAnyGroup(ClaimsPrincipal user, params string[] groups) =>
        groups.Any(group => user.FindAll(GroupClaim)
            .Any(claim => string.Equals(claim.Value, group, StringComparison.Ordinal)));
}
