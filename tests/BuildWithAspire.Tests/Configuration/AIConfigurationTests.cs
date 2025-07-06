using BuildWithAspire.ApiService.Configuration;
using Microsoft.Extensions.Configuration;

namespace BuildWithAspire.Tests.Configuration;

public class AIConfigurationTests
{
    [Theory]
    [InlineData("ollama", AIConfiguration.AIProvider.Ollama)]
    [InlineData("azureopenai", AIConfiguration.AIProvider.AzureOpenAI)]
    [InlineData("githubmodels", AIConfiguration.AIProvider.GitHubModels)]
    [InlineData("foundrylocal", AIConfiguration.AIProvider.FoundryLocal)]
    [InlineData("OLLAMA", AIConfiguration.AIProvider.Ollama)] // Test case insensitivity
    public void GetProvider_WithValidProviderString_ReturnsCorrectEnum(string providerString, AIConfiguration.AIProvider expected)
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = providerString
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var result = AIConfiguration.GetProvider(configuration);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetProvider_WithNoConfiguration_ReturnsOllamaDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = AIConfiguration.GetProvider(configuration);

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.Ollama, result);
    }

    [Fact]
    public void GetProvider_WithInvalidProvider_ThrowsException()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "invalid-provider"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AIConfiguration.GetProvider(configuration));
        Assert.Contains("Unsupported AI provider", exception.Message);
        Assert.Contains("invalid-provider", exception.Message);
    }

    [Theory]
    [InlineData("custom-deployment", "custom-deployment")]
    [InlineData(null, "chat")]
    [InlineData("", "chat")]
    public void GetDeploymentName_ReturnsExpectedValue(string? configValue, string expected)
    {
        // Arrange
        var configData = new Dictionary<string, string?>();
        if (configValue != null)
        {
            configData["AI:DeploymentName"] = configValue;
        }
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var result = AIConfiguration.GetDeploymentName(configuration);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(AIConfiguration.AIProvider.Ollama, "llama3.2")]
    [InlineData(AIConfiguration.AIProvider.AzureOpenAI, "gpt-4o")]
    [InlineData(AIConfiguration.AIProvider.GitHubModels, "gpt-4o-mini")]
    [InlineData(AIConfiguration.AIProvider.FoundryLocal, "phi-3.5-mini")]
    public void GetModel_WithNoConfiguredModel_ReturnsDefaultForProvider(AIConfiguration.AIProvider provider, string expectedDefault)
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = AIConfiguration.GetModel(configuration, provider);

        // Assert
        Assert.Equal(expectedDefault, result);
    }

    [Fact]
    public void GetModel_WithConfiguredModel_ReturnsConfiguredModel()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Model"] = "custom-model"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var result = AIConfiguration.GetModel(configuration, AIConfiguration.AIProvider.Ollama);

        // Assert
        Assert.Equal("custom-model", result);
    }

    [Fact]
    public void GetSettings_WithFullConfiguration_ReturnsCompleteSettings()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "azureopenai",
            ["AI:DeploymentName"] = "test-deployment",
            ["AI:Model"] = "gpt-4-turbo"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var result = AIConfiguration.GetSettings(configuration);

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.AzureOpenAI, result.Provider);
        Assert.Equal("test-deployment", result.DeploymentName);
        Assert.Equal("gpt-4-turbo", result.Model);
    }

    [Fact]
    public void GetSettings_WithMinimalConfiguration_ReturnsDefaultSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = AIConfiguration.GetSettings(configuration);

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.Ollama, result.Provider);
        Assert.Equal("chat", result.DeploymentName);
        Assert.Equal("llama3.2", result.Model);
    }

    [Fact]
    public void GetSettings_WithPartialConfiguration_CombinesConfiguredAndDefaults()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "githubmodels",
            ["AI:DeploymentName"] = "custom-deployment"
            // AI:Model not specified - should use default for GitHubModels
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var result = AIConfiguration.GetSettings(configuration);

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.GitHubModels, result.Provider);
        Assert.Equal("custom-deployment", result.DeploymentName);
        Assert.Equal("gpt-4o-mini", result.Model); // Default for GitHubModels
    }
}