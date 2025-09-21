using Microsoft.Extensions.Configuration;

namespace BuildWithAspire.Abstractions;

public static class AIConfiguration
{
    public enum AIProvider
    {
        Ollama,
        AzureOpenAI,
        GitHubModels,
        AzureAIFoundry
    }

    public record AISettings(AIProvider Provider, string DeploymentName, string Model);

    public static AISettings GetSettings(IConfiguration configuration)
    {
        var provider = GetProvider(configuration);
        var deploymentName = GetDeploymentName(configuration);
        var model = GetModel(configuration, provider);

        return new AISettings(provider, deploymentName, model);
    }

    public static AIProvider GetProvider(IConfiguration configuration)
    {
        var providerString = configuration["AI:Provider"]?.ToLowerInvariant() ?? "ollama";
        return providerString switch
        {
            "ollama" => AIProvider.Ollama,
            "azureopenai" => AIProvider.AzureOpenAI,
            "githubmodels" => AIProvider.GitHubModels,
            "foundrylocal" or "azureaifoundry" => AIProvider.AzureAIFoundry,
            _ => throw new InvalidOperationException($"Unsupported AI provider: {providerString}. Supported providers: azureopenai, githubmodels, ollama, foundrylocal, azureaifoundry")
        };
    }

    public static string GetDeploymentName(IConfiguration configuration)
    {
        var deploymentName = configuration["AI:DeploymentName"];
        return string.IsNullOrEmpty(deploymentName) ? "chat" : deploymentName;
    }

    public static string GetModel(IConfiguration configuration, AIProvider? provider = null)
    {
        provider ??= GetProvider(configuration);

        var configuredModel = configuration["AI:Model"];
        if (!string.IsNullOrEmpty(configuredModel))
        {
            // Normalize GitHub Models naming (ensure vendor prefix openai/ when missing)
            if (provider == AIProvider.GitHubModels && !configuredModel.Contains('/'))
            {
                configuredModel = $"openai/{configuredModel}";
            }
            return configuredModel;
        }

        var model = provider switch
        {
            AIProvider.Ollama => "llama3.2",
            AIProvider.AzureOpenAI => "gpt-4o",
            AIProvider.GitHubModels => "openai/gpt-4o-mini",
            AIProvider.AzureAIFoundry => "phi-3.5-mini",
            _ => throw new InvalidOperationException($"No default model available for provider: {provider}")
        };
        return model;
    }
}
