using System.ComponentModel.DataAnnotations;

namespace BuildWithAspire.ApiService.UnitTests.Models;

public class ModelValidationTests
{
    [Fact]
    public void ChatMessageRequest_WithValidData_ShouldBeValid()
    {
        // Arrange
        var request = new ChatMessageRequest
        {
            Role = "user",
            Content = "Hello, world!"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void Conversation_WithValidData_ShouldInitializeProperly()
    {
        // Arrange & Act
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, conversation.Id);
        Assert.Equal("Test Conversation", conversation.Name);
        Assert.NotNull(conversation.Messages);
        Assert.Empty(conversation.Messages);
        Assert.True(conversation.CreatedAt > DateTime.MinValue);
        Assert.True(conversation.UpdatedAt > DateTime.MinValue);
    }

    [Fact]
    public void Message_WithValidData_ShouldInitializeProperly()
    {
        // Arrange & Act
        var conversationId = Guid.NewGuid();
        var message = new Message
        {
            Id = Guid.NewGuid(),
            Role = "user",
            Content = "Hello",
            CreatedAt = DateTime.UtcNow,
            ConversationId = conversationId
        };

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal("user", message.Role);
        Assert.Equal("Hello", message.Content);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.True(message.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public void Conversation_Messages_ShouldSupportNavigation()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            Name = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message1 = new Message
        {
            Id = Guid.NewGuid(),
            Role = "user",
            Content = "Hello",
            ConversationId = conversationId,
            Conversation = conversation,
            CreatedAt = DateTime.UtcNow
        };

        var message2 = new Message
        {
            Id = Guid.NewGuid(),
            Role = "assistant",
            Content = "Hi there!",
            ConversationId = conversationId,
            Conversation = conversation,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        conversation.Messages.Add(message1);
        conversation.Messages.Add(message2);

        // Assert
        Assert.Equal(2, conversation.Messages.Count);
        Assert.Equal(conversation, message1.Conversation);
        Assert.Equal(conversation, message2.Conversation);
        Assert.Contains(message1, conversation.Messages);
        Assert.Contains(message2, conversation.Messages);
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}
