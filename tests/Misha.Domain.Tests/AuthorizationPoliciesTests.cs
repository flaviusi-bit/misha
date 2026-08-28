using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Misha.Api;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class AuthorizationPoliciesTests
{
    private const string ApiIdentifier = "https://misha-api";

    [Fact]
    public async Task ApiWrite_Allows_Admin_And_Operator()
    {
        var authorization = BuildAuthorization();

        var admin = await authorization.AuthorizeAsync(User("misha-admin", "write"), null, AuthorizationPolicies.ApiWrite);
        var operatorUser = await authorization.AuthorizeAsync(User("misha-operator", "write"), null, AuthorizationPolicies.ApiWrite);

        Assert.True(admin.Succeeded);
        Assert.True(operatorUser.Succeeded);
    }

    [Fact]
    public async Task ApiWrite_Denies_Reviewer_And_Auditor()
    {
        var authorization = BuildAuthorization();

        var reviewer = await authorization.AuthorizeAsync(User("misha-reviewer", "write"), null, AuthorizationPolicies.ApiWrite);
        var auditor = await authorization.AuthorizeAsync(User("misha-auditor", "write"), null, AuthorizationPolicies.ApiWrite);

        Assert.False(reviewer.Succeeded);
        Assert.False(auditor.Succeeded);
    }

    [Fact]
    public async Task ApiRead_Allows_All_Operational_Roles()
    {
        var authorization = BuildAuthorization();

        foreach (var group in new[] { "misha-admin", "misha-operator", "misha-reviewer", "misha-auditor" })
        {
            var result = await authorization.AuthorizeAsync(User(group, "read"), null, AuthorizationPolicies.ApiRead);
            Assert.True(result.Succeeded, $"Expected {group} to read API resources.");
        }
    }

    [Fact]
    public async Task DecisionWrite_Allows_Admin_And_Operator()
    {
        var authorization = BuildAuthorization();

        var admin = await authorization.AuthorizeAsync(User("misha-admin", "decision.write"), null, AuthorizationPolicies.DecisionWrite);
        var operatorUser = await authorization.AuthorizeAsync(User("misha-operator", "decision.write"), null, AuthorizationPolicies.DecisionWrite);

        Assert.True(admin.Succeeded);
        Assert.True(operatorUser.Succeeded);
    }

    [Fact]
    public async Task DecisionWrite_Denies_Reviewer_And_Auditor()
    {
        var authorization = BuildAuthorization();

        var reviewer = await authorization.AuthorizeAsync(User("misha-reviewer", "decision.write"), null, AuthorizationPolicies.DecisionWrite);
        var auditor = await authorization.AuthorizeAsync(User("misha-auditor", "decision.write"), null, AuthorizationPolicies.DecisionWrite);

        Assert.False(reviewer.Succeeded);
        Assert.False(auditor.Succeeded);
    }

    [Fact]
    public async Task ReviewWrite_Allows_Reviewer_But_Denies_Auditor()
    {
        var authorization = BuildAuthorization();

        var reviewer = await authorization.AuthorizeAsync(User("misha-reviewer", "review.write"), null, AuthorizationPolicies.ReviewWrite);
        var auditor = await authorization.AuthorizeAsync(User("misha-auditor", "review.write"), null, AuthorizationPolicies.ReviewWrite);

        Assert.True(reviewer.Succeeded);
        Assert.False(auditor.Succeeded);
    }

    [Fact]
    public async Task DecisionRead_Allows_All_Operational_Roles()
    {
        var authorization = BuildAuthorization();

        foreach (var group in new[] { "misha-admin", "misha-operator", "misha-reviewer", "misha-auditor" })
        {
            var result = await authorization.AuthorizeAsync(User(group, "decision.read"), null, AuthorizationPolicies.DecisionRead);
            Assert.True(result.Succeeded, $"Expected {group} to read decisions.");
        }
    }

    [Fact]
    public async Task ReviewRead_Denies_Operator()
    {
        var authorization = BuildAuthorization();

        var result = await authorization.AuthorizeAsync(User("misha-operator", "review.read"), null, AuthorizationPolicies.ReviewRead);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Policy_Denies_MissingScope()
    {
        var authorization = BuildAuthorization();

        var result = await authorization.AuthorizeAsync(User("misha-admin"), null, AuthorizationPolicies.DecisionWrite);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Policy_Denies_UnknownGroup_EvenWithScope()
    {
        var authorization = BuildAuthorization();

        var result = await authorization.AuthorizeAsync(User("misha-unknown", "write"), null, AuthorizationPolicies.ApiWrite);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Policy_Denies_Unauthenticated_User()
    {
        var authorization = BuildAuthorization();

        var result = await authorization.AuthorizeAsync(UnauthenticatedUser(), null, AuthorizationPolicies.ApiRead);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Policy_Denies_ValidGroup_WithScopeForDifferentApi()
    {
        var authorization = BuildAuthorization();

        var result = await authorization.AuthorizeAsync(UserWithScope("misha-admin", "https://other-api", "write"), null, AuthorizationPolicies.ApiWrite);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Policy_Denies_ValidGroup_WithAlmostMatchingScope()
    {
        var authorization = BuildAuthorization();

        var result = await authorization.AuthorizeAsync(UserWithScope("misha-admin", ApiIdentifier, "write-extra"), null, AuthorizationPolicies.ApiWrite);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Policy_Denies_ValidScope_WithoutAuthorizedGroup()
    {
        var authorization = BuildAuthorization();

        var result = await authorization.AuthorizeAsync(User("misha-unknown", "write"), null, AuthorizationPolicies.ApiWrite);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Policy_Denies_ReviewWrite_For_Operator_EvenWithScope()
    {
        var authorization = BuildAuthorization();

        var result = await authorization.AuthorizeAsync(User("misha-operator", "review.write"), null, AuthorizationPolicies.ReviewWrite);

        Assert.False(result.Succeeded);
    }

    private static IAuthorizationService BuildAuthorization()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AuthorizationPolicies.Add(services, ApiIdentifier);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal User(string group, params string[] scopes) =>
        UserWithScope(group, ApiIdentifier, scopes);

    private static ClaimsPrincipal UserWithScope(string group, string apiIdentifier, params string[] scopes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("cognito:groups", group),
            new("scope", string.Join(' ', scopes.Select(scope => $"{apiIdentifier}/{scope}")))
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static ClaimsPrincipal UnauthenticatedUser() =>
        new(new ClaimsIdentity());
}
