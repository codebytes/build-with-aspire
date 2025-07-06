using BuildWithAspire.ApiService.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.AI.Foundry.Local;
using OpenAI;

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
                builder.AddGitHubModelsAIServices(aiSettings);
                break;
            case AIConfiguration.AIProvider.FoundryLocal:
                builder.AddFoundryLocalAIServices(aiSettings);
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
        builder.AddOllamaApiClient(aiSettings.DeploymentName)
            .AddChatClient();
    }

    private static void AddAzureOpenAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
    {
        var connectionString = builder.Configuration.GetConnectionString("ai-service");
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("AIServiceExtensions");
        logger.LogDebug("Azure OpenAI connection string: {ConnectionString}", connectionString);

        builder.AddAzureOpenAIClient("ai-service")
            .AddChatClient(aiSettings.DeploymentName);
    }

    private static void AddGitHubModelsAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
    {
        var githubToken = builder.Configuration["GITHUB_TOKEN"] ?? 
                         builder.Configuration["ConnectionStrings:GitHubModels"] ??
                         throw new InvalidOperationException("GitHub token not found. Set GITHUB_TOKEN environment variable or ConnectionStrings:GitHubModels");

        builder.Services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var openAIClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(githubToken), new OpenAIClientOptions
            {
                Endpoint = new Uri("https://models.inference.ai.azure.com")
            });
            return openAIClient.GetChatClient(aiSettings.Model).AsIChatClient();
        });
    }

    private static void AddFoundryLocalAIServices(this IHostApplicationBuilder builder, AIConfiguration.AISettings aiSettings)
    {
        builder.Services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<IChatClient>>();
            try
            {
                // Initialize FoundryLocalManager with the model
                var manager = FoundryLocalManager.StartModelAsync(aliasOrModelId: aiSettings.Model).GetAwaiter().GetResult();
                var modelInfo = manager.GetModelInfoAsync(aliasOrModelId: aiSettings.Model).GetAwaiter().GetResult();
                
                logger.LogInformation("Foundry Local initialized with endpoint: {Endpoint}, Model: {Model}", 
                    manager.Endpoint, modelInfo?.ModelId);
                
                var openAIClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(manager.ApiKey), new OpenAIClientOptions
                {
                    Endpoint = manager.Endpoint
                });
                return openAIClient.GetChatClient(modelInfo?.ModelId ?? aiSettings.Model).AsIChatClient();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Foundry Local client");
                throw;
            }
        });
    }
}

/// <summary>
/// Background service that logs AI configuration on startup.
/// </summary>
internal class AIConfigurationLogger : BackgroundService
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
        await Task.CompletedTask;
    }
}