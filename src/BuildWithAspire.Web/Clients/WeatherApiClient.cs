using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BuildWithAspire.Web.Clients;

public class WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting weather request for {MaxItems} items", maxItems);
            logger.LogInformation("HttpClient BaseAddress: {BaseAddress}", httpClient.BaseAddress);
            
            // Call the MCP GetWeatherForecast tool with maxItems parameter
            var requestBody = new { MaxDays = Math.Min(maxItems, 10) }; // Limit to 10 days max
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            logger.LogInformation("Making POST request to mcp/call/GetWeatherForecast with body: {RequestBody}", jsonContent);
            
            var response = await httpClient.PostAsync($"mcp/call/GetWeatherForecast", content, cancellationToken).ConfigureAwait(false);
            
            logger.LogInformation("Response received - Status: {StatusCode}, ReasonPhrase: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("MCP forecast call failed with status {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                // Fallback to current weather if forecast fails
                return await GetCurrentWeatherFallback(cancellationToken).ConfigureAwait(false);
            }
            
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var mcpResult = JsonSerializer.Deserialize<McpCallResult>(responseJson);
            
            if (mcpResult?.Content != null && mcpResult.Content.Length > 0)
            {
                var contentText = mcpResult.Content[0].Text;
                logger.LogInformation("MCP content text: {ContentText}", contentText);
                if (!string.IsNullOrEmpty(contentText))
                {
                    // The contentText is already a valid JSON string, just deserialize directly
                    logger.LogInformation("Deserializing weather data from JSON: {ContentText}", contentText);
                    
                    try 
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            // Remove PropertyNamingPolicy to handle exact JSON property names
                        };
                        var weatherData = JsonSerializer.Deserialize<WeatherForecast[]>(contentText, options);
                        logger.LogInformation("Deserialized {WeatherEntryCount} weather entries", weatherData?.Length ?? 0);
                        
                        if (weatherData != null && weatherData.Length > 0)
                        {
                            logger.LogInformation("First weather entry: Date={Date}, Temp={Temp}C, Summary={Summary}", 
                                weatherData[0].Date, weatherData[0].TemperatureC, weatherData[0].Summary);
                        }
                        
                        return weatherData ?? Array.Empty<WeatherForecast>();
                    }
                    catch (JsonException ex)
                    {
                        logger.LogError(ex, "JSON deserialization failed for content: {ContentText}", contentText);
                        return Array.Empty<WeatherForecast>();
                    }
                }
            }
            else
            {
                logger.LogWarning("MCP result is null or has no content. IsError: {IsError}", mcpResult?.IsError);
            }
            
            return Array.Empty<WeatherForecast>();
        }
        catch (Exception ex)
        {
            // Log error details
            logger.LogError(ex, "Exception occurred while calling MCP weather forecast - Type: {ExceptionType}", ex.GetType().Name);
            logger.LogInformation("Falling back to current weather");
            return await GetCurrentWeatherFallback(cancellationToken).ConfigureAwait(false);
        }
    }
    
    private async Task<WeatherForecast[]> GetCurrentWeatherFallback(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Trying fallback GetCurrentWeather");
            var response = await httpClient.PostAsync("mcp/call/GetCurrentWeather", new StringContent("{}", Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);
            
            logger.LogInformation("Fallback current weather call status: {StatusCode}", response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var mcpResult = JsonSerializer.Deserialize<McpCallResult>(responseJson);
                
                if (mcpResult?.Content != null && mcpResult.Content.Length > 0)
                {
                    var contentText = mcpResult.Content[0].Text;
                    if (!string.IsNullOrEmpty(contentText))
                    {
                        // Deserialize the JSON directly from contentText
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        };
                        var singleWeather = JsonSerializer.Deserialize<WeatherForecast>(contentText, options);
                        return singleWeather != null ? new[] { singleWeather } : Array.Empty<WeatherForecast>();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling MCP current weather fallback tool 'GetCurrentWeather'");
        }
        
        return Array.Empty<WeatherForecast>();
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// MCP response DTOs
public class McpCallResult
{
    [JsonPropertyName("Content")]
    public McpContent[]? Content { get; set; }
    
    [JsonPropertyName("IsError")]
    public bool IsError { get; set; }
}

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
