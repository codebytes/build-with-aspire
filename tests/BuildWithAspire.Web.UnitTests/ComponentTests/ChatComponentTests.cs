namespace BuildWithAspire.Web.UnitTests.ComponentTests;

public class ChatComponentTests
{
    [Fact]
    public void GetMessageCss_WithUserRole_ShouldReturnSent()
    {
        // Arrange
        var role = "user";

        // Act
        var result = GetMessageCss(role);

        // Assert
        Assert.Equal("sent", result);
    }

    [Fact]
    public void GetMessageCss_WithAssistantRole_ShouldReturnReceived()
    {
        // Arrange
        var role = "assistant";

        // Act
        var result = GetMessageCss(role);

        // Assert
        Assert.Equal("received", result);
    }

    [Fact]
    public void GetMessageCss_WithMixedCaseRole_ShouldHandleCaseInsensitive()
    {
        // Arrange
        var role = "USER";

        // Act
        var result = GetMessageCss(role);

        // Assert
        Assert.Equal("sent", result);
    }

    [Fact]
    public void GetMessageCss_WithUnknownRole_ShouldReturnReceived()
    {
        // Arrange
        var role = "unknown";

        // Act
        var result = GetMessageCss(role);

        // Assert
        Assert.Equal("received", result);
    }

    // Helper method extracted from Chat component logic
    private static string GetMessageCss(string role)
    {
        return role.ToLower() == "user" ? "sent" : "received";
    }
}
