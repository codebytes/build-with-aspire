using NSubstitute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using BuildWithAspire.ApiService.Configuration;
using BuildWithAspire.ApiService.Services;

namespace BuildWithAspire.ApiService.UnitTests.Services;

public class ChatServiceSimpleTests
{
    [Fact]
    public void ChatService_Constructor_RequiresParameters()
    {
        // Arrange
        var mockChatClient = Substitute.For<IChatClient>();
        var mockLogger = Substitute.For<ILogger<ChatService>>();
        var aiSettings = new AIConfiguration.AISettings(AIConfiguration.AIProvider.Ollama, "test", "test-model");
        var mockKernel = new Kernel(); // Use real kernel since it's sealed
        var mockChatCompletion = Substitute.For<IChatCompletionService>();
        var mockServiceProvider = Substitute.For<IServiceProvider>();

        var mockConfiguration = Substitute.For<IConfiguration>();
        mockConfiguration.GetValue<int>("AI:TimeoutMinutes", 3).Returns(3);
        mockConfiguration.GetValue<bool>("MCP:ServerEnabled", Arg.Any<bool>()).Returns(false);
        
        // Act & Assert
        var service = new ChatService(mockChatClient, mockLogger, aiSettings, mockKernel, mockChatCompletion, mockServiceProvider, mockConfiguration);
        Assert.NotNull(service);
    }

    [Fact]
    public async Task ProcessMessage_WithNullMessage_ThrowsException()
    {
        // Arrange
        var mockChatClient = Substitute.For<IChatClient>();
        var mockLogger = Substitute.For<ILogger<ChatService>>();
        var aiSettings = new AIConfiguration.AISettings(AIConfiguration.AIProvider.Ollama, "test", "test-model");
        var mockKernel = new Kernel(); // Use real kernel since it's sealed
        var mockChatCompletion = Substitute.For<IChatCompletionService>();
        var mockServiceProvider = Substitute.For<IServiceProvider>();
        var mockConfiguration = Substitute.For<IConfiguration>();
        mockConfiguration.GetValue<int>("AI:TimeoutMinutes", 3).Returns(3);
        mockConfiguration.GetValue<bool>("MCP:ServerEnabled", Arg.Any<bool>()).Returns(false);
        var service = new ChatService(mockChatClient, mockLogger, aiSettings, mockKernel, mockChatCompletion, mockServiceProvider, mockConfiguration);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => service.ProcessMessage(null!));
    }

    [Fact]
    public async Task ProcessMessagesWithHistory_WithNullMessages_ThrowsException()
    {
        // Arrange
        var mockChatClient = Substitute.For<IChatClient>();
        var mockLogger = Substitute.For<ILogger<ChatService>>();
        var aiSettings = new AIConfiguration.AISettings(AIConfiguration.AIProvider.Ollama, "test", "test-model");
        var mockKernel = new Kernel(); // Use real kernel since it's sealed
        var mockChatCompletion = Substitute.For<IChatCompletionService>();
        var mockServiceProvider = Substitute.For<IServiceProvider>();
        var mockConfiguration = Substitute.For<IConfiguration>();
        mockConfiguration.GetValue<int>("AI:TimeoutMinutes", 3).Returns(3);
        mockConfiguration.GetValue<bool>("MCP:ServerEnabled", Arg.Any<bool>()).Returns(false);
        var service = new ChatService(mockChatClient, mockLogger, aiSettings, mockKernel, mockChatCompletion, mockServiceProvider, mockConfiguration);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessMessagesWithHistory(null!));
    }

    [Fact]
    public void ChatMessageRequest_Properties_Work()
    {
        // Arrange & Act
        var request = new ChatMessageRequest
        {
            Role = "user",
            Content = "Hello world"
        };

        // Assert
        Assert.Equal("user", request.Role);
        Assert.Equal("Hello world", request.Content);
    }

    [Fact]
    public void AISettings_Constructor_SetsProperties()
    {
        // Arrange & Act
        var settings = new AIConfiguration.AISettings(
            AIConfiguration.AIProvider.AzureOpenAI,
            "test-deployment",
            "gpt-4o"
        );

        // Assert
        Assert.Equal(AIConfiguration.AIProvider.AzureOpenAI, settings.Provider);
        Assert.Equal("test-deployment", settings.DeploymentName);
        Assert.Equal("gpt-4o", settings.Model);
    }
}
