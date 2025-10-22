using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BuildWithAspire.MCPServer.Tools;

/// <summary>
/// Weather-related MCP tools for BuildWithAspire application.
/// These tools provide weather forecast functionality via MCP protocol.
/// </summary>
[McpServerToolType]
public sealed class WeatherTools
{
    private readonly IChatClient? _chatClient;
    private readonly ILogger<WeatherTools> _logger;

    public WeatherTools(ILogger<WeatherTools> logger, IChatClient? chatClient = null)
    {
        _logger = logger;
        _chatClient = chatClient;
    }

    [McpServerTool(Name = "getWeatherForecast")]
    [Description("Gets a weather forecast for the next 5 days with AI-generated weather descriptions.")]
    public async Task<WeatherForecast[]> GetWeatherForecast(
        [Description("Maximum number of forecast days to return (1-10)")] int maxDays = 5)
    {
        _logger.LogInformation("MCP Tool 'getWeatherForecast' called with maxDays={MaxDays}", maxDays);

        if (maxDays < 1)
        {
            maxDays = 1;
        }
        if (maxDays > 10)
        {
            maxDays = 10;
        }

        var forecasts = new List<WeatherForecast>();

        for (int index = 1; index <= maxDays; index++)
        {
            var temperature = Random.Shared.Next(-20, 55);
            var summary = await GetWeatherSummary(temperature).ConfigureAwait(false);

            forecasts.Add(new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                temperature,
                summary
            ));
        }

        return forecasts.ToArray();
    }

    [McpServerTool(Name = "getCurrentWeather")]
    [Description("Gets current weather information for today with AI-generated description.")]
    public async Task<WeatherForecast> GetCurrentWeather()
    {
        _logger.LogInformation("MCP Tool 'getCurrentWeather' called");

        var temperature = Random.Shared.Next(-20, 55);
        var summary = await GetWeatherSummary(temperature).ConfigureAwait(false);

        return new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Today),
            temperature,
            summary
        );
    }

    [McpServerTool(Name = "convertTemperature")]
    [Description("Converts temperature between Celsius and Fahrenheit.")]
    public static TemperatureConversion ConvertTemperature(
        [Description("Temperature value to convert")] double temperature,
        [Description("Source unit: 'C' for Celsius, 'F' for Fahrenheit")] string fromUnit = "C")
    {
        bool isCelsius = fromUnit.ToUpper() == "C";

        if (isCelsius)
        {
            var fahrenheit = (temperature * 9.0 / 5.0) + 32;
            return new TemperatureConversion(temperature, "°C", fahrenheit, "°F");
        }
        else
        {
            var celsius = (temperature - 32) * 5.0 / 9.0;
            return new TemperatureConversion(temperature, "°F", celsius, "°C");
        }
    }

    private async Task<string> GetWeatherSummary(int temp)
    {
        if (_chatClient == null)
        {
            // Fallback to simple temperature-based descriptions
            return temp switch
            {
                < 0 => "Freezing",
                < 10 => "Cold",
                < 20 => "Cool",
                < 30 => "Warm",
                _ => "Hot"
            };
        }

        try
        {
            List<ChatMessage> conversation = new()
            {
                new(ChatRole.System, "You are a helpful assistant that provides a description of the weather in one word based on the temperature."),
                new(ChatRole.User, $"How would you describe the weather at temp {temp} in celsius? Provide the response in 1 word with no punctuation.")
            };

            var completion = await _chatClient.GetResponseAsync(conversation).ConfigureAwait(false);
            return completion.Text ?? "Pleasant";
        }
        catch
        {
            // Fallback on error
            return temp switch
            {
                < 0 => "Freezing",
                < 10 => "Cold",
                < 20 => "Cool",
                < 30 => "Warm",
                _ => "Hot"
            };
        }
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record TemperatureConversion(double OriginalValue, string OriginalUnit, double ConvertedValue, string ConvertedUnit);
