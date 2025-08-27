using Microsoft.Extensions.Configuration;
using NSubstitute;
using FluentAssertions;
using BuildWithAspire.ApiService.Services;

namespace BuildWithAspire.ApiService.UnitTests.Services;

public class McpClientTests
{
    private readonly ILogger<McpClient> _mockLogger;
    private readonly IConfiguration _mockConfiguration;
    private readonly McpClient _mcpClient;

    public McpClientTests()
    {
        _mockLogger = Substitute.For<ILogger<McpClient>>();
        _mockConfiguration = Substitute.For<IConfiguration>();
        _mcpClient = new McpClient(_mockLogger, _mockConfiguration);
    }

    [Fact]
    public async Task InitializeAsync_FirstCall_ShouldReturnTrue()
    {
        // Act
        var result = await _mcpClient.InitializeAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_SecondCall_ShouldReturnTrueImmediately()
    {
        // Arrange
        await _mcpClient.InitializeAsync();

        // Act
        var result = await _mcpClient.InitializeAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ListToolsAsync_WhenNotInitialized_ShouldInitializeFirst()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListToolsAsync_WhenInitialized_ShouldReturnTools()
    {
        // Arrange
        await _mcpClient.InitializeAsync();

        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CallToolAsync_CurrentWeather_ShouldReturnResult()
    {
        // Arrange
        await _mcpClient.InitializeAsync();
        var parameters = new { location = "Seattle" };

        // Act
        var result = await _mcpClient.CallToolAsync("GetCurrentWeather", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task CallToolAsync_WeatherForecast_ShouldReturnResult()
    {
        // Arrange
        await _mcpClient.InitializeAsync();
        var parameters = new { location = "New York", days = 5 };

        // Act
        var result = await _mcpClient.CallToolAsync("GetWeatherForecast", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task CallToolAsync_Calculate_ShouldReturnResult()
    {
        // Arrange
        await _mcpClient.InitializeAsync();
        var parameters = new { expression = "2 + 2" };

        // Act
        var result = await _mcpClient.CallToolAsync("Calculate", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task CallToolAsync_UnknownTool_ShouldReturnError()
    {
        // Arrange
        await _mcpClient.InitializeAsync();
        var parameters = new { };

        // Act
        var result = await _mcpClient.CallToolAsync("unknown_tool", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);
    }

    [Theory]
    [InlineData("ConvertTemperature")]
    [InlineData("GetCurrentDateTime")]
    [InlineData("GenerateRandomNumber")]
    [InlineData("GetSystemInfo")]
    [InlineData("EncodeToBase64")]
    [InlineData("DecodeFromBase64")]
    [InlineData("SquareRoot")]
    [InlineData("Power")]
    [InlineData("IsPrime")]
    public async Task CallToolAsync_VariousTools_ShouldReturnResults(string toolName)
    {
        // Arrange
        await _mcpClient.InitializeAsync();
        object parameters = toolName switch
        {
            "ConvertTemperature" => new { temperature = 25.0, from_unit = "C", to_unit = "F" },
            "GenerateRandomNumber" => new { min = 1, max = 100 },
            "EncodeToBase64" => new { text = "Hello World" },
            "DecodeFromBase64" => new { base64 = "SGVsbG8gV29ybGQ=" },
            "SquareRoot" => new { number = 16 },
            "Power" => new { @base = 2, exponent = 3 },
            "Fibonacci" => new { n = 10 },
            "IsPrime" => new { number = 17 },
            _ => new { }
        };

        // Act
        var result = await _mcpClient.CallToolAsync(toolName, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await _mcpClient.DisposeAsync().ConfigureAwait(false));
        Assert.Null(exception);
    }
}