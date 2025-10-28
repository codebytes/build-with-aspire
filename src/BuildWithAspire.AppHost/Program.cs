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
// For local development, use a non-secret parameter with a default value so Aspire CLI won't prompt
var postgresPassword = builder.AddParameter("postgres-password", "aspire123!", secret: false);

IResourceBuilder<IResourceWithConnectionString>? chatDb = builder.ExecutionContext.IsPublishMode
    ? builder
        .AddAzurePostgresFlexibleServer("postgres")
        .AddDatabase("chatdb")
    : builder
        .AddPostgres("postgres", password: postgresPassword)
        .WithDataVolume()
        .AddDatabase("chatdb");

postgresPassword.WithParentRelationship(chatDb);

// Always add AI resource (FoundryLocal returns a simple connection string resource)
var aiService = builder.AddAIModel();

// Add MCP Server for tools integration
// Ports are defined in launchSettings.json (8080 http, 8081 https)
var mcpServer = builder.AddProject<Projects.BuildWithAspire_MCPServer>("mcpserver");

// Add API Service
// Ports are defined in launchSettings.json (5020 http, 7287 https)
var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithAIModel(aiService)
    .WithReference(chatDb)
    .WithReference(mcpServer)
    .WaitFor(chatDb)
    .WaitFor(mcpServer);

// Add Web service
var _ = builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
