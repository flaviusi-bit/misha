using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Misha.Api;

#if DEBUG
[ApiController]
[Route("dev/watchlist")]
public sealed class DevWatchlistMockController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DevWatchlistMockController(IConfiguration configuration) => _configuration = configuration;

    [HttpPost("screen")]
    public IActionResult Screen([FromBody] DevWatchlistRequest request)
    {
        var expectedApiKey = _configuration["DevWatchlistMock__ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedApiKey) ||
            !Request.Headers.TryGetValue("X-API-Key", out var supplied) ||
            !string.Equals(supplied.ToString(), expectedApiKey, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var reference = request.ApplicantReference?.Trim() ?? string.Empty;
        var decision = reference.Contains("confirmed", StringComparison.OrdinalIgnoreCase)
            ? "ConfirmedMatch"
            : reference.Contains("potential", StringComparison.OrdinalIgnoreCase)
                ? "PotentialMatch"
                : "Clear";

        return Ok(new
        {
            decision,
            match = decision == "Clear" ? null : new { reference, reason = "Deterministic dev mock response" },
            error = (object?)null
        });
    }
}

public sealed record DevWatchlistRequest(string? ApplicantReference, JsonElement? Identity = null);
#endif
