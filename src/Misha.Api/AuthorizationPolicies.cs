using System.Security.Claims;

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
    private const string ApiIdentifier = "https://misha-api";

    public static void Add(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => HasScope(context.User, "read")));

            options.AddPolicy(ApiWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => HasScope(context.User, "write")));

            options.AddPolicy(DecisionRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, "decision.read") &&
                        IsInAnyGroup(context.User, "misha-admin", "misha-operator", "misha-reviewer", "misha-auditor")));

            options.AddPolicy(DecisionWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, "decision.write") &&
                        IsInAnyGroup(context.User, "misha-admin", "misha-operator")));

            options.AddPolicy(ReviewRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, "review.read") &&
                        IsInAnyGroup(context.User, "misha-admin", "misha-reviewer", "misha-auditor")));

            options.AddPolicy(ReviewWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasScope(context.User, "review.write") &&
                        IsInAnyGroup(context.User, "misha-admin", "misha-reviewer")));
        });
    }

    private static bool HasScope(ClaimsPrincipal user, string scope)
    {
        var expected = $"{ApiIdentifier}/{scope}";
        return user.FindAll(ScopeClaim)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(value => string.Equals(value, expected, StringComparison.Ordinal));
    }

    private static bool IsInAnyGroup(ClaimsPrincipal user, params string[] groups) =>
        groups.Any(group => user.FindAll(GroupClaim).Any(claim => string.Equals(claim.Value, group, StringComparison.Ordinal)));
}
