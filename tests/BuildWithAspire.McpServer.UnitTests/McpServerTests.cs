using BuildWithAspire.McpServer.Tools;
using System.Text.Json;
using Xunit;

namespace BuildWithAspire.McpServer.UnitTests;

public class WeatherToolTests
{
    private readonly WeatherTool _weatherTool;

    public WeatherToolTests()
    {
        _weatherTool = new WeatherTool();
    }

    [Fact]
    public void Name_ShouldReturnExpectedValue()
    {
        // Act
        var name = _weatherTool.Name;

        // Assert
        Assert.Equal("get_weather", name);
    }

    [Fact]
    public void Description_ShouldReturnExpectedValue()
    {
        // Act
        var description = _weatherTool.Description;

        // Assert
        Assert.Equal("Get weather forecast for a specified location and number of days", description);
    }

    [Fact]
    public void GetParametersSchema_ShouldReturnValidSchema()
    {
        // Act
        var schema = _weatherTool.GetParametersSchema();

        // Assert
        Assert.NotNull(schema);
        
        var json = JsonSerializer.Serialize(schema);
        Assert.Contains("location", json);
        Assert.Contains("days", json);
        Assert.Contains("required", json);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidParameters_ShouldReturnWeatherData()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["location"] = "New York",
            ["days"] = 3
        };

        // Act
        var result = await _weatherTool.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(result);
        
        var json = JsonSerializer.Serialize(result);
        Assert.Contains("New York", json);
        Assert.Contains("forecasts", json);
        Assert.Contains("location", json);
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonElements_ShouldHandleCorrectly()
    {
        // Arrange - Simulate JSON deserialized parameters
        var daysElement = JsonSerializer.Deserialize<JsonElement>("2");
        var parameters = new Dictionary<string, object>
        {
            ["location"] = "Paris",
            ["days"] = daysElement
        };

        // Act
        var result = await _weatherTool.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(result);
        
        var json = JsonSerializer.Serialize(result);
        Assert.Contains("Paris", json);
        Assert.Contains("forecast_days", json);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultDays_ShouldUse5Days()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["location"] = "London"
        };

        // Act
        var result = await _weatherTool.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(result);
        
        var resultObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result));
        var forecastDays = resultObj.GetProperty("forecast_days").GetInt32();
        Assert.Equal(5, forecastDays);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidDaysRange_ShouldClampToValidRange()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["location"] = "Tokyo",
            ["days"] = 10 // Should be clamped to 7
        };

        // Act
        var result = await _weatherTool.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(result);
        
        var resultObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result));
        var forecastDays = resultObj.GetProperty("forecast_days").GetInt32();
        Assert.Equal(7, forecastDays); // Should be clamped to max 7
    }
}

public class TimeToolTests
{
    private readonly TimeTool _timeTool;

    public TimeToolTests()
    {
        _timeTool = new TimeTool();
    }

    [Fact]
    public void Name_ShouldReturnExpectedValue()
    {
        // Act
        var name = _timeTool.Name;

        // Assert
        Assert.Equal("get_time", name);
    }

    [Fact]
    public void Description_ShouldReturnExpectedValue()
    {
        // Act
        var description = _timeTool.Description;

        // Assert
        Assert.Equal("Get current date and time information in various formats", description);
    }

    [Fact]
    public async Task ExecuteAsync_WithUTCTimezone_ShouldReturnUTCTime()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["timezone"] = "UTC",
            ["format"] = "iso"
        };

        // Act
        var result = await _timeTool.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(result);
        
        var json = JsonSerializer.Serialize(result);
        Assert.Contains("UTC", json);
        Assert.Contains("current_time", json);
    }

    [Fact]
    public async Task ExecuteAsync_WithDetailedFormat_ShouldReturnDetailedInfo()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["timezone"] = "UTC",
            ["format"] = "detailed"
        };

        // Act
        var result = await _timeTool.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(result);
        
        var json = JsonSerializer.Serialize(result);
        Assert.Contains("day_of_week", json);
        Assert.Contains("unix_timestamp", json);
        Assert.Contains("week_of_year", json);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoParameters_ShouldUseDefaults()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _timeTool.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(result);
        
        var resultObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result));
        var timezone = resultObj.GetProperty("timezone").GetString();
        Assert.Equal("UTC", timezone);
    }
}

public class McpServerTests
{
    private readonly McpServer _mcpServer;
    private readonly WeatherTool _weatherTool;
    private readonly TimeTool _timeTool;

    public McpServerTests()
    {
        _weatherTool = new WeatherTool();
        _timeTool = new TimeTool();
        _mcpServer = new McpServer(_weatherTool, _timeTool);
    }

    [Fact]
    public void GetAvailableTools_ShouldReturnAllTools()
    {
        // Act
        var result = _mcpServer.GetAvailableTools();

        // Assert
        Assert.NotNull(result);
        
        var json = JsonSerializer.Serialize(result);
        Assert.Contains("get_weather", json);
        Assert.Contains("get_time", json);
        Assert.Contains("tools", json);
    }

    [Fact]
    public async Task CallToolAsync_WithValidWeatherRequest_ShouldReturnWeatherData()
    {
        // Arrange
        var request = new McpToolCallRequest
        {
            Name = "get_weather",
            Arguments = new Dictionary<string, object>
            {
                ["location"] = "Berlin",
                ["days"] = 2
            }
        };

        // Act
        var result = await _mcpServer.CallToolAsync(request);

        // Assert
        Assert.NotNull(result);
        
        var json = JsonSerializer.Serialize(result);
        Assert.Contains("Berlin", json);
    }

    [Fact]
    public async Task CallToolAsync_WithValidTimeRequest_ShouldReturnTimeData()
    {
        // Arrange
        var request = new McpToolCallRequest
        {
            Name = "get_time",
            Arguments = new Dictionary<string, object>
            {
                ["timezone"] = "UTC",
                ["format"] = "iso"
            }
        };

        // Act
        var result = await _mcpServer.CallToolAsync(request);

        // Assert
        Assert.NotNull(result);
        
        var json = JsonSerializer.Serialize(result);
        Assert.Contains("UTC", json);
    }

    [Fact]
    public async Task CallToolAsync_WithInvalidToolName_ShouldThrowException()
    {
        // Arrange
        var request = new McpToolCallRequest
        {
            Name = "invalid_tool",
            Arguments = new Dictionary<string, object>()
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mcpServer.CallToolAsync(request));
        
        Assert.Contains("Tool 'invalid_tool' not found", exception.Message);
    }
}