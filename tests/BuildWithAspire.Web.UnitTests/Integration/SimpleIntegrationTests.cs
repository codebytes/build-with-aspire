using BuildWithAspire.Web.Models;

namespace BuildWithAspire.Web.UnitTests.Integration;

public class SimpleIntegrationTests
{
    [Fact]
    public void ConversationSummary_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var conversation = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            MessageCount = 5
        };

        // Assert
        Assert.NotEqual(Guid.Empty, conversation.Id);
        Assert.Equal("Test Conversation", conversation.Name);
        Assert.Equal(5, conversation.MessageCount);
    }

    [Fact]
    public void Message_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Role = "user",
            Content = "Hello world",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.NotEqual(Guid.Empty, message.ConversationId);
        Assert.Equal("user", message.Role);
        Assert.Equal("Hello world", message.Content);
    }

    [Fact]
    public void ConversationDetail_WithMessages_WorksCorrectly()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<Message>
        {
            new() { Id = Guid.NewGuid(), ConversationId = conversationId, Role = "user", Content = "Hi", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ConversationId = conversationId, Role = "assistant", Content = "Hello", CreatedAt = DateTime.UtcNow }
        };

        // Act
        var conversation = new ConversationDetail
        {
            Id = conversationId,
            Name = "Test Chat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages = messages
        };

        // Assert
        Assert.Equal(conversationId, conversation.Id);
        Assert.Equal("Test Chat", conversation.Name);
        Assert.Equal(2, conversation.Messages.Count);
        Assert.Contains(conversation.Messages, m => m.Role == "user");
        Assert.Contains(conversation.Messages, m => m.Role == "assistant");
    }

    [Fact]
    public void MessageResponse_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var response = new MessageResponse
        {
            Response = "AI generated response"
        };

        // Assert
        Assert.Equal("AI generated response", response.Response);
    }
}
