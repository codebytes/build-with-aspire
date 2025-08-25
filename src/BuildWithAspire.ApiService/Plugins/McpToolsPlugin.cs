using Microsoft.SemanticKernel;
using BuildWithAspire.ApiService.Services;
using System.ComponentModel;
using System.Text.Json;

namespace BuildWithAspire.ApiService.Plugins;

/// <summary>
/// Semantic Kernel plugin that exposes MCP tools as kernel functions.
/// This allows the AI to discover and use MCP tools through function calling.
/// </summary>
public class McpToolsPlugin
{
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<McpToolsPlugin> _logger;

    public McpToolsPlugin(IMcpClient mcpClient, ILogger<McpToolsPlugin> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    [KernelFunction, Description("Get current weather information")]
    public async Task<string> GetCurrentWeatherAsync()
    {
        try
        {
            await _mcpClient.InitializeAsync().ConfigureAwait(false);
            var result = await _mcpClient.CallToolAsync("GetCurrentWeather").ConfigureAwait(false);

            if (result != null)
            {
                var jsonString = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Retrieved current weather via MCP: {Weather}", jsonString);
                return jsonString;
            }

            return "Current weather information is not available at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current weather via MCP");
            return "Unable to retrieve current weather information.";
        }
    }

    [KernelFunction, Description("Get weather forecast for specified number of days (default 5, max 10)")]
    public async Task<string> GetWeatherForecastAsync(
        [Description("Number of days for the forecast (1-10, default 5)")] int maxDays = 5)
    {
        try
        {
            // Validate input
            if (maxDays < 1)
            {
                maxDays = 1;
            }
            if (maxDays > 10)
            {
                maxDays = 10;
            }

            await _mcpClient.InitializeAsync().ConfigureAwait(false);
            var result = await _mcpClient.CallToolAsync("GetWeatherForecast", new { MaxDays = maxDays }).ConfigureAwait(false);

            if (result != null)
            {
                var jsonString = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Retrieved weather forecast for {Days} days via MCP: {Forecast}", maxDays, jsonString);
                return jsonString;
            }

            return $"Weather forecast for {maxDays} days is not available at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weather forecast via MCP for {Days} days", maxDays);
            return $"Unable to retrieve weather forecast for {maxDays} days.";
        }
    }

    [KernelFunction, Description("List all available MCP tools and their capabilities")]
    public async Task<string> ListAvailableToolsAsync()
    {
        try
        {
            await _mcpClient.InitializeAsync().ConfigureAwait(false);
            var tools = await _mcpClient.ListToolsAsync().ConfigureAwait(false);

            if (tools.Length > 0)
            {
                var toolsList = string.Join(", ", tools.Select(t => t.Name));
                _logger.LogInformation("Listed MCP tools: {Tools}", toolsList);
                return $"Available MCP tools: {toolsList}";
            }

            return "No MCP tools are currently available.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list MCP tools");
            return "Unable to retrieve available tools at this time.";
        }
    }

    [KernelFunction, Description("Call a specific MCP tool with optional parameters")]
    public async Task<string> CallMcpToolAsync(
        [Description("Name of the MCP tool to call")] string toolName,
        [Description("JSON parameters for the tool (optional)")] string? parameters = null)
    {
        try
        {
            await _mcpClient.InitializeAsync().ConfigureAwait(false);

            object? parsedParameters = null;
            if (!string.IsNullOrEmpty(parameters))
            {
                try
                {
                    parsedParameters = JsonSerializer.Deserialize<object>(parameters);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse parameters for MCP tool {ToolName}: {Parameters}", toolName, parameters);
                    return $"Invalid parameters format for tool {toolName}. Parameters must be valid JSON.";
                }
            }

            var result = await _mcpClient.CallToolAsync(toolName, parsedParameters).ConfigureAwait(false);

            if (result != null)
            {
                var jsonString = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Successfully called MCP tool {ToolName} with result: {Result}", toolName, jsonString);
                return jsonString;
            }

            return $"Tool {toolName} executed successfully but returned no data.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MCP tool {ToolName} with parameters {Parameters}", toolName, parameters);
            return $"Unable to execute tool {toolName}. Error: {ex.Message}";
        }
    }

    // System tools wrappers
    [KernelFunction, Description("Get current date and time info")]
    public Task<string> GetCurrentDateTimeAsync() => CallMcpToolAsync("GetCurrentDateTime");

    [KernelFunction, Description("Get basic system information (OS, .NET version, etc.)")]
    public Task<string> GetSystemInfoAsync() => CallMcpToolAsync("GetSystemInfo");

    [KernelFunction, Description("Generate a random integer between min and max (inclusive)")]
    public Task<string> GenerateRandomNumberAsync(
        [Description("Minimum value (inclusive)")] int min = 1,
        [Description("Maximum value (inclusive)")] int max = 100)
        => CallMcpToolAsync("GenerateRandomNumber", JsonSerializer.Serialize(new { Min = min, Max = max + 1 })); // server treats Max as exclusive

    [KernelFunction, Description("Encode plain text to Base64")]
    public Task<string> EncodeToBase64Async([Description("Text to encode")] string text)
        => CallMcpToolAsync("EncodeToBase64", JsonSerializer.Serialize(new { Text = text }));

    [KernelFunction, Description("Decode Base64 text to plain text")]
    public Task<string> DecodeFromBase64Async([Description("Base64 string to decode")] string base64Text)
        => CallMcpToolAsync("DecodeFromBase64", JsonSerializer.Serialize(new { Base64Text = base64Text }));

    // Math tools wrappers
    [KernelFunction, Description("Perform arithmetic operation (add, subtract, multiply, divide)")]
    public Task<string> CalculateAsync(
        [Description("First operand")] double a,
        [Description("Second operand")] double b,
        [Description("Operation: add | subtract | multiply | divide")] string operation)
        => CallMcpToolAsync("Calculate", JsonSerializer.Serialize(new { A = a, B = b, Operation = operation }));

    [KernelFunction, Description("Compute square root of a non-negative number")]
    public Task<string> SquareRootAsync([Description("Number to square root")] double number)
        => CallMcpToolAsync("SquareRoot", JsonSerializer.Serialize(new { Number = number }));

    [KernelFunction, Description("Raise base number to exponent")]
    public Task<string> PowerAsync(
        [Description("Base number")] double baseNumber,
        [Description("Exponent")] double exponent)
        => CallMcpToolAsync("Power", JsonSerializer.Serialize(new { BaseNumber = baseNumber, Exponent = exponent }));

    [KernelFunction, Description("Generate Fibonacci sequence up to N terms (1-50)")]
    public Task<string> GenerateFibonacciAsync([Description("Number of terms (1-50)")] int terms)
        => CallMcpToolAsync("GenerateFibonacci", JsonSerializer.Serialize(new { Terms = terms }));

    [KernelFunction, Description("Check if a number is prime")]
    public Task<string> IsPrimeAsync([Description("Number to test")] long number)
        => CallMcpToolAsync("IsPrime", JsonSerializer.Serialize(new { Number = number }));

    [KernelFunction, Description("Convert temperature between Celsius and Fahrenheit")]
    public Task<string> ConvertTemperatureAsync(
        [Description("Temperature value")] double temperature,
        [Description("Source unit: C or F")] string fromUnit = "C")
        => CallMcpToolAsync("ConvertTemperature", JsonSerializer.Serialize(new { Temperature = temperature, FromUnit = fromUnit }));
}
