
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
                builder.AddAzureOpenAIClient(aiSettings.DeploymentName)
                    .AddChatClient();
                break;

            // GitHub Models and Azure AI Foundry (local emulator + cloud) expose the
            // Azure AI Inference / OpenAI-compatible chat completions protocol, so they
            // use AddAzureChatCompletionsClient rather than the Azure OpenAI client
            // (which targets the /openai/deployments/... URL scheme and 404s here).
            // Matches the official Aspire FoundryEndToEnd / GitHubModelsEndToEnd playgrounds.
            case AIConfiguration.AIProvider.GitHubModels:
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


