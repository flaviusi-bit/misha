using Misha.Application.Payments;
using Misha.Infrastructure.Payments;
using Misha.Infrastructure.Persistence;

namespace Misha.Api;

public static class PaymentServiceRegistration
{
    public static void AddPaymentServices(IServiceCollection services)
    {
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IPaymentProvider, UnavailablePaymentProvider>();
        services.AddScoped<PaymentService>();
    }

    public static void MapPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/applications/{id:guid}/payment", async (
            Guid id,
            CreatePaymentRequest request,
            PaymentService service,
            CancellationToken ct) =>
        {
            try
            {
                var payment = await service.CreateAsync(id, request.AmountMinor, request.Currency, ct);
                return Results.Ok(ToResponse(payment));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        app.MapGet("/applications/{id:guid}/payment", async (
            Guid id,
            PaymentService service,
            CancellationToken ct) =>
        {
            var payment = await service.GetLatestAsync(id, ct);
            return payment is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(payment));
        }).RequireAuthorization();
    }

    private static PaymentResponse ToResponse(Misha.Domain.Payments.Payment payment) => new(
        payment.Id,
        payment.ApplicationId,
        payment.AmountMinor,
        payment.Currency,
        payment.Status.ToString(),
        payment.Provider,
        payment.ProviderReference,
        payment.FailureReason,
        payment.CreatedAtUtc,
        payment.CompletedAtUtc);
}

public sealed record CreatePaymentRequest(long AmountMinor, string Currency);

public sealed record PaymentResponse(
    Guid Id,
    Guid ApplicationId,
    long AmountMinor,
    string Currency,
    string Status,
    string? Provider,
    string? ProviderReference,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
