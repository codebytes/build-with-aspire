using BuildWithAspire.ApiService.Configuration;
using BuildWithAspire.ApiService.Models;
using BuildWithAspire.ApiService.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace BuildWithAspire.Tests.Services;

public class ChatServiceTests
{
    private readonly Mock<ILogger<ChatService>> _mockLogger;
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly AIConfiguration.AISettings _aiSettings;
    private readonly ChatService _chatService;

    public ChatServiceTests()
    {
        _mockLogger = new Mock<ILogger<ChatService>>();
        _mockChatClient = new Mock<IChatClient>();
        _aiSettings = new AIConfiguration.AISettings(
            AIConfiguration.AIProvider.AzureOpenAI,
            "test-deployment",
            "gpt-4"
        );

        _chatService = new ChatService(_mockChatClient.Object, _mockLogger.Object, _aiSettings);
    }

    [Fact]
    public async Task ProcessMessage_WithValidMessage_ReturnsResponse()
    {
        // Arrange
        var testMessage = "Hello, world!";
        var expectedResponse = "Hello! How can I help you today?";
        
        var usage = new UsageDetails
        {
            InputTokenCount = 10,
            OutputTokenCount = 15,
            TotalTokenCount = 25
        };
        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, expectedResponse)])
        {
            Usage = usage
        };

        _mockChatClient.Setup(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _chatService.ProcessMessage(testMessage);

        // Assert
        Assert.Equal(expectedResponse, result);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing single message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WithNullMessage_HandlesGracefully()
    {
        // Arrange
        string? testMessage = null;
        var expectedResponse = "I'm here to help!";
        
        var usage = new UsageDetails
        {
            InputTokenCount = 5,
            OutputTokenCount = 8,
            TotalTokenCount = 13
        };
        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, expectedResponse)])
        {
            Usage = usage
        };

        _mockChatClient.Setup(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _chatService.ProcessMessage(testMessage!);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task ProcessMessage_LogsTokenUsage_WhenAvailable()
    {
        // Arrange
        var testMessage = "Test message";
        var expectedResponse = "Test response";
        
        var usage = new UsageDetails
        {
            InputTokenCount = 12,
            OutputTokenCount = 8,
            TotalTokenCount = 20
        };
        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, expectedResponse)])
        {
            Usage = usage
        };

        _mockChatClient.Setup(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _chatService.ProcessMessage(testMessage);

        // Assert - Verify cost metrics logging specifically
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token usage metrics") && v.ToString()!.Contains("InputTokens: 12")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessagesWithHistory_WithValidMessages_ReturnsResponse()
    {
        // Arrange
        var messages = new List<ChatMessageRequest>
        {
            new() { Role = "user", Content = "Hello" },
            new() { Role = "assistant", Content = "Hi there!" },
            new() { Role = "user", Content = "How are you?" }
        };
        
        var expectedResponse = "I'm doing well, thank you!";
        var usage = new UsageDetails
        {
            InputTokenCount = 25,
            OutputTokenCount = 18,
            TotalTokenCount = 43
        };
        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, expectedResponse)])
        {
            Usage = usage
        };

        _mockChatClient.Setup(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _chatService.ProcessMessagesWithHistory(messages);

        // Assert
        Assert.Equal(expectedResponse, result);
        
        // Verify that the chat client was called with the right number of messages
        _mockChatClient.Verify(c => c.GetResponseAsync(
            It.Is<IList<ChatMessage>>(msgs => msgs.Count == 4), // 1 system + 3 conversation messages
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessagesWithHistory_WithEmptyList_ReturnsResponse()
    {
        // Arrange
        var messages = new List<ChatMessageRequest>();
        var expectedResponse = "How can I help you?";
        
        var usage = new UsageDetails
        {
            InputTokenCount = 8,
            OutputTokenCount = 12,
            TotalTokenCount = 20
        };
        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, expectedResponse)])
        {
            Usage = usage
        };

        _mockChatClient.Setup(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _chatService.ProcessMessagesWithHistory(messages);

        // Assert
        Assert.Equal(expectedResponse, result);
        
        // Verify only system message is sent
        _mockChatClient.Verify(c => c.GetResponseAsync(
            It.Is<IList<ChatMessage>>(msgs => msgs.Count == 1), // Only system message
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WhenExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        var testMessage = "Test message";
        var expectedException = new InvalidOperationException("AI service error");

        _mockChatClient.Setup(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _chatService.ProcessMessage(testMessage));

        Assert.Contains("Failed to process message", exception.Message);
        
        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process single message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessagesWithHistory_LogsConversationMetrics()
    {
        // Arrange
        var messages = new List<ChatMessageRequest>
        {
            new() { Role = "user", Content = "Hello" },
            new() { Role = "assistant", Content = "Hi!" },
            new() { Role = "user", Content = "How are you?" }
        };
        
        var usage = new UsageDetails
        {
            InputTokenCount = 30,
            OutputTokenCount = 5,
            TotalTokenCount = 35
        };
        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Great!")])
        {
            Usage = usage
        };

        _mockChatClient.Setup(c => c.GetResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _chatService.ProcessMessagesWithHistory(messages);

        // Assert - Verify conversation metrics logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token usage metrics") && 
                                           v.ToString()!.Contains("ConversationLength: 3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}