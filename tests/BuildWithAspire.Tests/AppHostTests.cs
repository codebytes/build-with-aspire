using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BuildWithAspire.Tests;

public class AppHostTests
{
    [Fact]
    public async Task AppHost_CreatesSuccessfully()
    {
        // Arrange & Act
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BuildWithAspire_AppHost>();
        await using var app = await builder.BuildAsync();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public async Task AppHost_StartsSuccessfully()
    {
        // Arrange
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BuildWithAspire_AppHost>();
        await using var app = await builder.BuildAsync();

        // Act
        await app.StartAsync();

        // Assert - If we get here without exceptions, the app started successfully
        Assert.True(true);
    }

    [Fact]
    public async Task AppHost_ApiService_IsAvailable()
    {
        // Arrange
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BuildWithAspire_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        var response = await httpClient.GetAsync("/weatherforecast");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task AppHost_WebFrontend_IsAvailable()
    {
        // Arrange
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BuildWithAspire_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        var response = await httpClient.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("BuildWithAspire", content);
    }

    [Fact]
    public async Task AppHost_Database_IsConfigured()
    {
        // Arrange
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BuildWithAspire_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        var response = await httpClient.GetAsync("/conversations");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task AppHost_HasExpectedResources()
    {
        // Arrange
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BuildWithAspire_AppHost>();
        await using var app = await builder.BuildAsync();

        // Act
        await app.StartAsync();

        // Basic verification that app started
        Assert.NotNull(app);
        
        // Verify we can create HTTP clients for our services
        var apiClient = app.CreateHttpClient("apiservice");
        var webClient = app.CreateHttpClient("webfrontend");
        
        Assert.NotNull(apiClient);
        Assert.NotNull(webClient);
    }
}