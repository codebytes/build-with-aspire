using BuildWithAspire.Abstractions;
using Microsoft.Extensions.AI;

namespace BuildWithAspire.ApiService.Endpoints;

/// <summary>
/// Weather forecast endpoints with AI-generated summaries.
/// </summary>
public static class WeatherEndpoints
{
    public static RouteGroupBuilder MapWeatherEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/weatherforecast");

        group.MapGet("/", GetWeatherForecast)
            .WithName("GetWeatherForecast")
            .WithOpenApi()
            .RequireRateLimiting("weather");

        return group;
    }

    private static IAsyncEnumerable<WeatherForecast> GetWeatherForecast(
        IChatClient client,
        ILoggerFactory loggerFactory,
        AIConfiguration.AISettings settings)
    {
        var logger = loggerFactory.CreateLogger("WeatherForecast");
        var weatherAgent = new Microsoft.Agents.AI.ChatClientAgent(
            client,
            new Microsoft.Agents.AI.ChatClientAgentOptions
            {
                Name = "WeatherAssistant",
                Instructions = "You are a helpful assistant that provides a description of the weather in one word based on the temperature."
            });

        return GetForecasts(weatherAgent, logger, settings);
    }

    private static async IAsyncEnumerable<WeatherForecast> GetForecasts(
        Microsoft.Agents.AI.AIAgent agent,
        ILogger logger,
        AIConfiguration.AISettings settings)
    {
        for (int index = 1; index <= 5; index++)
        {
            var temperature = Random.Shared.Next(-20, 55);
            string summary;
            try
            {
                summary = await GetWeatherSummary(agent, temperature, logger, settings).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI summary failed (temp={Temp})", temperature);
                summary = GetFallbackSummary(temperature);
            }
            yield return new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                temperature,
                summary
            );
        }
    }

    private static async Task<string> GetWeatherSummary(
        Microsoft.Agents.AI.AIAgent agent,
        int temp,
        ILogger logger,
        AIConfiguration.AISettings settings)
    {
        logger.LogDebug("Requesting weather summary (Provider={Provider}, Temp={Temp})", settings.Provider, temp);

        var response = await agent.RunAsync(
            $"How would you describe the weather at temp {temp} in celsius? Provide the response in 1 word with no punctuation.").ConfigureAwait(false);

        var responseText = response.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            logger.LogWarning("Empty AI response (Temp={Temp}), using fallback", temp);
            return GetFallbackSummary(temp);
        }

        var trimmed = responseText.Trim();
        if (trimmed.Length > 20)
        {
            logger.LogWarning("AI response too long (Temp={Temp}), using fallback", temp);
            return GetFallbackSummary(temp);
        }

        logger.LogDebug("AI response: {Response}", trimmed);
        return trimmed;
    }

    private static string GetFallbackSummary(int temp) => temp switch
    {
        < 0 => "freezing",
        < 10 => "cold",
        < 20 => "cool",
        < 30 => "warm",
        _ => "hot"
    };
}
