using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BuildWithAspire.PlaywrightTests;

public class ChatUITests : BasePlaywrightTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await NavigateToPageAsync("/chat");
    }

    [Fact]
    public async Task ChatPage_ShouldLoadSuccessfully()
    {
        // Assert
        var url = Page.Url;
        Assert.Contains("chat", url);
        await Expect(Page.Locator("h1")).ToContainTextAsync("Chat");
    }

    [Fact]
    public async Task ChatPage_ShouldHaveBasicElements()
    {
        // Check for common chat UI elements
        
        // Should have some kind of conversations list or area
        var conversationsArea = Page.Locator("[class*='conversation']")
            .Or(Page.Locator("[class*='sidebar']"))
            .Or(Page.Locator("aside"))
            .Or(Page.Locator("nav"));
        
        await Expect(conversationsArea.First).ToBeVisibleAsync();
        
        // Should have message input area
        var messageArea = Page.Locator("input")
            .Or(Page.Locator("textarea"))
            .Or(Page.Locator("[class*='message']"))
            .Or(Page.Locator("[class*='chat']"));
        
        await Expect(messageArea.First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ChatPage_ShouldHaveInteractiveElements()
    {
        // Should have buttons (send, new conversation, etc.)
        var buttons = Page.Locator("button");
        var buttonCount = await buttons.CountAsync();
        Assert.True(buttonCount > 0, "Should have at least one button");
        
        // Should have inputs
        var inputs = Page.Locator("input, textarea");
        var inputCount = await inputs.CountAsync();
        Assert.True(inputCount > 0, "Should have at least one input element");
    }

    [Fact]
    public async Task ChatPage_ShouldDisplayCorrectTitle()
    {
        await Expect(Page).ToHaveTitleAsync("Chat with History");
    }

    [Fact]
    public async Task ChatPage_ShouldLoadWithoutJavaScriptErrors()
    {
        var errors = new List<string>();
        
        Page.Console += (_, e) =>
        {
            if (e.Type == "error")
            {
                errors.Add(e.Text);
            }
        };

        // Reload to catch any initialization errors
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(2000);

        // Filter out common non-critical errors
        var criticalErrors = errors.Where(e => 
            !e.Contains("favicon") && 
            !e.Contains("_framework") &&
            !e.Contains("net::ERR_")).ToList();

        Assert.Empty(criticalErrors);
    }
}