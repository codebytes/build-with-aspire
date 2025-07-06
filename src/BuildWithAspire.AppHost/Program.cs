using BuildWithAspire.AppHost.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = DistributedApplication.CreateBuilder(args);

// Explicitly document or enforce environment-specific configuration loading
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Configure Azure provisioning with subscription and resource group from user secrets  
// Explicitly bind Azure configuration from user secrets to the Azure provisioning options
builder.Services.Configure<Aspire.Hosting.Azure.AzureProvisioningOptions>(
    builder.Configuration.GetSection("Azure"));

builder.AddAzureProvisioning();

// Add PostgreSQL database - use Azure PostgreSQL when publishing, local when developing
IResourceBuilder<IResourceWithConnectionString> chatDb;
if (builder.ExecutionContext.IsPublishMode)
{
    var azurePostgres = builder.AddAzurePostgresFlexibleServer("postgres");
    chatDb = azurePostgres.AddDatabase("chatdb");
}
else
{
    var localPostgres = builder.AddPostgres("postgres", password: builder.AddParameter("postgres-password", "aspire123!", secret: true))
        .WithDataVolume();
    chatDb = localPostgres.AddDatabase("chatdb");
}

// Add AI model service based on configuration
var aiService = builder.AddAIModel();

// Add API service with AI model configuration
var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithReference(chatDb)
    .WaitFor(chatDb)
    .WithAIModel(aiService);

builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
