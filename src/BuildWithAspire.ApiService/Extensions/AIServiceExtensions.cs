using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildWithAspire.ApiService.Extensions;

/// <summary>
/// Extensions for configuring AI services using official Aspire integrations
/// </summary>
public static class AIServiceExtensions
{
    /// <summary>
    /// Adds AI services to the host application builder.
    /// The AI provider and connection strings are configured through Aspire's official integrations in the AppHost.
    /// This service will use the IChatClient provided by the connection string.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddAIServices(this IHostApplicationBuilder builder)
    {
        var aiProvider = GetAIProvider(builder.Configuration);
        var deploymentName = builder.Configuration["AI:DeploymentName"] ?? "chat";

        // Add Semantic Kernel services
        // The IChatClient is already provided by the AppHost's connection string configuration
        builder.Services.AddKernel();

        // Add a simple configuration logger
        builder.Services.TryAddSingleton<IHostedService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AIConfigurationLogger>>();
            return new AIConfigurationLogger(logger, aiProvider, deploymentName);
        });

        return builder;
    }

    private static string GetAIProvider(IConfiguration configuration)
    {
        return configuration["AI:Provider"]?.ToLowerInvariant() ?? "ollama";
    }
}

/// <summary>
/// Simple configuration logger for AI settings
/// </summary>
internal sealed class AIConfigurationLogger : BackgroundService
{
    private readonly ILogger<AIConfigurationLogger> _logger;
    private readonly string _provider;
    private readonly string _deployment;

    public AIConfigurationLogger(ILogger<AIConfigurationLogger> logger, string provider, string deployment)
    {
        _logger = logger;
        _provider = provider;
        _deployment = deployment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Configuration: Provider={Provider}, Deployment={Deployment}", _provider, _deployment);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}