using System.Linq;

namespace BuildWithAspire.Web.Clients;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var allForecasts = await httpClient.GetFromJsonAsync<WeatherForecast[]>("weatherforecast", cancellationToken).ConfigureAwait(false);
        
        if (allForecasts == null)
        {
            return Array.Empty<WeatherForecast>();
        }

        return allForecasts.Take(maxItems).ToArray();
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
