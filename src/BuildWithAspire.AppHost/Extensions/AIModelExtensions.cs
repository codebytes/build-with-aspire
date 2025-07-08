// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            AIProvider.GitHubModels => builder.AddGitHubModels(name, aiModel),
            AIProvider.FoundryLocal => builder.AddFoundryLocal(name, aiModel),
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
            case AIProvider.Ollama:
                builder = builder
                    .WithReference(aiService, deploymentName)
                    .WaitFor(aiService);
                break;
            case AIProvider.FoundryLocal:
                builder = AddFoundryLocalConfiguration(builder)
                    .WithReference(aiService, deploymentName)
                    .WaitFor(aiService);
                break;
        }

        return builder;
    }

    /// <summary>
    /// Adds a GitHub Models resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="model">The model name to use with GitHub Models.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{GitHubModelsResource}"/>.</returns>
    public static IResourceBuilder<GitHubModelsResource> AddGitHubModels(
        this IDistributedApplicationBuilder builder,
        string name,
        string model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(model);

        var resource = new GitHubModelsResource(name, model);

        // Register the health check for this resource
        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddTypeActivatedCheck<GitHubModelsHealthCheck>(
            healthCheckKey,
            failureStatus: default,
            tags: default,
            resource);

        // Try to get the GitHub token from environment variable, if not available, create a parameter
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(githubToken))
        {
            // Use the environment variable directly
            resource.Key = null; // Will fall back to environment variable in connection string
            return builder.AddResource(resource)
                .WithEnvironment("AI_PROVIDER", "GitHub Models")
                .WithEnvironment("AI_MODEL", model)
                .WithEnvironment("AI_ENDPOINT", GitHubModelsResource.GitHubModelsEndpoint)
                .WithEnvironment("GITHUB_TOKEN", githubToken)
                .WithHealthCheck(healthCheckKey);
        }
        else
        {
            // Create a parameter for the GitHub token
            var keyParameter = builder.AddParameter("github-token", secret: true);
            resource.Key = keyParameter.Resource;
            return builder.AddResource(resource)
                .WithEnvironment("AI_PROVIDER", "GitHub Models")
                .WithEnvironment("AI_MODEL", model)
                .WithEnvironment("AI_ENDPOINT", GitHubModelsResource.GitHubModelsEndpoint)
                .WithEnvironment("GITHUB_TOKEN", keyParameter)
                .WithHealthCheck(healthCheckKey);
        }
    }

    /// <summary>
    /// Adds a Foundry Local resource to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="model">The model name to use with Foundry Local.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{FoundryLocalResource}"/>.</returns>
    public static IResourceBuilder<FoundryLocalResource> AddFoundryLocal(
        this IDistributedApplicationBuilder builder,
        string name,
        string model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(model);

        var resource = new FoundryLocalResource(name, model);

        // Register the health check for this resource
        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddTypeActivatedCheck<FoundryLocalHealthCheck>(
            healthCheckKey,
            failureStatus: default,
            tags: default,
            resource);

        return builder.AddResource(resource)
            .WithEnvironment("AI_PROVIDER", "Foundry Local")
            .WithEnvironment("AI_MODEL", model)
            .WithEnvironment("AI_ENDPOINT", resource.Endpoint)
            .WithEnvironment("FOUNDRY_LOCAL_AUTO_START", resource.AutoStart.ToString().ToLowerInvariant())
            .WithEnvironment("FOUNDRY_LOCAL_MODEL_CACHE_PATH", resource.ModelCachePath)
            .WithHealthCheck(healthCheckKey);
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

    private static IResourceBuilder<ProjectResource> AddFoundryLocalConfiguration(
        IResourceBuilder<ProjectResource> builder)
    {
        // Additional Foundry Local specific configuration for the API service
        // Environment variables are already set via the resource
        return builder;
    }
}

public enum AIProvider
{
    Ollama,
    AzureOpenAI,
    GitHubModels,
    FoundryLocal
}
