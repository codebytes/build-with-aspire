using Aspire.Hosting;
using BuildWithAspire.AppHost.Extensions;

namespace BuildWithAspire.Tests.Integration;

/// <summary>
/// Test-specific AppHost that configures resources for testing scenarios.
/// Avoids persistent volumes and uses test-friendly configurations.
/// </summary>
public class TestAppHost
{
    public static IDistributedApplicationBuilder CreateBuilder(string[]? args = null)
    {
        var builder = DistributedApplication.CreateBuilder(args ?? []);

        // Add PostgreSQL database without persistent volume for testing
        var postgres = builder.AddPostgres("postgres")
            .WithEnvironment("POSTGRES_DB", "testdb")
            .WithEnvironment("POSTGRES_USER", "testuser")
            .WithEnvironment("POSTGRES_PASSWORD", "testpass");

        var chatDb = postgres.AddDatabase("chatdb");

        // Add AI model service with test configuration
        var aiService = builder.AddAIModel();

        // Add API service with test configuration
        var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
            .WithReference(chatDb)
            .WaitFor(postgres)
            .WithAIModel(aiService)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing");

        builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithReference(apiService)
            .WaitFor(apiService)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing");

        return builder;
    }
}
