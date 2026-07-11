using Microsoft.Extensions.Configuration;
using BuildWithAspire.Abstractions; // For AIConfiguration
using AIProvider = BuildWithAspire.Abstractions.AIConfiguration.AIProvider;

namespace BuildWithAspire.AppHost.UnitTests.Extensions;

public class AIModelExtensionsSimpleTests
{
    [Fact]
    public void GetAIProvider_WithOllama_ReturnsOllama()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("AI:Provider", "ollama")])
            .Build();

        // Act
        var provider = GetAIProviderFromConfiguration(configuration);

        // Assert
        Assert.Equal(AIProvider.Ollama, provider);
    }

    [Fact]
    public void GetAIProvider_WithAzureOpenAI_ReturnsAzureOpenAI()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("AI:Provider", "azureopenai")])
            .Build();

        // Act
        var provider = GetAIProviderFromConfiguration(configuration);

        // Assert
        Assert.Equal(AIProvider.AzureOpenAI, provider);
    }

    [Fact]
    public void GetAIProvider_WithGitHubModels_ReturnsGitHubModels()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("AI:Provider", "githubmodels")])
            .Build();

        // Act
        var provider = GetAIProviderFromConfiguration(configuration);

        // Assert
        Assert.Equal(AIProvider.GitHubModels, provider);
    }

    [Fact]
    public void GetAIProvider_WithFoundryLocal_ReturnsAzureAIFoundry()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("AI:Provider", "foundrylocal")])
            .Build();

        // Act
        var provider = GetAIProviderFromConfiguration(configuration);

        // Assert
        Assert.Equal(AIProvider.AzureAIFoundry, provider);
    }

    [Fact]
    public void GetAIProvider_WithAzureAIFoundry_ReturnsAzureAIFoundry()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("AI:Provider", "azureaifoundry")])
            .Build();

        // Act
        var provider = GetAIProviderFromConfiguration(configuration);

        // Assert
        Assert.Equal(AIProvider.AzureAIFoundry, provider);
    }

    [Fact]
    public void GetAIModel_WithDefaultProvider_ReturnsExpectedDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert - Test each provider's default model
    Assert.Equal("llama3.2", GetAIModelFromConfiguration(configuration, AIProvider.Ollama));
    Assert.Equal("gpt-5", GetAIModelFromConfiguration(configuration, AIProvider.AzureOpenAI));
    Assert.Equal("openai/gpt-5-mini", GetAIModelFromConfiguration(configuration, AIProvider.GitHubModels));
    Assert.Equal("gpt-5-mini", GetAIModelFromConfiguration(configuration, AIProvider.AzureAIFoundry));
    }

    [Fact]
    public void GetAIModel_WithConfiguredModel_ReturnsConfiguredModel()
    {
        // Arrange
        const string configuredModel = "custom-model";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("AI:Model", configuredModel)])
            .Build();

        // Act & Assert - All providers should return the configured model
        Assert.Equal(configuredModel, GetAIModelFromConfiguration(configuration, AIProvider.Ollama));
        Assert.Equal(configuredModel, GetAIModelFromConfiguration(configuration, AIProvider.AzureOpenAI));
    // GitHub models are normalized with openai/ prefix if missing
    Assert.Equal($"openai/{configuredModel}", GetAIModelFromConfiguration(configuration, AIProvider.GitHubModels));
        Assert.Equal(configuredModel, GetAIModelFromConfiguration(configuration, AIProvider.AzureAIFoundry));
    }

    // Helper methods to test the private methods indirectly through reflection
    private static AIProvider GetAIProviderFromConfiguration(IConfiguration configuration)
    {
        return AIConfiguration.GetProvider(configuration);
    }

    private static string GetAIModelFromConfiguration(IConfiguration configuration, AIProvider provider)
    {
        // Delegate to the central resolution logic so this test stays in sync with
        // AIConfiguration.GetModel (override handling, GitHub normalization, and the
        // Foundry Local vs cloud default all live there).
        return AIConfiguration.GetModel(configuration, provider);
    }
}
