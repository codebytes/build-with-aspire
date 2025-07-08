// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildWithAspire.AppHost.Extensions;

/// <summary>
/// Represents a Foundry Local AI service resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="model">The model name.</param>
public class FoundryLocalResource(string name, string model)
    : Resource(name),
    IResourceWithConnectionString,
    IResourceWithEnvironment,
    IResourceWithoutLifetime
{
    internal const string DefaultFoundryLocalEndpoint = "http://localhost:8000";
    internal const string DefaultModelCachePath = "/tmp/foundry-local-models";

    /// <summary>
    /// Gets or sets the model name, e.g., "phi-3.5-mini".
    /// </summary>
    public string Model { get; set; } = model;

    /// <summary>
    /// Gets or sets the endpoint URL for the Foundry Local service.
    /// </summary>
    public string Endpoint { get; set; } = DefaultFoundryLocalEndpoint;

    /// <summary>
    /// Gets or sets the path where Foundry Local models are cached.
    /// </summary>
    public string ModelCachePath { get; set; } = DefaultModelCachePath;

    /// <summary>
    /// Gets or sets whether Foundry Local should automatically start the model.
    /// </summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Gets the connection string expression for the Foundry Local resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Provider=FoundryLocal;Model={Model};Endpoint={Endpoint};AutoStart={AutoStart.ToString().ToLowerInvariant()};ModelCachePath={ModelCachePath}");
}


/// <summary>
/// Provides extension methods for adding Foundry Local resources to the application model.
/// </summary>
public static class FoundryLocalExtensions
{
    /// <summary>
    /// Configures the Foundry Local resource with a specific model.
    /// </summary>
    /// <param name="builder">The Foundry Local resource builder.</param>
    /// <param name="model">The model name to use.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<FoundryLocalResource> WithModel(
        this IResourceBuilder<FoundryLocalResource> builder,
        string model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(model);

        builder.Resource.Model = model;
        return builder.WithEnvironment("AI_MODEL", model);
    }

    /// <summary>
    /// Configures the Foundry Local resource with a specific endpoint.
    /// </summary>
    /// <param name="builder">The Foundry Local resource builder.</param>
    /// <param name="endpoint">The endpoint URL to use.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<FoundryLocalResource> WithEndpoint(
        this IResourceBuilder<FoundryLocalResource> builder,
        string endpoint)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        builder.Resource.Endpoint = endpoint;
        return builder.WithEnvironment("AI_ENDPOINT", endpoint);
    }

    /// <summary>
    /// Configures the Foundry Local resource with a specific model cache path.
    /// </summary>
    /// <param name="builder">The Foundry Local resource builder.</param>
    /// <param name="cachePath">The path where models are cached.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<FoundryLocalResource> WithModelCachePath(
        this IResourceBuilder<FoundryLocalResource> builder,
        string cachePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(cachePath);

        builder.Resource.ModelCachePath = cachePath;
        return builder.WithEnvironment("FOUNDRY_LOCAL_MODEL_CACHE_PATH", cachePath);
    }

    /// <summary>
    /// Configures whether Foundry Local should automatically start the model.
    /// </summary>
    /// <param name="builder">The Foundry Local resource builder.</param>
    /// <param name="autoStart">Whether to automatically start the model.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<FoundryLocalResource> WithAutoStart(
        this IResourceBuilder<FoundryLocalResource> builder,
        bool autoStart = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.AutoStart = autoStart;
        return builder.WithEnvironment("FOUNDRY_LOCAL_AUTO_START", autoStart.ToString().ToLowerInvariant());
    }
}

/// <summary>
/// Health check for Foundry Local that always returns healthy.
/// </summary>
public class FoundryLocalHealthCheck : IHealthCheck
{
    private readonly FoundryLocalResource _resource;

    public FoundryLocalHealthCheck(FoundryLocalResource resource)
    {
        _resource = resource;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) => Task.FromResult(HealthCheckResult.Healthy($"Foundry Local '{_resource.Name}' with model '{_resource.Model}' at endpoint '{_resource.Endpoint}' is ready"));
}
