using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;

namespace BuildWithAspire.AppHost.Extensions;

public static class AIModelExtensions
{
    /// <summary>
    /// Adds an AI model service to the application based on the configured provider.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the AI service resource.</param>
    /// <returns>A resource builder for the AI service.</returns>
    public static IResourceBuilder<IResourceWithConnectionString> AddAIModel(
        this IDistributedApplicationBuilder builder,
        string name = "ai-service")
    {
        var configuration = builder.Configuration;
        var aiProvider = GetAIProvider(configuration);
        var deploymentName = GetDeploymentName(configuration);
        var aiModel = GetAIModel(configuration, aiProvider);

        return aiProvider switch
        {
            AIProvider.AzureOpenAI => AddAzureOpenAIWithDeployment(builder, name, deploymentName, aiModel),
            AIProvider.Ollama => builder.AddOllama(name)
                .WithDataVolume()
                .WithOpenWebUI()
                .AddModel(deploymentName, aiModel),
            AIProvider.GitHubModels => AddGitHubModels(builder, name, aiModel),
            AIProvider.FoundryLocal => AddFoundryLocal(builder, name, deploymentName, aiModel),
            _ => throw new InvalidOperationException($"Unsupported AI provider: {aiProvider}. Supported providers: azureopenai, githubmodels, ollama, foundrylocal")
        };

        // Local function to handle AzureOpenAI setup
        static IResourceBuilder<IResourceWithConnectionString> AddAzureOpenAIWithDeployment(
            IDistributedApplicationBuilder builder, string name, string deploymentName, string aiModel)
        {
            var configuration = builder.Configuration;

            // Get configurable values from configuration with sensible defaults
            var modelVersion = configuration["AI:ModelVersion"] ?? "2024-11-20";
            var skuName = configuration["AI:SkuName"] ?? "GlobalStandard";
            var skuCapacity = configuration.GetValue<int?>("AI:SkuCapacity") ?? 150;

            var openai = builder.AddAzureOpenAI(name);
            openai.AddDeployment(
                name: deploymentName,
                modelName: aiModel,
                modelVersion: modelVersion);
            openai.ConfigureInfrastructure(infra =>
            {
                var resources = infra.GetProvisionableResources();
                var deployments = resources.OfType<Azure.Provisioning.CognitiveServices.CognitiveServicesAccountDeployment>();
                foreach (var deployment in deployments)
                {
                    deployment.Sku = new Azure.Provisioning.CognitiveServices.CognitiveServicesSku
                    {
                        Name = skuName,
                        Capacity = skuCapacity
                    };
                }
            });

            return openai;
        }

        // Local function to handle GitHub Models setup using official integration
        static IResourceBuilder<IResourceWithConnectionString> AddGitHubModels(
            IDistributedApplicationBuilder builder, string name, string aiModel)
        {
            // GitHub Models integration automatically creates a parameter named {resourceName}-gh-apikey
            // Let Aspire handle the parameter creation automatically
            var githubModel = builder.AddGitHubModel(name, aiModel);
            
            // If GitHub token is available in environment, configure it
            var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(githubToken))
            {
                // The parameter is automatically created, we just need to set its value
                // This will be handled by the Aspire runtime through environment variables
                return githubModel;
            }
            
            return githubModel;
        }

        // Local function to handle Foundry Local setup using official integration
        static IResourceBuilder<IResourceWithConnectionString> AddFoundryLocal(
            IDistributedApplicationBuilder builder, string name, string deploymentName, string aiModel)
        {
            // Create Azure AI Foundry resource configured to run locally
            var foundry = builder.AddAzureAIFoundry(name).RunAsFoundryLocal();
            
            // Add deployment with proper Microsoft publisher
            // The version "1" and publisher "Microsoft" are required for Foundry Local
            return foundry.AddDeployment(deploymentName, aiModel, "1", "Microsoft");
        }
    }

    /// <summary>
    /// Adds AI model configuration to a project resource.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="aiService">The AI service resource.</param>
    /// <param name="deploymentName">The deployment name for the AI service.</param>
    /// <returns>The project resource builder with AI configuration.</returns>
    public static IResourceBuilder<ProjectResource> WithAIModel(
        this IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<IResourceWithConnectionString> aiService,
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
            case AIProvider.AzureOpenAI:
                builder = builder
                    .WithReference(aiService)
                    .WaitFor(aiService);
                break;
            case AIProvider.GitHubModels:
            case AIProvider.FoundryLocal:
            case AIProvider.Ollama:
                // For GitHub Models and Foundry Local, use the deployment name as connection name
                // This creates a connection string like ConnectionStrings:chat
                builder = builder
                    .WithReference(aiService, deploymentName)
                    .WaitFor(aiService);
                break;
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
