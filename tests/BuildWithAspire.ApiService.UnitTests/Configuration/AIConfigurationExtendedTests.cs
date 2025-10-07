namespace BuildWithAspire.ApiService.UnitTests.Configuration;

public class AIConfigurationExtendedTests
{
    [Theory]
    [InlineData("azureopenai", AIConfiguration.AIProvider.AzureOpenAI)]
    [InlineData("ollama", AIConfiguration.AIProvider.Ollama)]
    [InlineData("githubmodels", AIConfiguration.AIProvider.GitHubModels)]
    [InlineData("foundrylocal", AIConfiguration.AIProvider.AzureAIFoundry)]
    [InlineData("AzureOpenAI", AIConfiguration.AIProvider.AzureOpenAI)]
    [InlineData("OLLAMA", AIConfiguration.AIProvider.Ollama)]
    public void GetProvider_WithValidProviders_ShouldReturnCorrectProvider(string configValue, AIConfiguration.AIProvider expectedProvider)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = configValue
            })
            .Build();

        // Act
        var provider = AIConfiguration.GetProvider(configuration);

        // Assert
        Assert.Equal(expectedProvider, provider);
    }

    [Fact]
    public void GetProvider_WithMissingConfiguration_ShouldReturnDefaultOllama()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var provider = AIConfiguration.GetProvider(configuration);

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.Ollama, provider);
    }

    [Theory]
    [InlineData(AIConfiguration.AIProvider.AzureOpenAI, "gpt-4o")]
    [InlineData(AIConfiguration.AIProvider.Ollama, "llama3.2")]
    [InlineData(AIConfiguration.AIProvider.GitHubModels, "openai/gpt-4o-mini")]
    [InlineData(AIConfiguration.AIProvider.AzureAIFoundry, "phi-3.5-mini")]
    public void GetModel_WithProviders_ShouldReturnCorrectDefaults(AIConfiguration.AIProvider provider, string expectedModel)
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var model = AIConfiguration.GetModel(configuration, provider);

        // Assert
        Assert.Equal(expectedModel, model);
    }

    [Fact]
    public void AISettings_WithAllProviders_ShouldConstructCorrectly()
    {
        // Arrange & Act
        var ollamaSettings = new AIConfiguration.AISettings(
            AIConfiguration.AIProvider.Ollama,
            "llama-deployment",
            "llama3.2",
            120
        );

        var azureSettings = new AIConfiguration.AISettings(
            AIConfiguration.AIProvider.AzureOpenAI,
            "gpt-deployment",
            "gpt-4o",
            120
        );

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.Ollama, ollamaSettings.Provider);
        Assert.Equal("llama-deployment", ollamaSettings.DeploymentName);
        Assert.Equal("llama3.2", ollamaSettings.Model);

        Assert.Equal(AIConfiguration.AIProvider.AzureOpenAI, azureSettings.Provider);
        Assert.Equal("gpt-deployment", azureSettings.DeploymentName);
        Assert.Equal("gpt-4o", azureSettings.Model);
    }

    [Theory]
    [InlineData("custom-chat-deployment")]
    [InlineData("my-model-v2")]
    [InlineData("production-deployment")]
    public void GetDeploymentName_WithCustomValues_ShouldReturnCustomValue(string customDeployment)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:DeploymentName"] = customDeployment
            })
            .Build();

        // Act
        var deploymentName = AIConfiguration.GetDeploymentName(configuration);

        // Assert
        Assert.Equal(customDeployment, deploymentName);
    }

    [Theory]
    [InlineData("gpt-4")]
    [InlineData("gpt-3.5-turbo")]
    [InlineData("llama3.1")]
    [InlineData("custom-model-v1")]
    public void GetModel_WithCustomModel_ShouldReturnCustomModel(string customModel)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Model"] = customModel
            })
            .Build();

        // Act
        var model = AIConfiguration.GetModel(configuration, AIConfiguration.AIProvider.Ollama);

        // Assert
        Assert.Equal(customModel, model);
    }
}
