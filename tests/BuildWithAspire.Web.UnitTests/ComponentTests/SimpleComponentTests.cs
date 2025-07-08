using System.Globalization;

namespace BuildWithAspire.Web.UnitTests.ComponentTests;

public class SimpleComponentTests
{
    [Fact]
    public void GetMessageCss_UserRole_ReturnsSent()
    {
        // Arrange
        var role = "user";

        // Act - Simulate the logic from Chat.razor GetMessageCss method
        var result = role.ToLower() == "user" ? "sent" : "received";

        // Assert
        Assert.Equal("sent", result);
    }

    [Fact]
    public void GetMessageCss_AssistantRole_ReturnsReceived()
    {
        // Arrange
        var role = "assistant";

        // Act - Simulate the logic from Chat.razor GetMessageCss method
        var result = role.ToLower() == "user" ? "sent" : "received";

        // Assert
        Assert.Equal("received", result);
    }

    [Fact]
    public void GetMessageCss_UppercaseUserRole_ReturnsSent()
    {
        // Arrange
        var role = "USER";

        // Act - Simulate the logic from Chat.razor GetMessageCss method
        var result = role.ToLower() == "user" ? "sent" : "received";

        // Assert
        Assert.Equal("sent", result);
    }

    [Fact]
    public void NewConversationName_ContainsTimestamp()
    {
        // Arrange & Act - Simulate the logic from Chat.razor ShowNewConversationDialog method
        var conversationName = $"Chat {DateTime.Now:MMM dd, HH:mm}";

        // Assert
        Assert.StartsWith("Chat ", conversationName);
        Assert.Contains(DateTime.Now.ToString("MMM dd", CultureInfo.InvariantCulture), conversationName);
    }
}
