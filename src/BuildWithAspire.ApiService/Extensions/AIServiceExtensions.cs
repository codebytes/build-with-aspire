
using BuildWithAspire.Abstractions;

namespace BuildWithAspire.ApiService.Extensions;

public static class AIServiceExtensions
{
    /// <summary>
    /// Adds AI services to the host application builder based on the configured provider.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddAIServices(this IHostApplicationBuilder builder)
    {
        var aiSettings = AIConfiguration.GetSettings(builder.Configuration);

        // Register AI settings once
        builder.Services.AddSingleton(aiSettings);

        // Register provider specific services (pure registration – no provider builds/logging here)
        switch (aiSettings.Provider)
        {
            case AIConfiguration.AIProvider.Ollama:
                builder.AddOllamaAIServices(aiSettings);
                break;
            case AIConfiguration.AIProvider.AzureOpenAI:
                builder.AddAzureOpenAIServices(aiSettings);
                break;
            case AIConfiguration.AIProvider.GitHubModels:
                builder.AddGitHubModelsAIServices(aiSettings);
                break;
            case AIConfiguration.AIProvider.AzureAIFoundry:
                builder.AddFoundryLocalAIServices(aiSettings);
                break;
            default:
                throw new InvalidOperationException($"Unsupported AI provider: {aiSettings.Provider}");
        }

        // Add hosted startup logger (single provider – final container)
        builder.Services.AddHostedService<AIStartupLogger>();

        return builder;
    }

    private static void AddOllamaAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
    {
        builder.AddOllamaApiClient(aiSettings.DeploymentName)
            .AddChatClient();
    }

    private static void AddAzureOpenAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
        => builder.AddAzureOpenAIClient("ai-service")
                   .AddChatClient(aiSettings.DeploymentName);

    private static void AddGitHubModelsAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
        => builder.AddOpenAIClient(aiSettings.DeploymentName)
                   .AddChatClient();

    private static void AddFoundryLocalAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
        => builder.AddAzureChatCompletionsClient(aiSettings.DeploymentName)
                   .AddChatClient();
}

// Hosted service that logs final AI configuration once the real container is built.
internal sealed class AIStartupLogger : IHostedService
{
    private readonly ILogger<AIStartupLogger> _logger;
    private readonly AIConfiguration.AISettings _settings;
    private readonly IConfiguration _configuration;

    public AIStartupLogger(ILogger<AIStartupLogger> logger,
                           AIConfiguration.AISettings settings,
                           IConfiguration configuration)
    {
        _logger = logger;
        _settings = settings;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_settings.Provider == AIConfiguration.AIProvider.AzureOpenAI)
        {
            var hasConn = !string.IsNullOrEmpty(_configuration.GetConnectionString("ai-service"));
            _logger.LogInformation("AI configured: Provider={Provider} Deployment={Deployment} Model={Model} AzureConnPresent={HasConn}",
                _settings.Provider, _settings.DeploymentName, _settings.Model, hasConn);
        }
        else
        {
            _logger.LogInformation("AI configured: Provider={Provider} Deployment={Deployment} Model={Model}",
                _settings.Provider, _settings.DeploymentName, _settings.Model);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}


