// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildWithAspire.AppHost.Extensions;

/// <summary>
/// Represents a GitHub Models resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="model">The model name.</param>
public class GitHubModelsResource(string name, string model) : Resource(name), IResourceWithConnectionString, IResourceWithEnvironment, IResourceWithoutLifetime
{
    internal const string GitHubModelsEndpoint = "https://models.inference.ai.azure.com";

    /// <summary>
    /// Gets or sets the model name, e.g., "gpt-4o-mini".
    /// </summary>
    public string Model { get; set; } = model;

    /// <summary>
    /// Gets or sets the API key for accessing GitHub Models.
    /// </summary>
    /// <remarks>
    /// If not set, the value will be retrieved from the environment variable GITHUB_TOKEN.
    /// </remarks>
    public ParameterResource? Key { get; set; }

    /// <summary>
    /// Gets the connection string expression for the GitHub Models resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint={GitHubModelsEndpoint};Key={Key?.ToString() ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GitHub Models API key is not configured. Set GITHUB_TOKEN environment variable or configure the github-token parameter.")};Model={Model};DeploymentId={Model}");
}


/// <summary>
/// Provides extension methods for adding GitHub Models resources to the application model.
/// </summary>
public static class GitHubModelsExtensions
{
    /// <summary>
    /// Configures the GitHub Models resource to use a specific model.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="model">The model name (e.g., "gpt-4o-mini").</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<GitHubModelsResource> WithModel(this IResourceBuilder<GitHubModelsResource> builder, string model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(model);

        builder.Resource.Model = model;
        return builder.WithEnvironment("AI_MODEL", model);
    }

    /// <summary>
    /// Configures the GitHub Models resource to use a specific API key.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="apiKey">The API key parameter or value.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<GitHubModelsResource> WithApiKey(this IResourceBuilder<GitHubModelsResource> builder, IResourceBuilder<ParameterResource> apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);

        builder.Resource.Key = apiKey.Resource;
        return builder.WithEnvironment("GITHUB_TOKEN", apiKey);
    }

    /// <summary>
    /// Configures the GitHub Models resource to use a specific API key.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="apiKey">The API key value.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<GitHubModelsResource> WithApiKey(this IResourceBuilder<GitHubModelsResource> builder, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        return builder.WithEnvironment("GITHUB_TOKEN", apiKey);
    }
}
