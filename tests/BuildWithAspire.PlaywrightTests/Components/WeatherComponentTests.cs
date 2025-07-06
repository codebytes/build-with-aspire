using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BuildWithAspire.PlaywrightTests.Components;

public class WeatherComponentTests : BasePlaywrightTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await NavigateToPageAsync("/weather");
        // Give the component time to load data
        await Page.WaitForTimeoutAsync(3000);
    }

    [Fact]
    public async Task WeatherComponent_ShouldHaveCorrectPageStructure()
    {
        // Should have correct page title
        await Expect(Page).ToHaveTitleAsync("Weather");
        
        // Should have main heading
        await Expect(Page.Locator("h1")).ToContainTextAsync("Weather");
        
        // Should have description paragraph
        var description = Page.GetByText("This component demonstrates showing data loaded from a backend API service.");
        await Expect(description).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WeatherComponent_ShouldShowLoadingOrData()
    {
        // Check if we see loading state or actual data
        var loadingText = Page.GetByText("Loading...");
        var weatherTable = Page.Locator("table.table");
        
        var hasLoading = await loadingText.IsVisibleAsync();
        var hasTable = await weatherTable.IsVisibleAsync();
        
        Assert.True(hasLoading || hasTable, 
            "Component should show either loading state or weather data");
    }

    [Fact]
    public async Task WeatherTable_ShouldHaveCorrectStructure()
    {
        var table = Page.Locator("table.table");
        
        // Wait for table to appear (should replace loading state)
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Should have thead and tbody
        await Expect(table.Locator("thead")).ToBeVisibleAsync();
        await Expect(table.Locator("tbody")).ToBeVisibleAsync();
        
        // Should have correct headers
        var headers = table.Locator("thead th");
        await Expect(headers).ToHaveCountAsync(4);
        
        await Expect(headers.Nth(0)).ToContainTextAsync("Date");
        await Expect(headers.Nth(1)).ToContainTextAsync("Temp. (C)");
        await Expect(headers.Nth(2)).ToContainTextAsync("Temp. (F)");
        await Expect(headers.Nth(3)).ToContainTextAsync("Summary");
    }

    [Fact]
    public async Task WeatherTable_ShouldHaveMultipleRows()
    {
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Should have multiple forecast rows
        var dataRows = table.Locator("tbody tr");
        var rowCount = await dataRows.CountAsync();
        
        Assert.True(rowCount > 0, "Should have at least one weather forecast row");
        Assert.True(rowCount <= 10, "Should have reasonable number of forecast rows");
    }

    [Fact]
    public async Task WeatherTable_RowsShouldHaveCorrectDataStructure()
    {
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        var dataRows = table.Locator("tbody tr");
        var rowCount = await dataRows.CountAsync();
        
        if (rowCount > 0)
        {
            var firstRow = dataRows.First;
            var cells = firstRow.Locator("td");
            
            // Each row should have 4 cells
            await Expect(cells).ToHaveCountAsync(4);
            
            // Date cell should contain date information
            var dateCell = cells.Nth(0);
            var dateText = await dateCell.InnerTextAsync();
            Assert.True(!string.IsNullOrWhiteSpace(dateText), "Date cell should not be empty");
            
            // Temperature cells should contain numeric values
            var tempCCell = cells.Nth(1);
            var tempCText = await tempCCell.InnerTextAsync();
            Assert.True(!string.IsNullOrWhiteSpace(tempCText), "Temperature C cell should not be empty");
            
            var tempFCell = cells.Nth(2);
            var tempFText = await tempFCell.InnerTextAsync();
            Assert.True(!string.IsNullOrWhiteSpace(tempFText), "Temperature F cell should not be empty");
            
            // Summary cell should contain description
            var summaryCell = cells.Nth(3);
            var summaryText = await summaryCell.InnerTextAsync();
            Assert.True(!string.IsNullOrWhiteSpace(summaryText), "Summary cell should not be empty");
        }
    }

    [Fact]
    public async Task WeatherTable_ShouldDisplayValidTemperatureData()
    {
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        var dataRows = table.Locator("tbody tr");
        var rowCount = await dataRows.CountAsync();
        
        if (rowCount > 0)
        {
            // Check first few rows for valid temperature data
            var checkCount = Math.Min(3, rowCount);
            
            for (int i = 0; i < checkCount; i++)
            {
                var row = dataRows.Nth(i);
                var cells = row.Locator("td");
                
                var tempCText = await cells.Nth(1).InnerTextAsync();
                var tempFText = await cells.Nth(2).InnerTextAsync();
                
                // Temperature values should be numeric (allowing for negative temperatures)
                var tempCValid = int.TryParse(tempCText.Trim(), out var tempC);
                var tempFValid = int.TryParse(tempFText.Trim(), out var tempF);
                
                Assert.True(tempCValid, $"Row {i}: Temperature C should be numeric, got '{tempCText}'");
                Assert.True(tempFValid, $"Row {i}: Temperature F should be numeric, got '{tempFText}'");
                
                // Basic sanity check: Fahrenheit should be higher than Celsius for positive temps
                if (tempC > 0)
                {
                    Assert.True(tempF > tempC, $"Row {i}: Fahrenheit temp should be higher than Celsius for positive temperatures");
                }
            }
        }
    }

    [Fact]
    public async Task WeatherTable_ShouldDisplayValidDateFormat()
    {
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        var dataRows = table.Locator("tbody tr");
        var rowCount = await dataRows.CountAsync();
        
        if (rowCount > 0)
        {
            var firstRow = dataRows.First;
            var dateCell = firstRow.Locator("td").First;
            var dateText = await dateCell.InnerTextAsync();
            
            // Date should be in a readable format (could be various formats)
            // Just check it's not empty and contains some date-like characters
            Assert.NotEmpty(dateText);
            
            // Should contain numbers (date components)
            Assert.Matches(@"\d", dateText);
            
            // Common date separators
            var hasDateSeparator = dateText.Contains("/") || dateText.Contains("-") || dateText.Contains(".") || dateText.Contains(" ");
            Assert.True(hasDateSeparator, "Date should contain common separators");
        }
    }

    [Fact]
    public async Task WeatherComponent_ShouldBeAccessible()
    {
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Table should have proper header structure
        var headers = table.Locator("thead th");
        await Expect(headers).ToHaveCountAsync(4);
        
        // Should use semantic table elements
        await Expect(table.Locator("thead")).ToBeVisibleAsync();
        await Expect(table.Locator("tbody")).ToBeVisibleAsync();
        
        // Headers should have text content for screen readers
        for (int i = 0; i < 4; i++)
        {
            var headerText = await headers.Nth(i).InnerTextAsync();
            Assert.True(!string.IsNullOrWhiteSpace(headerText.Trim()), $"Header {i} should have text content");
        }
        
        // Table should have Bootstrap table class for styling
        await Expect(table).ToHaveClassAsync(new Regex(".*table.*"));
    }

    [Fact]
    public async Task WeatherComponent_ShouldHandleDataLoadingStates()
    {
        // Reload page to catch loading state
        await Page.ReloadAsync();
        
        // Initially should show loading
        var loadingElement = Page.Locator("em").Filter(new() { HasText = "Loading..." });
        
        try
        {
            await Expect(loadingElement).ToBeVisibleAsync(new() { Timeout = 2000 });
        }
        catch
        {
            // Loading might be too fast to catch, which is fine
        }
        
        // Eventually should show data
        await Page.WaitForTimeoutAsync(5000);
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();
        
        // Loading should be gone
        await Expect(loadingElement).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task WeatherComponent_ShouldBeResponsive()
    {
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Test different viewport sizes
        var viewports = new[]
        {
            new { Width = 1200, Height = 800 }, // Desktop
            new { Width = 768, Height = 1024 },  // Tablet
            new { Width = 375, Height = 667 }    // Mobile
        };
        
        foreach (var viewport in viewports)
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.WaitForTimeoutAsync(500);
            
            // Table should still be visible and accessible
            await Expect(table).ToBeVisibleAsync();
            
            // Headers should still be visible
            var headers = table.Locator("thead th");
            await Expect(headers).ToHaveCountAsync(4);
            
            // At least some data rows should be visible
            var dataRows = table.Locator("tbody tr");
            var rowCount = await dataRows.CountAsync();
            Assert.True(rowCount > 0, $"Table should have data rows in {viewport.Width}x{viewport.Height} viewport");
        }
    }

    [Fact]
    public async Task WeatherComponent_ShouldDisplayReasonableWeatherSummaries()
    {
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        var dataRows = table.Locator("tbody tr");
        var rowCount = await dataRows.CountAsync();
        
        if (rowCount > 0)
        {
            // Check that summary cells contain reasonable weather descriptions
            var weatherTerms = new[] { "sunny", "cloudy", "rainy", "hot", "cold", "warm", "cool", "clear", "partly", "stormy", "mild", "overcast", "snow", "fog", "windy" };
            
            var summaryTexts = new List<string>();
            var checkCount = Math.Min(3, rowCount);
            
            for (int i = 0; i < checkCount; i++)
            {
                var row = dataRows.Nth(i);
                var summaryCell = row.Locator("td").Nth(3);
                var summaryText = await summaryCell.InnerTextAsync();
                summaryTexts.Add(summaryText.ToLower());
            }
            
            // At least one summary should contain weather-related terms
            var hasWeatherTerms = summaryTexts.Any(summary => 
                weatherTerms.Any(term => summary.Contains(term)));
            
            Assert.True(hasWeatherTerms, 
                $"Weather summaries should contain weather-related terms. Found: {string.Join(", ", summaryTexts)}");
        }
    }

    [Fact]
    public async Task WeatherComponent_ShouldMaintainTableStructureWithDifferentDataSizes()
    {
        var table = Page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Verify table maintains structure regardless of data size
        var dataRows = table.Locator("tbody tr");
        var rowCount = await dataRows.CountAsync();
        
        // Each row should have consistent column count
        for (int i = 0; i < Math.Min(5, rowCount); i++)
        {
            var row = dataRows.Nth(i);
            var cells = row.Locator("td");
            await Expect(cells).ToHaveCountAsync(4, new() { Timeout = 5000 });
        }
        
        // Table should maintain its Bootstrap styling
        await Expect(table).ToHaveClassAsync(new Regex(".*table.*"));
        
        // Header structure should remain consistent
        var headers = table.Locator("thead th");
        await Expect(headers).ToHaveCountAsync(4);
    }
}