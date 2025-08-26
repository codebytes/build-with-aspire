using NSubstitute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using BuildWithAspire.ApiService.Configuration;
using BuildWithAspire.ApiService.Services;

namespace BuildWithAspire.ApiService.UnitTests.Services;

public class SimpleChatServiceTests
{
    [Fact]
    public void ChatService_Constructor_ShouldAcceptDependencies()
    {
        // Arrange
        var mockChatClient = Substitute.For<IChatClient>();
        var mockLogger = Substitute.For<ILogger<ChatService>>();
        var aiSettings = new AIConfiguration.AISettings(AIConfiguration.AIProvider.Ollama, "test-deployment", "test-model");
        var mockKernel = new Kernel(); // Use real kernel since it's sealed
        var mockChatCompletion = Substitute.For<IChatCompletionService>();
        var mockServiceProvider = Substitute.For<IServiceProvider>();

        var mockConfiguration = Substitute.For<IConfiguration>();
        mockConfiguration.GetValue<int>("AI:TimeoutMinutes", 3).Returns(3);
        mockConfiguration.GetValue<bool>("MCP:ServerEnabled", Arg.Any<bool>()).Returns(false);
        
        // Act & Assert (should not throw)
        var chatService = new ChatService(mockChatClient, mockLogger, aiSettings, mockKernel, mockChatCompletion, mockServiceProvider, mockConfiguration);
        Assert.NotNull(chatService);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("assistant")]
    [InlineData("system")]
    [InlineData("USER")]
    [InlineData("Assistant")]
    [InlineData("SYSTEM")]
    public void ChatMessageRequest_ValidRoles_ShouldBeAccepted(string role)
    {
        // Arrange & Act
        var request = new ChatMessageRequest
        {
            Role = role,
            Content = "Test message"
        };

        // Assert
        Assert.Equal(role, request.Role);
        Assert.Equal("Test message", request.Content);
    }

    [Fact]
    public void ChatMessageRequest_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var request = new ChatMessageRequest();

        // Act
        request.Role = "user";
        request.Content = "Hello, world!";

        // Assert
        Assert.Equal("user", request.Role);
        Assert.Equal("Hello, world!", request.Content);
    }
}
