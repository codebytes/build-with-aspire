using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods that add common .NET Aspire services to your application.
/// These provide observability (telemetry), resilience (retries), and health checks.
/// </summary>
/// <remarks>
/// Reference this project from each service in your solution to get:
/// - Service discovery (find other services automatically)
/// - Resilience patterns (automatic retries, circuit breakers, timeouts)
/// - Health checks (monitor if services are running)
/// - OpenTelemetry (logs, metrics, and traces)
/// Learn more: https://aka.ms/dotnet/aspire/service-defaults
/// </remarks>
public static class Extensions
{
    /// <summary>
    /// Adds all the standard Aspire service defaults to your application.
    /// Call this once in your Program.cs to configure observability, resilience, and health checks.
    /// </summary>
    /// <param name="builder">The host application builder from Program.cs</param>
    /// <returns>The builder for chaining additional configuration</returns>
    /// <example>
    /// In Program.cs:
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddServiceDefaults();
    /// </example>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // Configure OpenTelemetry for observability (logs, metrics, traces)
        builder.ConfigureOpenTelemetry();

        // Add health check endpoints
        builder.AddDefaultHealthChecks();

        // Enable service discovery (find services by name instead of hardcoded URLs)
        builder.Services.AddServiceDiscovery();

        // Configure HTTP clients with resilience patterns
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Get timeout and retry settings from configuration (or use defaults)
            int attemptTimeoutSeconds = ParsePositiveInt(builder.Configuration["Http:AttemptTimeoutSeconds"], defaultValue: 60);
            int totalTimeoutSeconds = ParsePositiveInt(builder.Configuration["Http:TotalRequestTimeoutSeconds"], defaultValue: 180);
            int maxRetries = ParsePositiveInt(builder.Configuration["Http:MaxRetries"], defaultValue: 3);

            // Add standard resilience handler (retries, timeouts, circuit breaker)
            http.AddStandardResilienceHandler(options =>
            {
                // Attempt timeout: How long each individual try can take
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(attemptTimeoutSeconds);

                // Total request timeout: Maximum time for all retries combined
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(totalTimeoutSeconds);

                // Maximum retry attempts on failure
                options.Retry.MaxRetryAttempts = maxRetries;

                // Circuit breaker: Opens after too many failures to prevent cascade failures
                // Sampling duration must be at least 2x the attempt timeout
                int circuitBreakerSamplingSeconds = ParsePositiveInt(
                    builder.Configuration["Http:CircuitBreakerSamplingSeconds"],
                    defaultValue: Math.Max(attemptTimeoutSeconds * 2, 120));

                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(circuitBreakerSamplingSeconds);

                // Optional: Configure minimum throughput before circuit breaker activates
                int minThroughput = ParsePositiveInt(builder.Configuration["Http:CircuitBreakerMinimumThroughput"], defaultValue: 0);
                if (minThroughput > 0)
                {
                    options.CircuitBreaker.MinimumThroughput = minThroughput;
                }
            });

            // Enable service discovery for HTTP clients
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry for comprehensive observability.
    /// This sets up logging, metrics, and distributed tracing.
    /// </summary>
    /// <remarks>
    /// OpenTelemetry provides:
    /// - Structured logs: Better than console logs for production
    /// - Metrics: Track performance indicators (requests/sec, CPU, memory)
    /// - Traces: Follow requests across multiple services
    /// </remarks>
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        // Configure logging to include more context
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;  // Include the formatted log message
            logging.IncludeScopes = true;             // Include log scopes for context
        });

        // Add OpenTelemetry with metrics and tracing
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                // Collect standard metrics from ASP.NET Core, HTTP clients, and runtime
                metrics.AddAspNetCoreInstrumentation()    // Request counts, durations, etc.
                       .AddHttpClientInstrumentation()     // Outgoing HTTP request metrics
                       .AddRuntimeInstrumentation();       // GC, memory, CPU metrics
            })
            .WithTracing(tracing =>
            {
                // Trace requests through your application
                tracing.AddAspNetCoreInstrumentation()    // Incoming requests
                       .AddHttpClientInstrumentation();    // Outgoing requests

                // Uncomment to add gRPC tracing (requires OpenTelemetry.Instrumentation.GrpcNetClient package):
                // .AddGrpcClientInstrumentation()
            });

        // Configure where to send telemetry data
        builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// Configures where OpenTelemetry data is exported.
    /// By default, exports to OTLP (OpenTelemetry Protocol) or Azure Monitor.
    /// </summary>
    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        // Check if OTLP endpoint is configured (common for production)
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            // Export to an OTLP-compatible backend (Aspire Dashboard, Jaeger, etc.)
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment to export to Azure Monitor (requires Azure.Monitor.OpenTelemetry.AspNetCore package):
        // if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        // {
        //     builder.Services.AddOpenTelemetry().UseAzureMonitor();
        // }

        return builder;
    }

    /// <summary>
    /// Adds basic health check endpoints to your application.
    /// Health checks let you monitor if your service is running correctly.
    /// </summary>
    /// <remarks>
    /// The default health check always returns healthy (just checks the app is responding).
    /// Add more specific health checks for databases, dependencies, etc.
    /// </remarks>
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // "self" check: Confirms the application is running and responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps health check endpoints for monitoring.
    /// Only enabled in Development for security - enable carefully in production!
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The application for chaining</returns>
    /// <remarks>
    /// Two endpoints:
    /// - /health: All health checks must pass (ready to accept traffic)
    /// - /alive: Only "live" checks must pass (process is running)
    ///
    /// Security warning: Exposing health checks in production can reveal system information.
    /// See: https://aka.ms/dotnet/aspire/healthchecks
    /// </remarks>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // /health - All health checks must pass (readiness)
            app.MapHealthChecks("/health");

            // /alive - Only checks tagged "live" must pass (liveness)
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = healthCheck => healthCheck.Tags.Contains("live")
            });
        }

        return app;
    }

    /// <summary>
    /// Helper method to parse configuration values as positive integers.
    /// Returns the default value if parsing fails or result is not positive.
    /// </summary>
    private static int ParsePositiveInt(string? value, int defaultValue)
    {
        if (int.TryParse(value, out var parsed) && parsed > 0)
        {
            return parsed;
        }
        return defaultValue;
    }
}
