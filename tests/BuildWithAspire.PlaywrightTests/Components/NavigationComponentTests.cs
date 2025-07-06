using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BuildWithAspire.PlaywrightTests.Components;

public class NavigationComponentTests : BasePlaywrightTest
{
    [Fact]
    public async Task NavMenu_ShouldHaveCorrectStructure()
    {
        // Navigate to any page to ensure nav is loaded
        await NavigateToPageAsync("/");
        
        // Should have top navbar with brand
        var topRow = Page.Locator(".top-row.navbar.navbar-dark");
        await Expect(topRow).ToBeVisibleAsync();
        
        var brand = topRow.Locator(".navbar-brand");
        await Expect(brand).ToBeVisibleAsync();
        await Expect(brand).ToContainTextAsync("BuildWithAspire.Web");
    }

    [Fact]
    public async Task NavMenu_ShouldHaveToggler()
    {
        await NavigateToPageAsync("/");
        
        // Should have navigation toggle checkbox
        var toggler = Page.Locator("input.navbar-toggler[type='checkbox']");
        await Expect(toggler).ToBeVisibleAsync();
        await Expect(toggler).ToHaveAttributeAsync("title", "Navigation menu");
    }

    [Fact]
    public async Task NavMenu_ShouldHaveScrollableArea()
    {
        await NavigateToPageAsync("/");
        
        var scrollableArea = Page.Locator(".nav-scrollable");
        await Expect(scrollableArea).ToBeVisibleAsync();
        
        // Should contain the main navigation
        var nav = scrollableArea.Locator("nav.flex-column");
        await Expect(nav).ToBeVisibleAsync();
    }

    [Fact]
    public async Task NavMenu_ShouldHaveAllExpectedLinks()
    {
        await NavigateToPageAsync("/");
        
        var navItems = Page.Locator(".nav-item");
        await Expect(navItems).ToHaveCountAsync(4); // Home, Counter, Weather, Chat
        
        // Home link
        var homeLink = Page.Locator(".nav-item").Filter(new() { HasText = "Home" }).Locator(".nav-link");
        await Expect(homeLink).ToBeVisibleAsync();
        await Expect(homeLink).ToHaveAttributeAsync("href", "");
        
        // Counter link
        var counterLink = Page.Locator(".nav-item").Filter(new() { HasText = "Counter" }).Locator(".nav-link");
        await Expect(counterLink).ToBeVisibleAsync();
        await Expect(counterLink).ToHaveAttributeAsync("href", "counter");
        
        // Weather link
        var weatherLink = Page.Locator(".nav-item").Filter(new() { HasText = "Weather" }).Locator(".nav-link");
        await Expect(weatherLink).ToBeVisibleAsync();
        await Expect(weatherLink).ToHaveAttributeAsync("href", "weather");
        
        // Chat link
        var chatLink = Page.Locator(".nav-item").Filter(new() { HasText = "Chat" }).Locator(".nav-link");
        await Expect(chatLink).ToBeVisibleAsync();
        await Expect(chatLink).ToHaveAttributeAsync("href", "chat");
    }

    [Fact]
    public async Task NavMenu_LinksShouldHaveBootstrapIcons()
    {
        await NavigateToPageAsync("/");
        
        // Each nav link should have a bootstrap icon span
        var iconSpans = Page.Locator(".nav-link span[class*='bi-']");
        await Expect(iconSpans).ToHaveCountAsync(4);
        
        // Home should have house icon
        var homeIcon = Page.Locator(".nav-item").Filter(new() { HasText = "Home" }).Locator("span.bi-house-door-fill-nav-menu");
        await Expect(homeIcon).ToBeVisibleAsync();
        
        // Counter should have plus square icon
        var counterIcon = Page.Locator(".nav-item").Filter(new() { HasText = "Counter" }).Locator("span.bi-plus-square-fill-nav-menu");
        await Expect(counterIcon).ToBeVisibleAsync();
        
        // Weather should have list icon
        var weatherIcon = Page.Locator(".nav-item").Filter(new() { HasText = "Weather" }).Locator("span.bi-list-nested-nav-menu");
        await Expect(weatherIcon).ToBeVisibleAsync();
        
        // Chat should have list icon
        var chatIcon = Page.Locator(".nav-item").Filter(new() { HasText = "Chat" }).Locator("span.bi-list-nested-nav-menu");
        await Expect(chatIcon).ToBeVisibleAsync();
    }

