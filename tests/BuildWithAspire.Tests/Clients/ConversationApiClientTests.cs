using BuildWithAspire.Web.Clients;
using BuildWithAspire.Web.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BuildWithAspire.Tests.Clients;

public class ConversationApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ConversationApiClient>> _loggerMock;
    private readonly ConversationApiClient _client;

    public ConversationApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://test.example.com/")
        };
        _loggerMock = new Mock<ILogger<ConversationApiClient>>();
        _client = new ConversationApiClient(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetConversationsAsync_ReturnsConversations()
    {
        // Arrange
        var expectedConversations = new List<ConversationSummary>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Conversation 1", MessageCount = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Test Conversation 2", MessageCount = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var jsonResponse = JsonSerializer.Serialize(expectedConversations);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.ToString().EndsWith("conversations")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetConversationsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedConversations[0].Name, result[0].Name);
        Assert.Equal(expectedConversations[1].Name, result[1].Name);
    }

    [Fact]
    public async Task GetConversationsAsync_WhenHttpRequestFails_LogsErrorAndThrows()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetConversationsAsync());

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to fetch conversations")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConversationAsync_WithValidId_ReturnsConversation()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var expectedConversation = new ConversationDetail
        {
            Id = conversationId,
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages = new List<Message>
            {
                new() { Id = Guid.NewGuid(), Role = "user", Content = "Hello", CreatedAt = DateTime.UtcNow, ConversationId = conversationId }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(expectedConversation);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.ToString().Contains(conversationId.ToString())),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetConversationAsync(conversationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConversation.Id, result.Id);
        Assert.Equal(expectedConversation.Name, result.Name);
        Assert.Single(result.Messages);
    }

    [Fact]
    public async Task GetConversationAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.ToString().Contains(conversationId.ToString())),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetConversationAsync(conversationId);

        // Assert
        Assert.Null(result);

        // Verify warning logging for not found
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Conversation not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateConversationAsync_WithValidName_ReturnsCreatedConversation()
    {
        // Arrange
        var conversationName = "New Test Conversation";
        var expectedConversation = new ConversationDetail
        {
            Id = Guid.NewGuid(),
            Name = conversationName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages = new List<Message>()
        };

        var jsonResponse = JsonSerializer.Serialize(expectedConversation);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri!.ToString().EndsWith("conversations")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.CreateConversationAsync(conversationName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConversation.Id, result.Id);
        Assert.Equal(conversationName, result.Name);

        // Verify information logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating new conversation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithValidData_ReturnsResponse()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var message = "Hello, how are you?";
        var expectedResponse = new MessageResponse { Response = "I'm doing well, thank you!" };

        var jsonResponse = JsonSerializer.Serialize(expectedResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri!.ToString().Contains($"conversations/{conversationId}/messages")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.SendMessageAsync(conversationId, message);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Response, result.Response);

        // Verify information logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending message to conversation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteConversationAsync_WithValidId_DeletesSuccessfully()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Delete && 
                    req.RequestUri!.ToString().Contains(conversationId.ToString())),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        await _client.DeleteConversationAsync(conversationId);

        // Assert - No exception thrown means success
        // Verify information logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully deleted conversation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteConversationAsync_WhenHttpRequestFails_LogsErrorAndThrows()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Delete failed"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _client.DeleteConversationAsync(conversationId));

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to delete conversation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}