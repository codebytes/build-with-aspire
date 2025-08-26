using BuildWithAspire.AppHost.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// Explicitly document or enforce environment-specific configuration loading
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure Azure provisioning with subscription and resource group from user secrets
// Explicitly bind Azure configuration from user secrets to the Azure provisioning options
builder.Services.Configure<Aspire.Hosting.Azure.AzureProvisioningOptions>(
    builder.Configuration.GetSection("Azure"));

builder.AddAzureProvisioning();

// Add PostgreSQL database - use Azure PostgreSQL when publishing, local when developing
IResourceBuilder<IResourceWithConnectionString>? chatDb = builder.ExecutionContext.IsPublishMode
    ? builder
        .AddAzurePostgresFlexibleServer("postgres")
        .AddDatabase("chatdb")
    : builder
        .AddPostgres("postgres",
            password: builder.AddParameter("postgres-password", "aspire123!", secret: true))
        .WithDataVolume()
        .AddDatabase("chatdb");

// Add API service with AI model configuration
var aiService = builder.AddAIModel();

var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithAIModel(aiService)
    .WithReference(chatDb)
    .WaitFor(chatDb);

// MCP Server runs as a standalone HTTP service with weather endpoints
var mcpServer = builder.AddProject<Projects.BuildWithAspire_MCPServer>("mcpserver")
    .WithHttpEndpoint(port: 5267, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Ensure API depends on MCP server for service discovery and startup ordering
// Pass MCP server connection to API service for proper service-to-service communication
apiService = apiService
    .WithReference(mcpServer) // This creates service discovery for MCP server
    .WithEnvironment("MCP:ServerEnabled", "true")
    .WaitFor(mcpServer);

// Add Web service
var _ = builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
