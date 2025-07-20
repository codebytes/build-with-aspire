using System.ComponentModel;
using System.Text.Json;

namespace BuildWithAspire.McpServer.Tools;

/// <summary>
/// Weather tool for MCP that provides weather forecasts.
/// </summary>
public class WeatherTool
{
    public string Name => "get_weather";
    public string Description => "Get weather forecast for a specified location and number of days";

    public object GetParametersSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                location = new
                {
                    type = "string",
                    description = "The location to get weather for (city, country)"
                },
                days = new
                {
                    type = "integer",
                    description = "Number of days to forecast (1-7, default: 5)",
                    minimum = 1,
                    maximum = 7,
                    @default = 5
                }
            },
            required = new[] { "location" }
        };
    }

    public async Task<object> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var location = parameters.GetValueOrDefault("location", "Unknown Location")?.ToString() ?? "Unknown Location";
        
        // Handle JsonElement properly
        var daysObj = parameters.GetValueOrDefault("days", 5);
        int days = 5; // default
        
        if (daysObj is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                days = jsonElement.GetInt32();
            }
        }
        else
        {
            days = Convert.ToInt32(daysObj, System.Globalization.CultureInfo.InvariantCulture);
        }
        
        // Clamp days to valid range
        days = Math.Max(1, Math.Min(7, days));

        var forecasts = new List<object>();
        var random = new Random();
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

        for (int i = 0; i < days; i++)
        {
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(i + 1));
            var tempC = random.Next(-20, 55);
            var tempF = 32 + (int)(tempC / 0.5556);
            var summary = summaries[random.Next(summaries.Length)];

            forecasts.Add(new
            {
                date = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                location = location,
                temperatureC = tempC,
                temperatureF = tempF,
                summary = summary,
                description = $"{summary} weather expected in {location} with temperature around {tempC}°C ({tempF}°F)"
            });
        }

        await Task.Delay(100, cancellationToken).ConfigureAwait(false); // Simulate some processing time

        return new
        {
            location = location,
            forecast_days = days,
            forecasts = forecasts,
            generated_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}