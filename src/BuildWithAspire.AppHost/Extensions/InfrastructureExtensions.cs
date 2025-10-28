namespace BuildWithAspire.AppHost.Extensions;

/// <summary>
/// Extension methods for configuring environment settings in Aspire projects.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Sets the environment name for both .NET and ASP.NET Core.
    /// </summary>
    /// <param name="builder">The project resource builder</param>
    /// <param name="environmentName">Environment name (e.g., "Development", "Production")</param>
    /// <returns>The builder for method chaining</returns>
    public static IResourceBuilder<ProjectResource> WithEnvironmentConfig(
        this IResourceBuilder<ProjectResource> builder,
        string environmentName)
    {
        return builder
            .WithEnvironment("DOTNET_ENVIRONMENT", environmentName)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName);
    }
}
