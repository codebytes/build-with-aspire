using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildWithAspire.AppHost.UnitTests;

/// <summary>
/// Integration tests for the AppHost following Aspire best practices.
/// Uses DistributedApplicationTestingBuilder for proper test orchestration.
/// </summary>
public class AppHostTests
{

    [Fact]
    public void AppHost_ShouldCreateBuilder()
    {
        // Act & Assert - This primarily tests that the AppHost can be instantiated
        var builder = DistributedApplication.CreateBuilder();
        Assert.NotNull(builder);
    }

    [Fact]
    public void AppHost_ShouldAddApiService()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice");

        // Assert
        Assert.NotNull(apiService);
        Assert.Equal("apiservice", apiService.Resource.Name);
    }

    [Fact]
    public void AppHost_ShouldAddWebFrontend()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice");

        // Act
        var webFrontend = builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithReference(apiService);

        // Assert
        Assert.NotNull(webFrontend);
        Assert.Equal("webfrontend", webFrontend.Resource.Name);
    }

    [Fact]
    public void AppHost_ShouldSupportServiceReferences()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice");

        // Act
        var webFrontend = builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithReference(apiService);

        // Assert
        Assert.NotNull(apiService);
        Assert.NotNull(webFrontend);
        Assert.Equal("apiservice", apiService.Resource.Name);
        Assert.Equal("webfrontend", webFrontend.Resource.Name);
    }

    /// <summary>
    /// Tests that the WebFrontend resource has the correct environment variable reference to ApiService.
    /// This follows Aspire testing best practices for verifying resource dependencies.
    /// </summary>
    [Fact]
    public async Task WebFrontend_ShouldHaveApiServiceReference()
    {
        // Arrange - Use DistributedApplicationTestingBuilder for proper Aspire testing
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.BuildWithAspire_AppHost>(
            [
                // Disable volumes for testing
                "UseVolumes=false"
            ]);

        // Act - Get the webfrontend resource
        var webFrontend = builder.CreateResourceBuilder<ProjectResource>("webfrontend");
        var envVars = await webFrontend.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish);

        // Assert - Verify the apiservice reference is configured
        Assert.Contains(envVars, kvp =>
        {
            var (key, _) = kvp;
            return key is "services__apiservice__https__0" or "services__apiservice__http__0"
                   or "APISERVICE_HTTPS" or "APISERVICE_HTTP";
        });
    }

    /// <summary>
    /// Tests that all expected resources are present in the AppHost.
    /// </summary>
    [Fact]
    public async Task AppHost_ShouldContainExpectedResources()
    {
        // Arrange
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.BuildWithAspire_AppHost>(
            [
                "UseVolumes=false"
            ]);

        // Act
        var resources = builder.Resources.Select(r => r.Name).ToList();

        // Assert - Verify core resources exist
        Assert.Contains("apiservice", resources);
        Assert.Contains("webfrontend", resources);
        Assert.Contains("mcpserver", resources);
    }
}