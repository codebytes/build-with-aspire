namespace BuildWithAspire.ApiService.UnitTests.Services;

public class ChatServiceTests
{
    [Fact]
    public void AISettings_ShouldConstructWithValidParameters()
    {
        // Arrange & Act
        var settings = new AIConfiguration.AISettings(
            AIConfiguration.AIProvider.Ollama,
            "test-deployment",
            "test-model",
            120
        );

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.Ollama, settings.Provider);
        Assert.Equal("test-deployment", settings.DeploymentName);
        Assert.Equal("test-model", settings.Model);
        Assert.Equal(120, settings.TimeoutSeconds);
    }

    [Fact]
    public void GetProvider_WithValidConfiguration_ShouldReturnCorrectProvider()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "ollama"
            })
            .Build();

        // Act
        var provider = AIConfiguration.GetProvider(configuration);

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.Ollama, provider);
    }

    [Fact]
    public void GetProvider_WithInvalidConfiguration_ShouldThrowException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "invalid-provider"
            })
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => AIConfiguration.GetProvider(configuration));
    }

    [Fact]
    public void GetDeploymentName_WithValidConfiguration_ShouldReturnValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:DeploymentName"] = "custom-deployment"
            })
            .Build();

        // Act
        var deploymentName = AIConfiguration.GetDeploymentName(configuration);

        // Assert
        Assert.Equal("custom-deployment", deploymentName);
    }

    [Fact]
    public void GetDeploymentName_WithEmptyConfiguration_ShouldReturnDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var deploymentName = AIConfiguration.GetDeploymentName(configuration);

        // Assert
        Assert.Equal("chat", deploymentName);
    }

    [Fact]
    public void GetModel_WithOllamaProvider_ShouldReturnDefaultModel()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var model = AIConfiguration.GetModel(configuration, AIConfiguration.AIProvider.Ollama);

        // Assert
        Assert.Equal("llama3.2", model);
    }

    [Fact]
    public void GetModel_WithAzureOpenAIProvider_ShouldReturnDefaultModel()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var model = AIConfiguration.GetModel(configuration, AIConfiguration.AIProvider.AzureOpenAI);

        // Assert
        Assert.Equal("gpt-5", model);
    }

    [Fact]
    public void GetModel_WithCustomConfiguration_ShouldReturnCustomModel()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Model"] = "custom-model"
            })
            .Build();

        // Act
        var model = AIConfiguration.GetModel(configuration, AIConfiguration.AIProvider.Ollama);

        // Assert
        Assert.Equal("custom-model", model);
    }
}
