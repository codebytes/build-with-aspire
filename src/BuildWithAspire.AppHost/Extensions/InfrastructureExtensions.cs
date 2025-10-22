using Microsoft.Extensions.Configuration;

namespace BuildWithAspire.AppHost.Extensions;

/// <summary>
/// Extensions for infrastructure and deployment configuration
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Adds infrastructure-specific environment configuration to a project
    /// </summary>
    /// <param name="builder">The project resource builder</param>
    /// <param name="environmentName">The environment name</param>
    /// <returns>The project resource builder</returns>
    public static IResourceBuilder<ProjectResource> WithEnvironmentConfig(
        this IResourceBuilder<ProjectResource> builder,
        string environmentName)
    {
        return builder
            .WithEnvironment("DOTNET_ENVIRONMENT", environmentName)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName);
    }
}
