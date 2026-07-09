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

    public record AISettings(AIProvider Provider, string DeploymentName, string Model, int TimeoutSeconds)
    {
        /// <summary>
        /// True when the model was set explicitly via <c>AI:Model</c> configuration,
        /// as opposed to falling back to the provider default. The AppHost uses this to
        /// choose between the strongly-typed model catalog (defaults) and the string
        /// overloads (explicit overrides).
        /// </summary>
        public bool ModelExplicitlyConfigured { get; init; }
    }

    public static AISettings GetSettings(IConfiguration configuration)
    {
        var provider = GetProvider(configuration);
        var deploymentName = GetDeploymentName(configuration);
        var model = GetModel(configuration, provider);
        var timeoutSeconds = GetTimeoutSeconds(configuration);

        return new AISettings(provider, deploymentName, model, timeoutSeconds)
        {
            ModelExplicitlyConfigured = !string.IsNullOrEmpty(configuration["AI:Model"])
        };
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
            AIProvider.AzureOpenAI => "gpt-5",
            AIProvider.GitHubModels => "openai/gpt-5-mini",
            // Foundry Local runs small on-device models; cloud Azure AI Foundry defaults to a
            // hosted OpenAI model. These names mirror the strongly-typed catalog constants the
            // AppHost deploys (FoundryModel.Local.Qwen2515b / FoundryModel.OpenAI.Gpt5Mini) so the
            // advertised AI:Model matches the deployed model. qwen2.5-1.5b reliably emits tool
            // calls locally (the phi-4-mini Foundry build does not).
            AIProvider.AzureAIFoundry => IsFoundryLocal(configuration) ? "qwen2.5-1.5b" : "gpt-5-mini",
            _ => throw new InvalidOperationException($"No default model available for provider: {provider}")
        };
        return model;
    }

    private static bool IsFoundryLocal(IConfiguration configuration) =>
        configuration["AI:Provider"]?.ToLowerInvariant() == "foundrylocal";

    public static int GetTimeoutSeconds(IConfiguration configuration)
    {
        var timeoutString = configuration["AI:TimeoutSeconds"];
        if (int.TryParse(timeoutString, out var timeout) && timeout > 0)
        {
            return timeout;
        }
        return 120; // Default 2 minutes
    }
}
