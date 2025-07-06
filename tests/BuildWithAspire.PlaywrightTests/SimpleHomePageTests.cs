using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BuildWithAspire.PlaywrightTests;

public class SimpleHomePageTests : BasePlaywrightTest
{
    [Fact]
    public async Task HomePage_ShouldLoadSuccessfully()
    {
        // Act - Navigation happens in SetUp

        // Assert
        await Expect(Page).ToHaveTitleAsync("Home");
        
        // Verify main navigation elements are present
        await Expect(Page.Locator("nav")).ToBeVisibleAsync();
        
        // Verify the main content area is present
        await Expect(Page.Locator("main")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_ShouldHaveNavigationLinks()
    {
        // Assert
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Home" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Chat" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Weather" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Navigation_ShouldWorkBetweenPages()
    {
        // Navigate to Chat page
        await Page.GetByRole(AriaRole.Link, new() { Name = "Chat" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify we're on the chat page (basic URL check)
        var url = Page.Url;
        Assert.Contains("chat", url);
        
        // Navigate to Weather page
        await Page.GetByRole(AriaRole.Link, new() { Name = "Weather" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify we're on the weather page
        url = Page.Url;
        Assert.Contains("weather", url);
        
        // Navigate back to Home
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify we're back on the home page
        url = Page.Url;
        var isHomePage = url.EndsWith("/") || (!url.Contains("chat") && !url.Contains("weather"));
        Assert.True(isHomePage);
    }

    [Fact]
    public async Task HomePage_ShouldBeResponsive()
    {
        // Test desktop view
        await Page.SetViewportSizeAsync(1200, 800);
        await Expect(Page.Locator("div.page")).ToBeVisibleAsync();

        // Test tablet view
        await Page.SetViewportSizeAsync(768, 1024);
        await Expect(Page.Locator("div.page")).ToBeVisibleAsync();

        // Test mobile view
        await Page.SetViewportSizeAsync(375, 667);
        await Expect(Page.Locator("div.page")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_ShouldLoadWithoutErrors()
    {
        var errors = new List<string>();
        
        // Listen for console errors
        Page.Console += (_, e) =>
        {
            if (e.Type == "error")
            {
                errors.Add(e.Text);
            }
        };

        // Navigate to home page
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert no console errors
        Assert.Empty(errors);
    }
}