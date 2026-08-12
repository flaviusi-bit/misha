using System.Threading.RateLimiting;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Misha.Api;
using Misha.Application.Applications;
using Misha.Application.Documents;
using Misha.Application.Etas;
using Misha.Application.Policy;
using Misha.Application.Watchlists;
using Misha.Domain.Documents;
using Misha.Infrastructure.Documents;
using Misha.Infrastructure.Persistence;
using Misha.Infrastructure.Storage;
using Misha.Infrastructure.Watchlists;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Misha");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var dbHost = builder.Configuration["DB_HOST"];
    var dbPort = builder.Configuration["DB_PORT"] ?? "5432";
    var dbName = builder.Configuration["DB_NAME"];
    var dbUser = builder.Configuration["DB_USER"];
    var dbPassword = builder.Configuration["DB_PASSWORD"];

    if (string.IsNullOrWhiteSpace(dbHost) ||
        string.IsNullOrWhiteSpace(dbName) ||
        string.IsNullOrWhiteSpace(dbUser) ||
        string.IsNullOrWhiteSpace(dbPassword))
    {
        throw new InvalidOperationException("Database configuration is missing. Expected ConnectionStrings:Misha or ECS DB_HOST, DB_PORT, DB_NAME, DB_USER and DB_PASSWORD settings.");
    }

    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
}

builder.Services.AddDbContext<MishaDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
builder.Services.AddScoped<IDocumentStorage>(sp =>
    new S3DocumentStorage(
        sp.GetRequiredService<IAmazonS3>(),
        builder.Configuration["DocumentStorage:BucketName"] ?? string.Empty));
builder.Services.AddScoped<IApplicationRepository, EfApplicationRepository>();
builder.Services.AddScoped<IDocumentArtifactRepository, EfDocumentArtifactRepository>();
builder.Services.AddScoped<IPassportRepository, EfPassportRepository>();
builder.Services.AddScoped<IWatchlistCheckRepository, EfWatchlistCheckRepository>();

builder.Services.AddHttpClient("watchlist", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<IWatchlistProvider>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Watchlist:BaseUrl"]?.Trim();

    return string.IsNullOrWhiteSpace(baseUrl)
        ? new UnavailableWatchlistProvider()
        : new HttpWatchlistProvider(
            sp.GetRequiredService<IHttpClientFactory>(),
            configuration);
});

builder.Services.AddScoped<IPassportVerificationProvider, HttpPassportVerificationProvider>();
builder.Services.AddHttpClient("passport-verification", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<PassportService>();
builder.Services.AddScoped<PassportVerificationService>();
builder.Services.AddScoped<WatchlistService>();
builder.Services.AddSingleton<IPolicyEngine, DefaultPolicyEngine>();
builder.Services.AddScoped<PolicyService>();
Misha.Api.DecisionServiceRegistration.AddDecisionEngine(builder.Services);
PaymentServiceRegistration.AddPaymentServices(builder.Services);
EtaServiceRegistration.AddEtaServices(builder.Services, builder.Configuration);
NotificationServiceRegistration.AddNotificationServices(builder.Services);
builder.Services.AddHealthChecks().AddDbContextCheck<MishaDbContext>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Misha.Security.RateLimiting");
        logger.LogWarning("eTA verification request rejected by rate limiter for {Path}", context.HttpContext.Request.Path);
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await ValueTask.CompletedTask;
    };

    options.AddPolicy("eta-verification", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var cognitoAuthority = builder.Configuration["Authentication:Authority"];
var cognitoAudience = builder.Configuration["Authentication:Audience"];
var cognitoApiIdentifier = builder.Configuration["Authentication:ApiIdentifier"] ?? "https://misha-api";

if (string.IsNullOrWhiteSpace(cognitoAuthority))
{
    throw new InvalidOperationException("Authentication:Authority is required.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = cognitoAuthority;
        options.RequireHttpsMetadata = true;

        // Cognito access tokens are the API credential. They carry `scope` and
        // `client_id`, while the ID token carries the OIDC `aud` claim. Do not
        // accept an ID token as an API bearer token by accident.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "username",
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenUse = context.Principal?.FindFirst("token_use")?.Value;
                if (!string.Equals(tokenUse, "access", StringComparison.Ordinal))
                {
                    context.Fail("Only Cognito access tokens may be used to call the API.");
                }

                return Task.CompletedTask;
            }
        };
    });

AuthorizationPolicies.Add(builder.Services, cognitoApiIdentifier);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MishaDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSecurityHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapPaymentEndpoints();
app.MapEtaEndpoints();
DecisionEndpoints.Map(app);
ManualReviewEndpoints.Map(app);
NotificationEndpoints.Map(app);

app.MapPost("/applications", async (CreateApplicationRequest request, ApplicationService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(request.ApplicantReference, ct);
    return Results.Created($"/applications/{id}", new { id });
}).RequireAuthorization(AuthorizationPolicies.ApiWrite);

app.MapGet("/applications/{id:guid}", async (Guid id, ApplicationService service, CancellationToken ct) =>
{
    var application = await service.GetAsync(id, ct);

    return application is null
        ? Results.NotFound()
        : Results.Ok(new ApplicationResponse(
            application.Id,
            application.ApplicantReference,
            application.Status.ToString(),
            application.CreatedAtUtc,
            application.SubmittedAtUtc,
            application.ProcessingStartedAtUtc,
            application.DecidedAtUtc,
            application.CancelledAtUtc,
            application.RefusalReason));
}).RequireAuthorization(AuthorizationPolicies.ApiRead);

