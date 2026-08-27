using System.Security.Claims;

namespace Misha.Api;

public sealed record AuditIdentityContext(
    string Subject,
    string? Username,
    string? ClientId)
{
    public static AuditIdentityContext From(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("Authenticated audit identity is required.");

        var subject = user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
            throw new InvalidOperationException("Authenticated audit identity is missing the subject claim.");

        return new AuditIdentityContext(
            subject.Trim(),
            user.FindFirstValue("username")?.Trim(),
            user.FindFirstValue("client_id")?.Trim());
    }

    public string ActorReference => Subject;
}