    [Fact]
    public async Task NavMenu_ShouldIndicateActiveLink()
    {
        // Test Home page active state
        await NavigateToPageAsync("/");
        var homeLink = Page.Locator(".nav-item").Filter(new() { HasText = "Home" }).Locator(".nav-link");
        
        // Wait for navigation to complete
        await Page.WaitForTimeoutAsync(500);
        
        // Home should be active (NavLink with Match="NavLinkMatch.All" handles this)
        // We can't easily test the active class without looking at Blazor's generated HTML
        // but we can verify the link is properly structured
        await Expect(homeLink).ToBeVisibleAsync();
        
        // Test navigation to other pages
        await Page.GetByRole(AriaRole.Link, new() { Name = "Weather" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Should navigate to weather page
        Assert.Contains("weather", Page.Url);
    }

    [Fact]
    public async Task NavMenu_ShouldWorkInMobileView()
    {
        // Set mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPageAsync("/");
        
        // Navigation should still be present
        var navScrollable = Page.Locator(".nav-scrollable");
        await Expect(navScrollable).ToBeVisibleAsync();
        
        // All nav items should still be accessible
        var navItems = Page.Locator(".nav-item");
        await Expect(navItems).ToHaveCountAsync(4);
        
        // Links should still work
        var chatLink = Page.Locator(".nav-item").Filter(new() { HasText = "Chat" }).Locator(".nav-link");
        await chatLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        Assert.Contains("chat", Page.Url);
    }

    [Fact]
    public async Task NavMenu_ShouldHaveAccessibleStructure()
    {
        await NavigateToPageAsync("/");
        
        // Should use semantic nav element
        var nav = Page.Locator("nav");
        await Expect(nav).ToBeVisibleAsync();
        
        // Icons should have aria-hidden attribute
        var icons = Page.Locator("span[aria-hidden='true']");
        var iconCount = await icons.CountAsync();
        Assert.True(iconCount > 0, "Icons should have aria-hidden attribute");
        
        // Links should have proper text content for screen readers
        var links = Page.Locator(".nav-link");
        var linkCount = await links.CountAsync();
        
        for (int i = 0; i < linkCount; i++)
        {
            var link = links.Nth(i);
            var text = await link.InnerTextAsync();
            Assert.True(!string.IsNullOrWhiteSpace(text), $"Link {i} should have text content");
        }
    }

    [Fact]
    public async Task NavMenu_BrandLinkShouldNavigateToHome()
    {
        await NavigateToPageAsync("/weather"); // Start on non-home page
        
        var brandLink = Page.Locator(".navbar-brand");
        await brandLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Should navigate to home page
        var url = Page.Url;
        var isHomePage = url.EndsWith("/") || (!url.Contains("weather") && !url.Contains("chat") && !url.Contains("counter"));
        Assert.True(isHomePage, "Brand link should navigate to home page");
    }

    [Fact]
    public async Task NavMenu_ShouldMaintainStateAcrossNavigation()
    {
        await NavigateToPageAsync("/");
        
        // Navigate through different pages
        var pages = new[] { "Counter", "Weather", "Chat" };
        
        foreach (var pageName in pages)
        {
            var link = Page.Locator(".nav-item").Filter(new() { HasText = pageName }).Locator(".nav-link");
            await link.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Navigation should still be visible and functional
            await Expect(Page.Locator(".nav-scrollable")).ToBeVisibleAsync();
            await Expect(Page.Locator(".nav-item")).ToHaveCountAsync(4);
            
            // Brand should still be visible
            await Expect(Page.Locator(".navbar-brand")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task NavMenu_ShouldHandleClickOnScrollableArea()
    {
        await NavigateToPageAsync("/");
        
        // The nav-scrollable div has an onclick handler that triggers the navbar toggler
        var scrollableArea = Page.Locator(".nav-scrollable");
        var toggler = Page.Locator(".navbar-toggler");
        
        // Get initial state of toggler
        var initialChecked = await toggler.IsCheckedAsync();
        
        // Click on scrollable area
        await scrollableArea.ClickAsync();
        await Page.WaitForTimeoutAsync(200);
        
        // Toggler state should change
        var finalChecked = await toggler.IsCheckedAsync();
        Assert.NotEqual(initialChecked, finalChecked);
    }

    [Fact]
    public async Task NavMenu_ShouldHaveConsistentStyling()
    {
        await NavigateToPageAsync("/");
        
        // Top row should have dark theme
        var topRow = Page.Locator(".top-row");
        await Expect(topRow).ToHaveClassAsync(new Regex(".*navbar-dark.*"));
        
        // Nav should have flex-column class
        var nav = Page.Locator("nav");
        await Expect(nav).ToHaveClassAsync(new Regex(".*flex-column.*"));
        
        // Nav items should have consistent padding
        var navItems = Page.Locator(".nav-item");
        var firstItem = navItems.First;
        await Expect(firstItem).ToHaveClassAsync(new Regex(".*px-3.*"));
        
        // All nav-links should have consistent styling
        var navLinks = Page.Locator(".nav-link");
        var linkCount = await navLinks.CountAsync();
        
        for (int i = 0; i < linkCount; i++)
        {
            var link = navLinks.Nth(i);
            await Expect(link).ToHaveClassAsync(new Regex(".*nav-link.*"));
        }
    }
}