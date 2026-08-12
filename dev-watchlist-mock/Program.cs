using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", provider = "misha-dev-watchlist-mock" }));

app.MapPost("/screen", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var expectedApiKey = builder.Configuration["MOCK_API_KEY"];
    if (string.IsNullOrWhiteSpace(expectedApiKey) ||
        !request.Headers.TryGetValue("X-API-Key", out var supplied) ||
        !string.Equals(supplied.ToString(), expectedApiKey, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
    var root = document.RootElement;
    var reference = root.TryGetProperty("applicantReference", out var referenceElement)
        ? referenceElement.GetString()?.Trim() ?? string.Empty
        : string.Empty;

    var decision = reference.Contains("confirmed", StringComparison.OrdinalIgnoreCase)
        ? "ConfirmedMatch"
        : reference.Contains("potential", StringComparison.OrdinalIgnoreCase)
            ? "PotentialMatch"
            : "Clear";

    return Results.Ok(new
    {
        decision,
        match = decision == "Clear" ? null : new { reference, reason = "Deterministic dev mock response" },
        error = (object?)null
    });
});

app.Run();
