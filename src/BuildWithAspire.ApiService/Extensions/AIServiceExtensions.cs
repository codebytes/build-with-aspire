
using BuildWithAspire.Abstractions;

namespace BuildWithAspire.ApiService.Extensions;

/// <summary>
/// Extensions for configuring AI services based on provider settings.
/// </summary>
public static class AIServiceExtensions
{
    /// <summary>
    /// Configures AI services based on appsettings.json configuration.
    /// </summary>
    public static IHostApplicationBuilder AddAIServices(this IHostApplicationBuilder builder)
    {
        var aiSettings = AIConfiguration.GetSettings(builder.Configuration);
        builder.Services.AddSingleton(aiSettings);

        builder.AddAIProvider(aiSettings);
        builder.Services.AddHostedService<AIStartupLogger>();

        return builder;
    }

    private static IHostApplicationBuilder AddAIProvider(
        this IHostApplicationBuilder builder,
        AIConfiguration.AISettings aiSettings)
    {
        switch (aiSettings.Provider)
        {
            case AIConfiguration.AIProvider.Ollama:
                builder.AddOllamaApiClient(aiSettings.DeploymentName)
                    .AddChatClient();
                break;

            case AIConfiguration.AIProvider.AzureOpenAI:
                builder.AddAzureOpenAIClient("ai-service")
                    .AddChatClient(aiSettings.DeploymentName);
                break;

            case AIConfiguration.AIProvider.GitHubModels:
                builder.AddOpenAIClient(aiSettings.DeploymentName)
                    .AddChatClient();
                break;

            case AIConfiguration.AIProvider.AzureAIFoundry:
                builder.AddAzureChatCompletionsClient(aiSettings.DeploymentName)
                    .AddChatClient();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported AI provider: {aiSettings.Provider}");
        }

        return builder;
    }
}

/// <summary>
/// Logs AI configuration at startup.
/// </summary>
internal sealed class AIStartupLogger(
    ILogger<AIStartupLogger> logger,
    AIConfiguration.AISettings settings,
    IConfiguration configuration) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (settings.Provider == AIConfiguration.AIProvider.AzureOpenAI)
        {
            var hasConnection = !string.IsNullOrEmpty(configuration.GetConnectionString("ai-service"));
            logger.LogInformation(
                "AI: {Provider}, Deployment: {Deployment}, Model: {Model}, Connected: {HasConnection}",
                settings.Provider, settings.DeploymentName, settings.Model, hasConnection);
        }
        else
        {
            logger.LogInformation(
                "AI: {Provider}, Deployment: {Deployment}, Model: {Model}",
                settings.Provider, settings.DeploymentName, settings.Model);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}


