using System.Text.Json;
using BuildWithAspire.Web.Models;

namespace BuildWithAspire.Web.UnitTests.Models;

public class ConversationModelsTests
{
    [Fact]
    public void ConversationSummary_ShouldInitializeProperly()
    {
        // Arrange & Act
        var summary = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            MessageCount = 5
        };

        // Assert
        Assert.NotEqual(Guid.Empty, summary.Id);
        Assert.Equal("Test Conversation", summary.Name);
        Assert.True(summary.CreatedAt > DateTime.MinValue);
        Assert.True(summary.UpdatedAt > DateTime.MinValue);
        Assert.Equal(5, summary.MessageCount);
    }

    [Fact]
    public void ConversationDetail_ShouldInitializeProperly()
    {
        // Arrange & Act
        var detail = new ConversationDetail
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, detail.Id);
        Assert.Equal("Test Conversation", detail.Name);
        Assert.True(detail.CreatedAt > DateTime.MinValue);
        Assert.True(detail.UpdatedAt > DateTime.MinValue);
        Assert.NotNull(detail.Messages);
        Assert.Empty(detail.Messages);
    }

    [Fact]
    public void Message_ShouldInitializeProperly()
    {
        // Arrange & Act
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Role = "user",
            Content = "Hello, world!",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.NotEqual(Guid.Empty, message.ConversationId);
        Assert.Equal("user", message.Role);
        Assert.Equal("Hello, world!", message.Content);
        Assert.True(message.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public void MessageResponse_ShouldInitializeProperly()
    {
        // Arrange & Act
        var response = new MessageResponse
        {
            Response = "Hello there!"
        };

        // Assert
        Assert.Equal("Hello there!", response.Response);
    }

    [Fact]
    public void ConversationSummary_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var originalSummary = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 12, 30, 0, DateTimeKind.Utc),
            MessageCount = 3
        };

        // Act
        var json = JsonSerializer.Serialize(originalSummary);
        var deserializedSummary = JsonSerializer.Deserialize<ConversationSummary>(json);

        // Assert
        Assert.NotNull(deserializedSummary);
        Assert.Equal(originalSummary.Id, deserializedSummary.Id);
        Assert.Equal(originalSummary.Name, deserializedSummary.Name);
        Assert.Equal(originalSummary.UpdatedAt, deserializedSummary.UpdatedAt);
        Assert.Equal(originalSummary.MessageCount, deserializedSummary.MessageCount);
        Assert.Equal(originalSummary.CreatedAt, deserializedSummary.CreatedAt);
    }

    [Fact]
    public void ConversationDetail_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var originalDetail = new ConversationDetail
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 12, 30, 0, DateTimeKind.Utc),
            Messages = new List<Message>
            {
                new() { Id = Guid.NewGuid(), ConversationId = Guid.NewGuid(), Role = "user", Content = "Hello", CreatedAt = new DateTime(2024, 1, 1, 12, 1, 0, DateTimeKind.Utc) },
                new() { Id = Guid.NewGuid(), ConversationId = Guid.NewGuid(), Role = "assistant", Content = "Hi there!", CreatedAt = new DateTime(2024, 1, 1, 12, 2, 0, DateTimeKind.Utc) }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(originalDetail);
        var deserializedDetail = JsonSerializer.Deserialize<ConversationDetail>(json);

        // Assert
        Assert.NotNull(deserializedDetail);
        Assert.Equal(originalDetail.Id, deserializedDetail.Id);
        Assert.Equal(originalDetail.Name, deserializedDetail.Name);
        Assert.Equal(originalDetail.UpdatedAt, deserializedDetail.UpdatedAt);
        Assert.Equal(originalDetail.CreatedAt, deserializedDetail.CreatedAt);
        Assert.Equal(2, deserializedDetail.Messages.Count);
        Assert.Equal("Hello", deserializedDetail.Messages[0].Content);
        Assert.Equal("Hi there!", deserializedDetail.Messages[1].Content);
    }

    [Fact]
    public void ConversationDetail_WithMessages_ShouldMaintainOrder()
    {
        // Arrange
        var detail = new ConversationDetail
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message1 = new Message { Id = Guid.NewGuid(), ConversationId = detail.Id, Role = "user", Content = "First", CreatedAt = DateTime.UtcNow };
        var message2 = new Message { Id = Guid.NewGuid(), ConversationId = detail.Id, Role = "assistant", Content = "Second", CreatedAt = DateTime.UtcNow.AddMinutes(1) };
        var message3 = new Message { Id = Guid.NewGuid(), ConversationId = detail.Id, Role = "user", Content = "Third", CreatedAt = DateTime.UtcNow.AddMinutes(2) };

        // Act
        detail.Messages.Add(message1);
        detail.Messages.Add(message2);
        detail.Messages.Add(message3);

        // Assert
        Assert.Equal(3, detail.Messages.Count);
        Assert.Equal("First", detail.Messages[0].Content);
        Assert.Equal("Second", detail.Messages[1].Content);
        Assert.Equal("Third", detail.Messages[2].Content);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("assistant")]
    [InlineData("system")]
    public void Message_WithDifferentRoles_ShouldAcceptValidRoles(string role)
    {
        // Arrange & Act
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Role = role,
            Content = "Test content",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(role, message.Role);
    }

    [Fact]
    public void MessageResponse_JsonSerialization_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var originalResponse = new MessageResponse
        {
            Response = "Hello! Here are some special characters: \"quotes\", 'apostrophes', \n newlines, and emoji 🎉"
        };

        // Act
        var json = JsonSerializer.Serialize(originalResponse);
        var deserializedResponse = JsonSerializer.Deserialize<MessageResponse>(json);

        // Assert
        Assert.NotNull(deserializedResponse);
        Assert.Equal(originalResponse.Response, deserializedResponse.Response);
        Assert.Contains("\"quotes\"", deserializedResponse.Response);
        Assert.Contains("🎉", deserializedResponse.Response);
    }

    [Fact]
    public void ConversationDetail_EmptyMessages_ShouldHandleGracefully()
    {
        // Arrange & Act
        var detail = new ConversationDetail
        {
            Id = Guid.NewGuid(),
            Name = "Empty Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages = new List<Message>()
        };

        // Assert
        Assert.NotNull(detail.Messages);
        Assert.Empty(detail.Messages);
        Assert.Equal("Empty Conversation", detail.Name);
    }
}
