using Microsoft.Extensions.Configuration;

namespace BuildWithAspire.AppHost.Extensions;

/// <summary>
/// Extensions for adding AI models using official Aspire integrations
/// </summary>
public static class AIModelExtensions
{
    /// <summary>
    /// Adds an AI model service to the application based on the configured provider using official integrations.
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
            AIProvider.AzureOpenAI => AddAzureOpenAIWithDeployment(builder, name, deploymentName, aiModel, configuration),
            AIProvider.Ollama => AddOllamaWithModel(builder, name, deploymentName, aiModel),
            AIProvider.GitHubModels => AddGitHubModelsWithModel(builder, name, aiModel),
            AIProvider.FoundryLocal => AddFoundryLocalWithModel(builder, name, deploymentName, aiModel),
            _ => throw new InvalidOperationException($"Unsupported AI provider: {aiProvider}. Supported providers: azureopenai, githubmodels, ollama, foundrylocal")
        };
    }

    /// <summary>
    /// Adds AI model configuration to a project resource using connection strings.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="aiService">The AI service resource.</param>
    /// <returns>The project resource builder with AI configuration.</returns>
    public static IResourceBuilder<ProjectResource> WithAIModel(
        this IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<IResourceWithConnectionString> aiService)
    {
        var configuration = builder.ApplicationBuilder.Configuration;
        var aiProvider = GetAIProvider(configuration);
        var deploymentName = GetDeploymentName(configuration);

        // Add the AI service reference (this provides the connection string)
        builder = builder.WithReference(aiService);

        // Add minimal environment variables for the AI provider type and deployment name
        // The connection string will be provided automatically by Aspire
        return builder
            .WithEnvironment("AI__Provider", aiProvider.ToString().ToLowerInvariant())
            .WithEnvironment("AI__DeploymentName", deploymentName);
    }

    private static IResourceBuilder<IResourceWithConnectionString> AddAzureOpenAIWithDeployment(
        IDistributedApplicationBuilder builder, 
        string name, 
        string deploymentName, 
        string aiModel, 
        IConfiguration configuration)
    {
        // Get configurable values from configuration with sensible defaults
        var modelVersion = configuration["AI:ModelVersion"] ?? "2024-05-13";
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

    private static IResourceBuilder<IResourceWithConnectionString> AddGitHubModelsWithModel(
        IDistributedApplicationBuilder builder, 
        string name, 
        string aiModel)
    {
        // Use official GitHub Models integration - it automatically handles the API key parameter
        return builder.AddGitHubModel(name, aiModel);
    }

    private static IResourceBuilder<IResourceWithConnectionString> AddFoundryLocalWithModel(
        IDistributedApplicationBuilder builder, 
        string name, 
        string deploymentName, 
        string aiModel)
    {
        // Use official Azure AI Foundry integration configured to run locally
        var foundry = builder.AddAzureAIFoundry(name).RunAsFoundryLocal();
        
        // Add deployment with proper Microsoft publisher for local foundry
        return foundry.AddDeployment(deploymentName, aiModel, "1", "Microsoft");
    }

    private static IResourceBuilder<IResourceWithConnectionString> AddOllamaWithModel(
        IDistributedApplicationBuilder builder, 
        string name, 
        string deploymentName, 
        string aiModel)
    {
        // Use official Ollama integration from Community Toolkit
        return builder.AddOllama(name)
            .WithDataVolume()
            .WithOpenWebUI()
            .AddModel(deploymentName, aiModel);
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