using BuildWithAspire.AppHost.Extensions;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var chatDb = postgres.AddDatabase("chatdb");

// Add AI model service based on configuration
var aiService = builder.AddAIModel();

// Add API service with AI model configuration
var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithReference(chatDb)
    .WaitFor(postgres)
    .WithAIModel(aiService);

builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
