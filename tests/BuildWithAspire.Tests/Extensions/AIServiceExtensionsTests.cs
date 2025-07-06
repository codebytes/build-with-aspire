using BuildWithAspire.ApiService.Configuration;
using BuildWithAspire.ApiService.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildWithAspire.Tests.Extensions;

public class AIServiceExtensionsTests
{
    [Fact]
    public void AddAIServices_RegistersAISettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "azureopenai",
            ["AI:DeploymentName"] = "test-deployment",
            ["AI:Model"] = "gpt-4"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act
        builder.AddAIServices();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var aiSettings = serviceProvider.GetService<AIConfiguration.AISettings>();
        
        Assert.NotNull(aiSettings);
        Assert.Equal(AIConfiguration.AIProvider.AzureOpenAI, aiSettings.Provider);
        Assert.Equal("test-deployment", aiSettings.DeploymentName);
        Assert.Equal("gpt-4", aiSettings.Model);
    }

    [Fact]
    public void AddAIServices_RegistersLoggingService()
    {
        // Arrange
        var builder = new HostApplicationBuilder();
        builder.Services.AddLogging();

        // Act
        builder.AddAIServices();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        
        Assert.Contains(hostedServices, service => service.GetType().Name.Contains("AIConfigurationLogger"));
    }

    [Theory]
    [InlineData("ollama")]
    [InlineData("azureopenai")]
    [InlineData("githubmodels")]
    [InlineData("foundrylocal")]
    public void AddAIServices_WithDifferentProviders_DoesNotThrow(string provider)
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = provider
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddLogging();

        // For GitHub Models, add a mock token to prevent initialization errors
        if (provider == "githubmodels")
        {
            builder.Configuration["GITHUB_TOKEN"] = "test-token";
        }

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => builder.AddAIServices());
        Assert.Null(exception);
    }

    [Fact]
    public void AddAIServices_WithInvalidProvider_ThrowsException()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "invalid-provider"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.AddAIServices());
    }

    [Fact]
    public void AddAIServices_WithGitHubModelsButNoToken_ThrowsException()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "githubmodels"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddAIServices());
        Assert.Contains("GitHub token not found", exception.Message);
    }

    [Fact]
    public void AddAIServices_WithGitHubModelsAndValidToken_RegistersSuccessfully()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "githubmodels",
            ["GITHUB_TOKEN"] = "valid-test-token"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddLogging();

        // Act
        var exception = Record.Exception(() => builder.AddAIServices());

        // Assert
        Assert.Null(exception);
        
        var serviceProvider = builder.Services.BuildServiceProvider();
        var aiSettings = serviceProvider.GetRequiredService<AIConfiguration.AISettings>();
        Assert.Equal(AIConfiguration.AIProvider.GitHubModels, aiSettings.Provider);
    }

    [Fact]
    public void AddAIServices_WithDefaultConfiguration_UsesOllamaDefaults()
    {
        // Arrange
        var builder = new HostApplicationBuilder();
        builder.Services.AddLogging();

        // Act
        builder.AddAIServices();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var aiSettings = serviceProvider.GetRequiredService<AIConfiguration.AISettings>();
        
        Assert.Equal(AIConfiguration.AIProvider.Ollama, aiSettings.Provider);
        Assert.Equal("chat", aiSettings.DeploymentName);
        Assert.Equal("llama3.2", aiSettings.Model);
    }

    [Fact]
    public void AddAIServices_ReturnsBuilder_ForMethodChaining()
    {
        // Arrange
        var builder = new HostApplicationBuilder();

        // Act
        var result = builder.AddAIServices();

        // Assert
        Assert.Same(builder, result);
    }
}