app.MapPost("/applications/{id:guid}/submit", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.SubmitAsync(id, ct))).RequireAuthorization(AuthorizationPolicies.ApiWrite);

app.MapPost("/applications/{id:guid}/process", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.StartProcessingAsync(id, ct))).RequireAuthorization(AuthorizationPolicies.DecisionWrite);

app.MapPost("/applications/{id:guid}/approve", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.ApproveAsync(id, ct))).RequireAuthorization(AuthorizationPolicies.DecisionWrite);

app.MapPost("/applications/{id:guid}/refuse", async (
    Guid id,
    RefuseApplicationRequest request,
    ApplicationService service,
    CancellationToken ct) =>
    await ExecuteCommand(() => service.RefuseAsync(id, request.Reason, ct))).RequireAuthorization(AuthorizationPolicies.DecisionWrite);

app.MapPost("/applications/{id:guid}/cancel", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.CancelAsync(id, ct))).RequireAuthorization(AuthorizationPolicies.ApiWrite);

app.MapGet("/applications/{id:guid}/documents", async (Guid id, DocumentService service, CancellationToken ct) =>
{
    var documents = await service.GetAsync(id, ct);
    return Results.Ok(documents.Select(ToDocumentResponse));
}).RequireAuthorization(AuthorizationPolicies.ApiRead);

app.MapPost("/applications/{id:guid}/documents", async (
    Guid id,
    DocumentRequest request,
    DocumentService service,
    CancellationToken ct) =>
{
    try
    {
        var document = await service.RegisterAsync(id, request.DocumentType, request.FileName, request.ContentType, request.SizeBytes, request.Sha256, request.StorageKey, ct);
        return Results.Created($"/applications/{id}/documents", ToDocumentResponse(document));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization(AuthorizationPolicies.ApiWrite);

app.MapGet("/applications/{id:guid}/documents/{documentId:guid}", async (
    Guid id,
    Guid documentId,
    DocumentService service,
    CancellationToken ct) =>
{
    var document = await service.GetAsync(id, documentId, ct);
    return document is null ? Results.NotFound() : Results.Ok(ToDocumentResponse(document));
}).RequireAuthorization(AuthorizationPolicies.ApiRead);

app.MapPost("/applications/{id:guid}/documents/{documentId:guid}/verify", async (
    Guid id,
    Guid documentId,
    PassportService service,
    CancellationToken ct) =>
{
    try
    {
        var passport = await service.VerifyAsync(id, documentId, ct);
        return Results.Ok(ToPassportResponse(passport));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization(AuthorizationPolicies.ApiWrite);

app.MapGet("/applications/{id:guid}/passport", async (
    Guid id,
    PassportService service,
    CancellationToken ct) =>
{
    var passport = await service.GetAsync(id, ct);
    return passport is null ? Results.NotFound() : Results.Ok(ToPassportResponse(passport));
}).RequireAuthorization(AuthorizationPolicies.ApiRead);

app.MapPost("/applications/{id:guid}/watchlist/check", async (
    Guid id,
    WatchlistService service,
    CancellationToken ct) =>
{
    try
    {
        var check = await service.ScreenAsync(id, ct);
        return Results.Ok(ToWatchlistResponse(check));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
}).RequireAuthorization(AuthorizationPolicies.DecisionWrite);

app.MapGet("/applications/{id:guid}/watchlist/latest", async (
    Guid id,
    WatchlistService service,
    CancellationToken ct) =>
{
    var check = await service.GetLatestAsync(id, ct);
    return check is null ? Results.NotFound() : Results.Ok(ToWatchlistResponse(check));
}).RequireAuthorization(AuthorizationPolicies.ApiRead);

app.Run();

static async Task<IResult> ExecuteCommand(Func<Task> command)
{
    try
    {
        await command();
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
}

static object ToDocumentResponse(DocumentArtifact document) => new
{
    document.Id,
    document.ApplicationId,
    document.DocumentType,
    document.FileName,
    document.ContentType,
    document.SizeBytes,
    document.Sha256,
    document.StorageKey,
    document.Status,
    document.CreatedAtUtc,
    document.VerifiedAtUtc
};

static object ToPassportResponse(PassportDocument passport) => new
{
    passport.Id,
    passport.ApplicationId,
    passport.DocumentId,
    passport.DocumentNumber,
    passport.IssuingCountry,
    passport.Surname,
    passport.GivenNames,
    passport.DateOfBirth,
    passport.Nationality,
    passport.ExpiryDate,
    passport.VerificationStatus,
    passport.VerificationReference,
    passport.VerificationError,
    passport.CreatedAtUtc,
    passport.VerifiedAtUtc
};

static object ToWatchlistResponse(Misha.Domain.Watchlists.WatchlistCheck check) => new
{
    check.Id,
    check.ApplicationId,
    check.ProviderName,
    check.Status,
    check.Decision,
    check.MatchReference,
    check.ErrorMessage,
    check.CreatedAtUtc,
    check.CompletedAtUtc
};

public partial class Program;
