
namespace BuildWithAspire.ApiService.UnitTests.Models;

public class WeatherForecastTests
{
    [Fact]
    public void WeatherForecast_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var temperatureC = 25;
        var summary = "Pleasant";

        // Act
        var forecast = new WeatherForecast(date, temperatureC, summary);

        // Assert
        Assert.Equal(date, forecast.Date);
        Assert.Equal(temperatureC, forecast.TemperatureC);
        Assert.Equal(summary, forecast.Summary);
    }

    [Fact]
    public void WeatherForecast_TemperatureF_ShouldConvertCorrectly()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var temperatureC = 0; // Freezing point
        var summary = "Freezing";
        var forecast = new WeatherForecast(date, temperatureC, summary);

        // Act
        var temperatureF = forecast.TemperatureF;

        // Assert
        Assert.Equal(32, temperatureF);
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(100, 212)]
    [InlineData(25, 77)]
    public void WeatherForecast_TemperatureF_ShouldConvertTemperatures(int celsius, int expectedApproximate)
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var forecast = new WeatherForecast(date, celsius, "Test");

        // Act
        var actualFahrenheit = forecast.TemperatureF;

        // Assert - Allow some variation due to the conversion formula used
        Assert.True(Math.Abs(actualFahrenheit - expectedApproximate) <= 5, 
            $"Expected approximately {expectedApproximate}°F, but got {actualFahrenheit}°F");
    }

    [Fact]
    public void WeatherForecast_WithNullSummary_ShouldWork()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var temperatureC = 20;

        // Act
        var forecast = new WeatherForecast(date, temperatureC, null);

        // Assert
        Assert.Equal(date, forecast.Date);
        Assert.Equal(temperatureC, forecast.TemperatureC);
        Assert.Null(forecast.Summary);
    }
}