using Microsoft.Extensions.Configuration;
using Aspire.Hosting;
using Aspire.Hosting.Foundry;
using Aspire.Hosting.GitHub;
using BuildWithAspire.Abstractions;
using AIProvider = BuildWithAspire.Abstractions.AIConfiguration.AIProvider;

namespace BuildWithAspire.AppHost.Extensions;

/// <summary>
/// Extensions for configuring AI models in .NET Aspire AppHost.
/// </summary>
public static class AIModelExtensions
{
    /// <summary>
    /// Adds an AI model resource based on configuration.
    /// </summary>
    /// <param name="builder">The distributed application builder</param>
    /// <param name="name">The resource name for references</param>
    /// <returns>The AI service resource builder</returns>
    public static IResourceBuilder<IResourceWithConnectionString> AddAIModel(
        this IDistributedApplicationBuilder builder,
        string name = "ai-service")
    {
        var settings = AIConfiguration.GetSettings(builder.Configuration);

        Console.WriteLine(
            $"Configuring AI: {settings.Provider}, Model: {settings.Model}, " +
            $"Deployment: {settings.DeploymentName}");

        return settings.Provider switch
        {
            AIProvider.AzureOpenAI => ConfigureAzureOpenAI(builder, name, settings),
            AIProvider.Ollama => ConfigureOllama(builder, name, settings),
            AIProvider.GitHubModels => ConfigureGitHubModels(builder, settings),
            AIProvider.AzureAIFoundry => ConfigureAzureAIFoundry(builder, name, settings),
            _ => throw new InvalidOperationException(
                $"Unsupported AI provider: {settings.Provider}")
        };
    }

    /// <summary>
    /// Connects a project to an AI service with proper environment configuration.
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithAIModel(
        this IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<IResourceWithConnectionString> aiService,
        string deploymentName = "chat")
    {
        var settings = AIConfiguration.GetSettings(builder.ApplicationBuilder.Configuration);

        builder
            .WithEnvironment("AI:Provider", settings.Provider.ToString().ToLowerInvariant())
            .WithEnvironment("AI:DeploymentName", deploymentName)
            .WithEnvironment("AI:Model", settings.Model);

        return ConnectToAIService(builder, aiService, settings.Provider);
    }

    private static IResourceBuilder<IResourceWithConnectionString> ConfigureAzureOpenAI(
        IDistributedApplicationBuilder builder,
        string name,
        AIConfiguration.AISettings settings)
    {
        var config = builder.Configuration;
        var modelVersion = config["AI:ModelVersion"] ?? "2025-08-07";
        var skuName = config["AI:SkuName"] ?? "GlobalStandard";
        var skuCapacity = config.GetValue<int?>("AI:SkuCapacity") ?? 150;

        var openai = builder.AddAzureOpenAI(name);
        openai.AddDeployment(settings.DeploymentName, settings.Model, modelVersion);

        openai.ConfigureInfrastructure(infra =>
        {
            var deployments = infra.GetProvisionableResources()
                .OfType<Azure.Provisioning.CognitiveServices.CognitiveServicesAccountDeployment>();

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

    private static IResourceBuilder<IResourceWithConnectionString> ConfigureOllama(
        IDistributedApplicationBuilder builder,
        string name,
        AIConfiguration.AISettings settings)
    {
        return builder.AddOllama(name)
            .WithDataVolume()
            .WithOpenWebUI()
            .AddModel(settings.DeploymentName, settings.Model);
    }

    private static IResourceBuilder<IResourceWithConnectionString> ConfigureGitHubModels(
        IDistributedApplicationBuilder builder,
        AIConfiguration.AISettings settings)
    {
        // Default to the strongly-typed model catalog; fall back to the string overload
        // only when a model is explicitly configured via AI:Model.
        var github = settings.ModelExplicitlyConfigured
            ? builder.AddGitHubModel(settings.DeploymentName, settings.Model)
            : builder.AddGitHubModel(settings.DeploymentName, GitHubModel.OpenAI.OpenAIGpt5Mini);

        return github.WithHealthCheck();
    }

    private static IResourceBuilder<IResourceWithConnectionString> ConfigureAzureAIFoundry(
        IDistributedApplicationBuilder builder,
        string name,
        AIConfiguration.AISettings settings)
    {
        var config = builder.Configuration;
        var isLocal = config["AI:Provider"]?.ToLowerInvariant() == "foundrylocal";
        var format = config["AI:ModelFormat"] ?? config["AI:ModelVendor"] ?? "Microsoft";
        var version = config["AI:ModelVersion"] ?? "1";
        var skuCapacity = config.GetValue<int?>("AI:SkuCapacity") ?? 20;

        Console.WriteLine(
            $"Azure AI Foundry: {(isLocal ? "Local" : "Cloud")}, " +
            $"Model: {settings.Model}, Version: {version}");

        var foundry = builder.AddFoundry(name);
        if (isLocal)
        {
            foundry = foundry.RunAsFoundryLocal();
        }

        // Default to the strongly-typed model catalog (which encodes the correct model
        // version/format per environment); fall back to the string overload only when a
        // model is explicitly configured via AI:Model.
        // Local default is qwen2.5-1.5b: unlike the Foundry-optimized phi-4-mini build (which
        // does not emit tool calls), the qwen2.5 builds reliably invoke MCP tools. Swap to the
        // larger FoundryModel.Local.Qwen257b (qwen2.5-7b) for higher tool-calling reliability.
        var deployment = settings.ModelExplicitlyConfigured
            ? foundry.AddDeployment(settings.DeploymentName, settings.Model, version, format)
            : foundry.AddDeployment(
                settings.DeploymentName,
                isLocal ? FoundryModel.Local.Qwen2515b : FoundryModel.OpenAI.Gpt5Mini);

        return deployment.WithProperties(p => p.SkuCapacity = skuCapacity);
    }

    private static IResourceBuilder<ProjectResource> ConnectToAIService(
        IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<IResourceWithConnectionString> aiService,
        AIProvider provider)
    {
        return provider switch
        {
            AIProvider.AzureOpenAI => builder.WithReference(aiService).WaitFor(aiService),
            AIProvider.GitHubModels => builder.WithReference(aiService).WaitFor(aiService),
            AIProvider.AzureAIFoundry => builder.WithReference(aiService).WaitFor(aiService),
            AIProvider.Ollama => builder
                .WithReference(aiService, connectionName: aiService.Resource.Name)
                .WaitFor(aiService),
            _ => throw new InvalidOperationException($"Unknown provider: {provider}")
        };
    }

}
