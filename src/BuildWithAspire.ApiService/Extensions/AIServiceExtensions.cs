using BuildWithAspire.ApiService.Configuration;
using Microsoft.Extensions.AI;

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

        // Register the AI settings for dependency injection
        builder.Services.AddSingleton(aiSettings);

        // Configure the appropriate AI provider
        switch (aiSettings.Provider)
        {
            case AIConfiguration.AIProvider.Ollama:
                builder.AddOllamaAIServices(aiSettings);
                break;
            case AIConfiguration.AIProvider.AzureOpenAI:
                builder.AddAzureOpenAIServices(aiSettings);
                break;
            case AIConfiguration.AIProvider.GitHubModels:
            case AIConfiguration.AIProvider.FoundryLocal:
                builder.AddAzureAIInferenceAIServices(aiSettings);
                break;
            default:
                throw new InvalidOperationException($"Unsupported AI provider: {aiSettings.Provider}");
        }

        // Add logging for AI configuration
        builder.Services.AddSingleton<IHostedService, AIConfigurationLogger>();

        return builder;
    }

    private static void AddOllamaAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
    {
        // First add the Ollama API client
        builder.AddOllamaApiClient(aiSettings.DeploymentName)
            .AddChatClient();

        // Add Semantic Kernel's chat completion service for Ollama
        builder.Services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(serviceProvider =>
        {
            // For Ollama, we'll use a generic implementation that delegates to the IChatClient
#pragma warning disable SKEXP0010
            // Try to get connection string for the deployment (added when external endpoint configured)
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(aiSettings.DeploymentName) ?? configuration.GetConnectionString("ollama") ?? string.Empty;
            string? endpointUrl = null;
            if (!string.IsNullOrEmpty(connectionString))
            {
                // Expect pattern: Endpoint=http://host:port;Model=xyz
                var endpointMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Endpoint=([^;]+)");
                if (endpointMatch.Success)
                {
                    endpointUrl = endpointMatch.Groups[1].Value;
                }
            }
            // Default Ollama serve endpoint per upstream is 127.0.0.1:11434
            var endpoint = new Uri(endpointUrl ?? "http://127.0.0.1:11434");
            return new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIChatCompletionService(
                modelId: aiSettings.Model,
                apiKey: "ollama-key", // Not used by Ollama
                httpClient: serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(),
                endpoint: endpoint);
#pragma warning restore SKEXP0010
        });
    }

    private static void AddAzureOpenAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
    {
        var connectionString = builder.Configuration.GetConnectionString("ai-service");
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("AIServiceExtensions");
        logger.LogDebug("Azure OpenAI connection string: {ConnectionString}", connectionString);

        builder.AddAzureOpenAIClient("ai-service")
            .AddChatClient(aiSettings.DeploymentName);

        // Add Semantic Kernel's chat completion service for Azure OpenAI
        builder.Services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(serviceProvider =>
        {
            var connectionString = builder.Configuration.GetConnectionString("ai-service");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Azure OpenAI connection string 'ai-service' not found.");
            }

#pragma warning disable SKEXP0010
            return new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIChatCompletionService(
                modelId: aiSettings.DeploymentName,
                apiKey: "azure-key", // Will be handled by the Azure SDK
                httpClient: serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient());
#pragma warning restore SKEXP0010
        });
    }

    private static void AddAzureAIInferenceAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
    {
        // Use the official Azure AI Foundry integration
        builder.AddAzureChatCompletionsClient(aiSettings.DeploymentName)
            .AddChatClient();

        // Add Semantic Kernel's chat completion service for Azure AI Inference
        builder.Services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(serviceProvider =>
        {
            var connectionString = builder.Configuration.GetConnectionString(aiSettings.DeploymentName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Azure AI Foundry connection string '{aiSettings.DeploymentName}' not found.");
            }

            // Parse the connection string to extract the API key and endpoint
            var apiKey = ExtractApiKeyFromConnectionString(connectionString);
            var endpoint = ExtractEndpointFromConnectionString(connectionString);

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("API key not found in connection string.");
            }

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AIServiceExtensions");
            logger.LogDebug("Configuring Azure AI Inference SK service. Provider={Provider}, DeploymentName={Deployment}, Model={Model}, Endpoint={Endpoint}", aiSettings.Provider, aiSettings.DeploymentName, aiSettings.Model, endpoint);

            // For FoundryLocal the OpenAIChatCompletionService needs the deployment name (acts like Azure OpenAI deployment)
            // Using the model id here results in HTTP 400 from the local Foundry server.
            var modelIdForSk = aiSettings.Provider == AIConfiguration.AIProvider.FoundryLocal
                ? aiSettings.DeploymentName
                : aiSettings.Model;

#pragma warning disable SKEXP0010
            if (!string.IsNullOrEmpty(endpoint))
            {
                return new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIChatCompletionService(
                    modelId: modelIdForSk,
                    endpoint: new Uri(endpoint),
                    apiKey: apiKey,
                    httpClient: serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient());
            }
            else
            {
                return new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIChatCompletionService(
                    modelId: modelIdForSk,
                    apiKey: apiKey,
                    httpClient: serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient());
            }
#pragma warning restore SKEXP0010
        });
    }

    private static string? ExtractApiKeyFromConnectionString(string connectionString)
    {
        // Parse connection string format: "Endpoint=...;Key=...;Model=...;DeploymentId=..."
        var keyMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Key=([^;]+)");
        return keyMatch.Success ? keyMatch.Groups[1].Value : null;
    }

    private static string? ExtractEndpointFromConnectionString(string connectionString)
    {
        // Parse connection string format: "Endpoint=...;Key=...;Model=...;DeploymentId=..."
        var endpointMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Endpoint=([^;]+)");
        return endpointMatch.Success ? endpointMatch.Groups[1].Value : null;
    }
}

/// <summary>
/// Background service that logs AI configuration on startup.
/// </summary>
internal sealed class AIConfigurationLogger : BackgroundService
{
    private readonly ILogger<AIConfigurationLogger> _logger;
    private readonly AIConfiguration.AISettings _aiSettings;

    public AIConfigurationLogger(ILogger<AIConfigurationLogger> logger, AIConfiguration.AISettings aiSettings)
    {
        _logger = logger;
        _aiSettings = aiSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Configuration: Provider={Provider}, Model={Model}, Deployment={Deployment}",
            _aiSettings.Provider, _aiSettings.Model, _aiSettings.DeploymentName);

        // Complete immediately - this is just for logging
        await Task.CompletedTask.ConfigureAwait(false);
    }
}

