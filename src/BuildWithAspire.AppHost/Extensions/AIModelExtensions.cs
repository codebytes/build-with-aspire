using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

namespace BuildWithAspire.AppHost.Extensions;

public static class AIModelExtensions
{
    /// <summary>
    /// Adds an AI model service to the application based on the configured provider.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the AI service resource.</param>
    /// <returns>A resource builder for the AI service, or null if the provider doesn't need a connection string resource.</returns>
    public static IResourceBuilder<IResourceWithConnectionString>? AddAIModel(
        this IDistributedApplicationBuilder builder, 
        string name = "ai-service")
    {
        var configuration = builder.Configuration;
        var aiProvider = GetAIProvider(configuration);
        var deploymentName = GetDeploymentName(configuration);
        var aiModel = GetAIModel(configuration, aiProvider);

        return aiProvider switch
        {
            AIProvider.AzureOpenAI => builder.AddAzureOpenAI(name),
            AIProvider.Ollama => builder.AddOllama(name)
                .WithDataVolume()
                .WithOpenWebUI()
                .AddModel(deploymentName, aiModel),
            AIProvider.GitHubModels => null, // GitHub Models doesn't need a connection string resource
            AIProvider.FoundryLocal => null, // Foundry Local doesn't need a connection string resource
            _ => throw new InvalidOperationException($"Unsupported AI provider: {aiProvider}. Supported providers: azureopenai, githubmodels, ollama, foundrylocal")
        };
    }

    /// <summary>
    /// Adds AI model configuration to a project resource.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="aiService">The AI service resource (can be null for providers like GitHub Models).</param>
    /// <param name="deploymentName">The deployment name for the AI service.</param>
    /// <returns>The project resource builder with AI configuration.</returns>
    public static IResourceBuilder<ProjectResource> WithAIModel(
        this IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<IResourceWithConnectionString>? aiService = null,
        string deploymentName = "chat")
    {
        var configuration = builder.ApplicationBuilder.Configuration;
        var aiProvider = GetAIProvider(configuration);
        var aiModel = GetAIModel(configuration, aiProvider);

        // Add environment variables for AI configuration
        builder = builder
            .WithEnvironment("AI:Provider", aiProvider.ToString().ToLowerInvariant())
            .WithEnvironment("AI:DeploymentName", deploymentName)
            .WithEnvironment("AI:Model", aiModel);

        // Add provider-specific configuration
        switch (aiProvider)
        {
            case AIProvider.GitHubModels:
                builder = AddGitHubModelsConfiguration(builder);
                break;
            case AIProvider.AzureOpenAI:
            case AIProvider.Ollama:
                if (aiService != null)
                {
                    builder = builder
                        .WithReference(aiService, deploymentName)
                        .WaitFor(aiService);
                }
                break;
            case AIProvider.FoundryLocal:
                // Foundry Local doesn't need additional configuration
                break;
        }

        return builder;
    }

    private static IResourceBuilder<ProjectResource> AddGitHubModelsConfiguration(
        IResourceBuilder<ProjectResource> builder)
    {
        var configuration = builder.ApplicationBuilder.Configuration;
        
        // Try to get GitHub token from environment first, then parameter
        var envGithubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(envGithubToken))
        {
            builder = builder.WithEnvironment("GITHUB_TOKEN", envGithubToken);
        }
        else
        {
            var githubToken = builder.ApplicationBuilder.AddParameter("github-token", secret: true);
            builder = builder.WithEnvironment("GITHUB_TOKEN", githubToken);
        }

        return builder;
    }

    private static AIProvider GetAIProvider(IConfiguration configuration)
    {
        var providerString = configuration["AI:Provider"]?.ToLowerInvariant() ?? "ollama";
        return providerString switch
        {
            "ollama" => AIProvider.Ollama,
            "azureopenai" => AIProvider.AzureOpenAI,
            "githubmodels" => AIProvider.GitHubModels,
            "foundrylocal" => AIProvider.FoundryLocal,
            _ => throw new InvalidOperationException($"Unsupported AI provider: {providerString}. Supported providers: azureopenai, githubmodels, ollama, foundrylocal")
        };
    }

    private static string GetDeploymentName(IConfiguration configuration)
    {
        return configuration["AI:DeploymentName"] ?? "chat";
    }

    private static string GetAIModel(IConfiguration configuration, AIProvider provider)
    {
        var configuredModel = configuration["AI:Model"];
        if (!string.IsNullOrEmpty(configuredModel))
        {
            return configuredModel;
        }

        // Default models based on provider
        return provider switch
        {
            AIProvider.Ollama => "llama3.2",
            AIProvider.AzureOpenAI => "gpt-4o",
            AIProvider.GitHubModels => "gpt-4o-mini",
            AIProvider.FoundryLocal => "phi-3.5-mini",
            _ => throw new InvalidOperationException($"No default model available for provider: {provider}")
        };
    }
}

public enum AIProvider
{
    Ollama,
    AzureOpenAI,
    GitHubModels,
    FoundryLocal
}