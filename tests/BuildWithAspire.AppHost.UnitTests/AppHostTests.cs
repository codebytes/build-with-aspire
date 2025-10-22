using Aspire.Hosting;

namespace BuildWithAspire.AppHost.UnitTests;

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
}