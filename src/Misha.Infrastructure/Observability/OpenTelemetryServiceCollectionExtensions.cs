using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Misha.Infrastructure.Observability;

public static class OpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddMishaOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        bool includeAspNetCoreInstrumentation)
    {
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        var openTelemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: serviceName));

        openTelemetry.WithTracing(tracing =>
        {
            if (includeAspNetCoreInstrumentation)
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/health");
                });
            }

            tracing.AddHttpClientInstrumentation();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter();
            }
        });

        openTelemetry.WithMetrics(metrics =>
        {
            if (includeAspNetCoreInstrumentation)
            {
                metrics.AddAspNetCoreInstrumentation();
            }

            metrics.AddRuntimeInstrumentation();
            metrics.AddMeter("Misha.Watchlist");

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                metrics.AddOtlpExporter();
            }
        });

        return services;
    }
}
