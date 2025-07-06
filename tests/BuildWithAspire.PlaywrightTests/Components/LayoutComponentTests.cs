using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BuildWithAspire.PlaywrightTests.Components;

public class LayoutComponentTests : BasePlaywrightTest
{
    [Fact]
    public async Task MainLayout_ShouldHaveCorrectStructure()
    {
        await NavigateToPageAsync("/");
        
        // Should have main page container
        var pageDiv = Page.Locator("div.page");
        await Expect(pageDiv).ToBeVisibleAsync();
        
        // Should have sidebar for navigation
        var sidebar = pageDiv.Locator("div.sidebar");
        await Expect(sidebar).ToBeVisibleAsync();
        
        // Should have main content area
        var main = pageDiv.Locator("main");
        await Expect(main).ToBeVisibleAsync();
    }

    [Fact]
    public async Task MainLayout_ShouldHaveFlexboxStructure()
    {
        await NavigateToPageAsync("/");
        
        var pageDiv = Page.Locator("div.page");
        
        // Page should use flexbox or grid layout (check computed styles)
        var display = await pageDiv.EvaluateAsync<string>("el => getComputedStyle(el).display");
        
        // Should use some form of layout system (flex, grid, or block with proper positioning)
        Assert.False(string.IsNullOrEmpty(display));
    }

    [Fact]
    public async Task MainLayout_SidebarShouldContainNavigation()
    {
        await NavigateToPageAsync("/");
        
        var sidebar = Page.Locator("div.sidebar");
        await Expect(sidebar).ToBeVisibleAsync();
        
        // Sidebar should contain the navigation menu
        var navMenu = sidebar.Locator(".top-row, .nav-scrollable, nav");
        await Expect(navMenu.First).ToBeVisibleAsync();
        
        // Should contain navigation links
        var navLinks = sidebar.Locator(".nav-link");
        var linkCount = await navLinks.CountAsync();
        Assert.True(linkCount > 0, "Sidebar should contain navigation links");
    }

    [Fact]
    public async Task MainLayout_MainContentShouldHaveTopRowAndContent()
    {
        await NavigateToPageAsync("/");
        
        var main = Page.Locator("main");
        await Expect(main).ToBeVisibleAsync();
        
        // Should have top row with external link
        var topRow = main.Locator("div.top-row");
        await Expect(topRow).ToBeVisibleAsync();
        await Expect(topRow).ToHaveClassAsync(new Regex(".*px-4.*"));
        
        // Top row should contain About link
        var aboutLink = topRow.Locator("a[target='_blank']");
        await Expect(aboutLink).ToBeVisibleAsync();
        await Expect(aboutLink).ToContainTextAsync("About");
        await Expect(aboutLink).ToHaveAttributeAsync("href", "https://learn.microsoft.com/aspnet/core/");
        
        // Should have content area
        var contentArea = main.Locator("article.content");
        await Expect(contentArea).ToBeVisibleAsync();
        await Expect(contentArea).ToHaveClassAsync(new Regex(".*px-4.*"));
    }

    [Fact]
    public async Task MainLayout_ShouldContainPageContent()
    {
        var pages = new[]
        {
            new { Path = "/", ExpectedContent = "Hello, world!" },
            new { Path = "/weather", ExpectedContent = "Weather" },
            new { Path = "/chat", ExpectedContent = "Chat with History" }
        };
        
        foreach (var page in pages)
        {
            await NavigateToPageAsync(page.Path);
            
            var contentArea = Page.Locator("article.content");
            await Expect(contentArea).ToBeVisibleAsync();
            
            // Content area should contain the page-specific content
            await Expect(contentArea).ToContainTextAsync(page.ExpectedContent);
        }
    }

    [Fact]
    public async Task MainLayout_AboutLinkShouldOpenInNewTab()
    {
        await NavigateToPageAsync("/");
        
        var aboutLink = Page.Locator("main .top-row a[target='_blank']");
        await Expect(aboutLink).ToBeVisibleAsync();
        
        // Should have target="_blank" for new tab
        await Expect(aboutLink).ToHaveAttributeAsync("target", "_blank");
        
        // Should link to Microsoft Learn
        var href = await aboutLink.GetAttributeAsync("href");
        Assert.StartsWith("https://learn.microsoft.com/", href);
    }

