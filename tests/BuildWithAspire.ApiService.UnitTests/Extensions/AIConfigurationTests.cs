namespace BuildWithAspire.ApiService.UnitTests.Extensions;

public class AIConfigurationTests
{
    [Theory]
    [InlineData("ollama", AIConfiguration.AIProvider.Ollama)]
    [InlineData("azureopenai", AIConfiguration.AIProvider.AzureOpenAI)]
    [InlineData("githubmodels", AIConfiguration.AIProvider.GitHubModels)]
    [InlineData("foundrylocal", AIConfiguration.AIProvider.AzureAIFoundry)]
    [InlineData("OLLAMA", AIConfiguration.AIProvider.Ollama)]
    [InlineData("AZUREOPENAI", AIConfiguration.AIProvider.AzureOpenAI)]
    public void GetProvider_WithValidProviderStrings_ReturnsCorrectEnum(string providerString, AIConfiguration.AIProvider expectedProvider)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = providerString
            })
            .Build();

        // Act
        var result = AIConfiguration.GetProvider(configuration);

        // Assert
        Assert.Equal(expectedProvider, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("unknown-provider")]
    public void GetProvider_WithInvalidProviderStrings_ThrowsException(string providerString)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = providerString
            })
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => AIConfiguration.GetProvider(configuration));
    }

    [Fact]
    public void GetModel_WithCustomModel_ReturnsCustomModel()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Model"] = "custom-model"
            })
            .Build();

        // Act
        var result = AIConfiguration.GetModel(configuration, AIConfiguration.AIProvider.Ollama);

        // Assert
        Assert.Equal("custom-model", result);
    }

    [Theory]
    [InlineData(AIConfiguration.AIProvider.Ollama, "llama3.2")]
    [InlineData(AIConfiguration.AIProvider.AzureOpenAI, "gpt-5")]
    [InlineData(AIConfiguration.AIProvider.GitHubModels, "openai/gpt-5-mini")]
    [InlineData(AIConfiguration.AIProvider.AzureAIFoundry, "gpt-5-mini")]
    public void GetModel_WithDefaultConfiguration_ReturnsProviderDefaults(AIConfiguration.AIProvider provider, string expectedModel)
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = AIConfiguration.GetModel(configuration, provider);

        // Assert
        Assert.Equal(expectedModel, result);
    }

    [Fact]
    public void GetDeploymentName_WithCustomDeployment_ReturnsCustomName()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:DeploymentName"] = "custom-deployment"
            })
            .Build();

        // Act
        var result = AIConfiguration.GetDeploymentName(configuration);

        // Assert
        Assert.Equal("custom-deployment", result);
    }

    [Fact]
    public void GetDeploymentName_WithDefaultConfiguration_ReturnsChat()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = AIConfiguration.GetDeploymentName(configuration);

        // Assert
        Assert.Equal("chat", result);
    }

    [Fact]
    public void GetSettings_WithValidConfiguration_ReturnsSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "ollama",
                ["AI:DeploymentName"] = "test-deployment",
                ["AI:Model"] = "test-model"
            })
            .Build();

        // Act
        var result = AIConfiguration.GetSettings(configuration);

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.Ollama, result.Provider);
        Assert.Equal("test-deployment", result.DeploymentName);
        Assert.Equal("test-model", result.Model);
    }
}
