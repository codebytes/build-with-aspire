using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BuildWithAspire.PlaywrightTests;

public class WeatherUITests : BasePlaywrightTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await NavigateToPageAsync("/weather");
    }

    [Fact]
    public async Task WeatherPage_ShouldLoadSuccessfully()
    {
        // Assert
        var url = Page.Url;
        Assert.Contains("weather", url);
        await Expect(Page.Locator("h1")).ToContainTextAsync("Weather");
    }

    [Fact]
    public async Task WeatherPage_ShouldDisplayWeatherData()
    {
        // Wait for weather data to load
        await Page.WaitForTimeoutAsync(3000);
        
        // Look for weather-related content (flexible selectors)
        var weatherContent = Page.Locator("table")
            .Or(Page.Locator("[class*='weather']"))
            .Or(Page.Locator("[class*='forecast']"))
            .Or(Page.GetByText("°"))
            .Or(Page.GetByText("Temperature"))
            .Or(Page.GetByText("Summary"));
        
        await Expect(weatherContent.First).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task WeatherPage_ShouldDisplayCorrectTitle()
    {
        await Expect(Page).ToHaveTitleAsync("Weather");
    }

    [Fact]
    public async Task WeatherPage_ShouldHaveMultipleDataRows()
    {
        // Wait for data to load
        await Page.WaitForTimeoutAsync(3000);
        
        // Look for multiple rows of data (typical for weather forecasts)
        var rows = Page.Locator("tr")
            .Or(Page.Locator("[class*='weather-item']"))
            .Or(Page.Locator("[class*='forecast-item']"));
        
        var rowCount = await rows.CountAsync();
        Assert.True(rowCount > 1, "Should have multiple weather data entries");
    }

    [Fact]
    public async Task WeatherPage_ShouldBeAccessible()
    {
        // Wait for content to load
        await Page.WaitForTimeoutAsync(3000);
        
        // Check for proper heading structure
        var headings = Page.Locator("h1, h2, h3, h4, h5, h6");
        var headingCount = await headings.CountAsync();
        Assert.True(headingCount > 0, "Should have at least one heading");
        
        // If tables exist, they should have headers
        var tables = Page.Locator("table");
        var tableCount = await tables.CountAsync();
        
        if (tableCount > 0)
        {
            var tableHeaders = Page.Locator("th");
            var headerCount = await tableHeaders.CountAsync();
            Assert.True(headerCount > 0, "Tables should have header cells");
        }
    }
}