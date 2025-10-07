using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // ---- Resilience Configuration (per official docs) ----
            // Config keys (override defaults):
            //   Http:AttemptTimeoutSeconds (default 60)
            //   Http:TotalRequestTimeoutSeconds (default 180)
            //   Http:MaxRetries (default 3)  -- applies to primary retry strategy
            //   Http:EnableDevExtendedPipeline (true/false) adds a named extended handler in Development
            int attemptSeconds = HttpTimeoutHelpers.ParsePositive(builder.Configuration["Http:AttemptTimeoutSeconds"], 60);
            int totalSeconds   = HttpTimeoutHelpers.ParsePositive(builder.Configuration["Http:TotalRequestTimeoutSeconds"], 180);
            int maxRetries     = HttpTimeoutHelpers.ParsePositive(builder.Configuration["Http:MaxRetries"], 3);
            bool devExtended   = bool.TryParse(builder.Configuration["Http:EnableDevExtendedPipeline"], out var de) && de;

            http.AddStandardResilienceHandler(options =>
            {
                // Attempt timeout: each try budget
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(attemptSeconds);
                // Total request timeout: overall budget including retries
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(totalSeconds);
                // Adjust retry count (default is usually 3). Ensure at least 0.
                if (maxRetries >= 0)
                {
                    options.Retry.MaxRetryAttempts = maxRetries;
                }
                // Circuit breaker sampling must be >= 2 * attempt timeout per validation rules.
                // Allow override via Http:CircuitBreakerSamplingSeconds; else pick max(2*attemptSeconds,120).
                var samplingSecondsOverride = HttpTimeoutHelpers.ParsePositive(builder.Configuration["Http:CircuitBreakerSamplingSeconds"], 0);
                var requiredSampling = Math.Max(attemptSeconds * 2, 120);
                var samplingSeconds = samplingSecondsOverride > 0 ? Math.Max(samplingSecondsOverride, requiredSampling) : requiredSampling;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(samplingSeconds);
                // Keep other circuit breaker defaults but allow MinimumThroughput override.
                var minThroughput = HttpTimeoutHelpers.ParsePositive(builder.Configuration["Http:CircuitBreakerMinimumThroughput"], 0);
                if (minThroughput > 0)
                {
                    options.CircuitBreaker.MinimumThroughput = minThroughput;
                }
                // Partitioning strategy left as default; expose config key if needed later.
            });

            // Extended dev pipeline removed (current package lacks required builder extensions in this solution).
            // If future upgrade adds builder methods (AddRetry/AddAttemptTimeout/etc.), reintroduce here.

            // Diagnostics record removed per user request (timeouts still applied).

            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}

file static class HttpTimeoutHelpers
{
    public static int ParsePositive(string? raw, int fallback)
    {
        if (int.TryParse(raw, out var v) && v > 0)
        {
            return v;
        }
        return fallback;
    }
}
