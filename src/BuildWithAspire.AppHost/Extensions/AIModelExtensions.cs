using Microsoft.Extensions.Configuration;
using Aspire.Hosting; // core builder
using Aspire.Hosting.GitHub.Models; // AddGitHubModel extension
using BuildWithAspire.Abstractions; // Shared AIConfiguration
using AIProvider = BuildWithAspire.Abstractions.AIConfiguration.AIProvider;

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
    var settings = AIConfiguration.GetSettings(configuration);
    var aiProvider = settings.Provider;
    var deploymentName = settings.DeploymentName;
    var aiModel = settings.Model;

        Console.WriteLine($"AddAIModel: Provider={aiProvider}, Model={aiModel}, Deployment={deploymentName}, Environment={builder.Environment.EnvironmentName}");

        return aiProvider switch
        {
            AIProvider.AzureOpenAI => AddAzureOpenAIWithDeployment(builder, name, deploymentName, aiModel, configuration),
            AIProvider.Ollama => builder.AddOllama(name)
                .WithDataVolume()
                .WithOpenWebUI()
                .AddModel(deploymentName, aiModel),
            AIProvider.GitHubModels => builder.AddGitHubModel(deploymentName, aiModel)
             .WithHealthCheck(),
            AIProvider.AzureAIFoundry => AddAzureAIFoundryResource(builder, name, deploymentName, aiModel, configuration),
            _ => throw new InvalidOperationException($"Supported providers: azureopenai, githubmodels, ollama, azureaifoundry")
        };
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
    var settings = AIConfiguration.GetSettings(configuration);
    var aiProvider = settings.Provider;
    var aiModel = settings.Model;

        // Add environment variables for AI configuration
        builder = builder
            .WithEnvironment("AI:Provider", aiProvider.ToString().ToLowerInvariant())
            .WithEnvironment("AI:DeploymentName", deploymentName)
            .WithEnvironment("AI:Model", aiModel);

        // Provider-specific configuration (FoundryLocal is treated under AzureAIFoundry path and still returns a resource)
        return aiProvider switch
        {
            AIProvider.AzureOpenAI => builder.WithReference(aiService).WaitFor(aiService),
            AIProvider.GitHubModels => builder.WithReference(aiService).WaitFor(aiService),
            AIProvider.Ollama => builder.WithReference(aiService, deploymentName).WaitFor(aiService),
            AIProvider.AzureAIFoundry => AddAzureAIFoundryReference(builder, aiService),
            _ => throw new InvalidOperationException($"Unsupported AI provider: {aiProvider}")
        };
    }

    private static IResourceBuilder<ProjectResource> AddAzureAIFoundryReference(
        IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<IResourceWithConnectionString> aiService)
    {
        Console.WriteLine($"Adding Azure AI Foundry reference for {aiService.Resource.Name}");
        return builder
            .WithReference(aiService)
            .WaitFor(aiService);
    }

    private static IResourceBuilder<IResourceWithConnectionString> AddAzureOpenAIWithDeployment(
        IDistributedApplicationBuilder builder,
        string name,
        string deploymentName,
        string aiModel,
        IConfiguration configuration)
    {
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

    // GitHub Models handled directly via AddGitHubModel (no manual connection string building required)


    private static IResourceBuilder<IResourceWithConnectionString> AddAzureAIFoundryResource(
        IDistributedApplicationBuilder builder,
        string name,
        string deploymentName,
        string aiModel,
        IConfiguration configuration)
    {
        var provider = configuration["AI:Provider"]?.ToLowerInvariant();
        var isLocal = provider == "foundrylocal"; // local developer experience pattern

        // Allow either AI:ModelFormat or AI:ModelVendor per evolving docs
        var format = configuration["AI:ModelFormat"]
                     ?? configuration["AI:ModelVendor"]
                     ?? "Microsoft"; // doc samples: "Microsoft" or "OpenAI"
        var version = configuration["AI:ModelVersion"] ?? "1"; // doc default
        var skuCapacity = configuration.GetValue<int?>("AI:SkuCapacity") ?? 20; // doc sample often shows 20

        Console.WriteLine($"Azure AI Foundry configuration: Provider={provider}, Local={isLocal}, Model={aiModel}, Version={version}, Vendor/Format={format}, SkuCapacity={skuCapacity}, Environment={builder.Environment.EnvironmentName}");

        // Official patterns from docs:
        // Local:  builder.AddAzureAIFoundry(name).RunAsFoundryLocal().AddDeployment(deploymentName, model, version, vendor)
        // Cloud:  var ai = builder.AddAzureAIFoundry(name); ai.AddDeployment(deploymentName, model, version, vendor).WithProperties(d => d.SkuCapacity = <cap>);

        var foundry = builder.AddAzureAIFoundry(name);
        if (isLocal)
        {
            foundry = foundry.RunAsFoundryLocal();
        }

        var deployment = foundry
            .AddDeployment(deploymentName, aiModel, version, format)
            .WithProperties(p =>
            {
                // Capacity meaningful for cloud; harmless locally. Keep minimal to align with doc guidance.
                p.SkuCapacity = skuCapacity;
            });

        Console.WriteLine(isLocal
            ? $"Configured FoundryLocal deployment {deploymentName}:{aiModel}@{version} ({format}) Capacity={skuCapacity}"
            : $"Configured Azure AI Foundry cloud deployment {deploymentName}:{aiModel}@{version} ({format}) Capacity={skuCapacity}");

        return deployment;
    }

}

