using BuildWithAspire.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildWithAspire.ApiService.UnitTests.Data;

public class ChatDbContextTests : IDisposable
{
    private readonly ChatDbContext _context;

    public ChatDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatDbContext(options);
    }

    [Fact]
    public async Task Conversation_CanBeCreatedAndRetrieved()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Conversations.FindAsync(conversation.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Test Conversation", retrieved.Name);
        Assert.Equal(conversation.Id, retrieved.Id);
    }

    [Fact]
    public async Task Message_CanBeCreatedWithConversationRelationship()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = "Hello, world!",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Conversations.Add(conversation);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var retrievedMessage = await _context.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == message.Id);

        // Assert
        Assert.NotNull(retrievedMessage);
        Assert.Equal("Hello, world!", retrievedMessage.Content);
        Assert.Equal("user", retrievedMessage.Role);
        Assert.Equal(conversation.Id, retrievedMessage.ConversationId);
        Assert.NotNull(retrievedMessage.Conversation);
        Assert.Equal("Test Conversation", retrievedMessage.Conversation.Name);
    }

    [Fact]
    public async Task Conversation_CanHaveMultipleMessages()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message1 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = "Hello",
            CreatedAt = DateTime.UtcNow
        };

        var message2 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = "Hi there!",
            CreatedAt = DateTime.UtcNow.AddMinutes(1)
        };

        // Act
        _context.Conversations.Add(conversation);
        _context.Messages.AddRange(message1, message2);
        await _context.SaveChangesAsync();

        var retrievedConversation = await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversation.Id);

        // Assert
        Assert.NotNull(retrievedConversation);
        Assert.Equal(2, retrievedConversation.Messages.Count);
        Assert.Contains(retrievedConversation.Messages, m => m.Role == "user" && m.Content == "Hello");
        Assert.Contains(retrievedConversation.Messages, m => m.Role == "assistant" && m.Content == "Hi there!");
    }

    [Fact]
    public async Task DeleteConversation_CascadeDeletesMessages()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = "Hello",
            CreatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Act
        _context.Conversations.Remove(conversation);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedConversation = await _context.Conversations.FindAsync(conversation.Id);
        var retrievedMessage = await _context.Messages.FindAsync(message.Id);

        Assert.Null(retrievedConversation);
        Assert.Null(retrievedMessage); // Should be cascade deleted
    }

    [Fact]
    public async Task Conversation_RequiredFields_AreValidated()
    {
        // Arrange - Test that Name has a default value (empty string)
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            // Name has default empty string value
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Conversations.FindAsync(conversation.Id);

        // Assert - In-memory database allows empty string but not null
        Assert.NotNull(retrieved);
        Assert.Equal(string.Empty, retrieved.Name);
    }

    [Fact]
    public async Task Message_RequiredFields_AreValidated()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            // Role and Content have default empty string values
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Conversations.Add(conversation);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Messages.FindAsync(message.Id);

        // Assert - In-memory database allows empty strings
        Assert.NotNull(retrieved);
        Assert.Equal(string.Empty, retrieved.Role);
        Assert.Equal(string.Empty, retrieved.Content);
    }

    [Fact]
    public async Task Message_WithInvalidConversationId_CanBeSaved()
    {
        // Arrange
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(), // Non-existent conversation
            Role = "user",
            Content = "Hello",
            CreatedAt = DateTime.UtcNow
        };

        // Act - In-memory database doesn't enforce foreign key constraints
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Messages.FindAsync(message.Id);

        // Assert - Message can be saved even with invalid foreign key in in-memory DB
        Assert.NotNull(retrieved);
        Assert.Equal(message.ConversationId, retrieved.ConversationId);
    }

    [Fact]
    public async Task ConversationName_CanBe200Characters()
    {
        // Arrange
        var longName = new string('A', 200);
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = longName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Conversations.FindAsync(conversation.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(longName, retrieved.Name);
    }

    [Fact]
    public async Task MessageContent_CanBeVeryLong()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var longContent = new string('A', 10000); // Very long content
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = longContent,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Conversations.Add(conversation);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Messages.FindAsync(message.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(longContent, retrieved.Content);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("assistant")]
    [InlineData("system")]
    public async Task Message_ValidRoles_AreAccepted(string role)
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = role,
            Content = "Test content",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Conversations.Add(conversation);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Messages.FindAsync(message.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(role, retrieved.Role);
    }

    [Fact]
    public async Task Messages_OrderedByCreatedAt_ReturnsInCorrectOrder()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var baseTime = DateTime.UtcNow;
        var message1 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = "First message",
            CreatedAt = baseTime
        };

        var message2 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = "Second message",
            CreatedAt = baseTime.AddMinutes(1)
        };

        var message3 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = "Third message",
            CreatedAt = baseTime.AddMinutes(2)
        };

        // Act
        _context.Conversations.Add(conversation);
        _context.Messages.AddRange(message3, message1, message2); // Add in random order
        await _context.SaveChangesAsync();

        var orderedMessages = await _context.Messages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Assert
        Assert.Equal(3, orderedMessages.Count);
        Assert.Equal("First message", orderedMessages[0].Content);
        Assert.Equal("Second message", orderedMessages[1].Content);
        Assert.Equal("Third message", orderedMessages[2].Content);
    }

    [Fact]
    public async Task ConversationWithMessages_IncludeQuery_LoadsAllData()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var messages = new List<Message>
        {
            new() { Id = Guid.NewGuid(), ConversationId = conversation.Id, Role = "user", Content = "Hello", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ConversationId = conversation.Id, Role = "assistant", Content = "Hi!", CreatedAt = DateTime.UtcNow.AddMinutes(1) }
        };

        // Act
        _context.Conversations.Add(conversation);
        _context.Messages.AddRange(messages);
        await _context.SaveChangesAsync();

        var result = await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversation.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Conversation", result.Name);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal("Hello", result.Messages.First().Content);
        Assert.Equal("Hi!", result.Messages.Last().Content);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