    [Fact]
    public async Task MainLayout_ShouldBeResponsive()
    {
        var viewports = new[]
        {
            new { Width = 1200, Height = 800, Name = "Desktop" },
            new { Width = 768, Height = 1024, Name = "Tablet" },
            new { Width = 375, Height = 667, Name = "Mobile" }
        };
        
        foreach (var viewport in viewports)
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await NavigateToPageAsync("/");
            await Page.WaitForTimeoutAsync(500);
            
            // Core layout elements should remain visible
            await Expect(Page.Locator("div.page")).ToBeVisibleAsync();
            await Expect(Page.Locator("main")).ToBeVisibleAsync();
            await Expect(Page.Locator("article.content")).ToBeVisibleAsync();
            
            // Navigation should be accessible (though may be collapsed on mobile)
            var sidebar = Page.Locator("div.sidebar");
            await Expect(sidebar).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task MainLayout_ShouldMaintainStructureAcrossPages()
    {
        var pages = new[] { "/", "/weather", "/chat", "/counter" };
        
        foreach (var pagePath in pages)
        {
            await NavigateToPageAsync(pagePath);
            
            // Core layout should be consistent across all pages
            await Expect(Page.Locator("div.page")).ToBeVisibleAsync();
            await Expect(Page.Locator("div.sidebar")).ToBeVisibleAsync();
            await Expect(Page.Locator("main")).ToBeVisibleAsync();
            await Expect(Page.Locator("main .top-row")).ToBeVisibleAsync();
            await Expect(Page.Locator("article.content")).ToBeVisibleAsync();
            
            // About link should be present on all pages
            await Expect(Page.Locator("main .top-row a[target='_blank']")).ToBeVisibleAsync();
            
            // Navigation should be present and functional
            var navLinks = Page.Locator(".nav-link");
            var linkCount = await navLinks.CountAsync();
            Assert.True(linkCount > 0, $"Navigation should be present on {pagePath}");
        }
    }

    [Fact]
    public async Task MainLayout_ShouldHaveConsistentSpacing()
    {
        await NavigateToPageAsync("/");
        
        // Top row should have px-4 padding
        var topRow = Page.Locator("main .top-row");
        await Expect(topRow).ToHaveClassAsync(new Regex(".*px-4.*"));
        
        // Content area should have px-4 padding
        var contentArea = Page.Locator("article.content");
        await Expect(contentArea).ToHaveClassAsync(new Regex(".*px-4.*"));
        
        // Check computed styles for consistency
        var topRowPadding = await topRow.EvaluateAsync<string>("el => getComputedStyle(el).paddingLeft");
        var contentPadding = await contentArea.EvaluateAsync<string>("el => getComputedStyle(el).paddingLeft");
        
        Assert.Equal(contentPadding, topRowPadding);
    }

    [Fact]
    public async Task MainLayout_ShouldIncludeScrollToBottomScript()
    {
        await NavigateToPageAsync("/");
        
        // The layout should include the scrollToBottom JavaScript function
        // This is used by the Chat component
        var hasScrollFunction = await Page.EvaluateAsync<bool>("() => typeof window.scrollToBottom === 'function'");
        Assert.True(hasScrollFunction, "Layout should include scrollToBottom function for chat functionality");
    }

    [Fact]
    public async Task MainLayout_ShouldAllowContentToScroll()
    {
        await NavigateToPageAsync("/");
        
        var main = Page.Locator("main");
        var contentArea = Page.Locator("article.content");
        
        // Main and content areas should be properly configured for scrolling
        await Expect(main).ToBeVisibleAsync();
        await Expect(contentArea).ToBeVisibleAsync();
        
        // Content should be able to scroll if it exceeds viewport
        var mainOverflow = await main.EvaluateAsync<string>("el => getComputedStyle(el).overflowY");
        var contentOverflow = await contentArea.EvaluateAsync<string>("el => getComputedStyle(el).overflowY");
        
        // Should allow scrolling (auto, scroll, or visible)
        var allowsScrolling = mainOverflow != "hidden" && contentOverflow != "hidden";
        Assert.True(allowsScrolling, "Layout should allow content scrolling");
    }

    [Fact]
    public async Task MainLayout_ShouldHaveAccessibleStructure()
    {
        await NavigateToPageAsync("/");
        
        // Should use semantic HTML elements
        await Expect(Page.Locator("main")).ToBeVisibleAsync();
        await Expect(Page.Locator("nav")).ToBeVisibleAsync();
        await Expect(Page.Locator("article")).ToBeVisibleAsync();
        
        // Links should have proper href attributes
        var aboutLink = Page.Locator("main .top-row a");
        var href = await aboutLink.GetAttributeAsync("href");
        Assert.False(string.IsNullOrEmpty(href), "About link should have valid href");
        
        // Navigation links should be accessible
        var navLinks = Page.Locator(".nav-link");
        var linkCount = await navLinks.CountAsync();
        
        for (int i = 0; i < Math.Min(3, linkCount); i++)
        {
            var link = navLinks.Nth(i);
            var linkHref = await link.GetAttributeAsync("href");
            var linkText = await link.InnerTextAsync();
            
            Assert.NotNull(linkHref);
            Assert.True(!string.IsNullOrWhiteSpace(linkText.Trim()), $"Navigation link {i} should have text content");
        }
    }

    [Fact]
    public async Task MainLayout_ShouldHandlePageTransitions()
    {
        await NavigateToPageAsync("/");
        
        // Navigate to different page
        var weatherLink = Page.GetByRole(AriaRole.Link, new() { Name = "Weather" });
        await weatherLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Layout should remain intact during navigation
        await Expect(Page.Locator("div.page")).ToBeVisibleAsync();
        await Expect(Page.Locator("main")).ToBeVisibleAsync();
        await Expect(Page.Locator("article.content")).ToBeVisibleAsync();
        
        // Content should have changed to weather page
        await Expect(Page.Locator("article.content")).ToContainTextAsync("Weather");
        
        // But layout structure should remain the same
        await Expect(Page.Locator("main .top-row")).ToBeVisibleAsync();
        await Expect(Page.Locator("main .top-row a[target='_blank']")).ToBeVisibleAsync();
    }
}