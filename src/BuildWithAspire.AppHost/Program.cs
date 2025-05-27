using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Get AI configuration from settings
var aiType = builder.Configuration["AI:Type"] ?? "ollama";
var chatDeploymentName = builder.Configuration["AI:ChatDeploymentName"] ?? "chat";

// Azure OpenAI configuration
var openai = builder.AddAzureOpenAI("openai")
    .AddDeployment(new AzureOpenAIDeployment(chatDeploymentName, "gpt-4o", "2024-11-20", "GlobalStandard", 10));

// Ollama configuration
var ollama = builder.AddOllama("ollama")
                .WithDataVolume()
                .WithOpenWebUI()
                //.WithContainerRuntimeArgs("--gpus=all")
                .AddModel("chat", "llama3.2");

// Select AI provider based on configuration
IResourceBuilder<IResourceWithConnectionString> chat;
switch (aiType.ToLower())
{
    case "azureopenai":
        chat = openai;
        break;
    case "ollama":
        chat = ollama;
        break;
    case "github":
        // Use connection string for GitHub endpoint
        var githubBaseUrl = builder.Configuration["AI:GitHub:BaseUrl"] ?? "https://api.github.com";
        var githubModelId = builder.Configuration["AI:GitHub:ModelId"] ?? "codify";
        chat = builder.AddConnectionString("github", $"BaseUrl={githubBaseUrl};ModelId={githubModelId}");
        break;
    case "foundry":
        // Use connection string for Foundry endpoint
        var foundryBaseUrl = builder.Configuration["AI:Foundry:BaseUrl"] ?? "http://localhost:8080";
        var foundryModelId = builder.Configuration["AI:Foundry:ModelId"] ?? "llama3";
        chat = builder.AddConnectionString("foundry", $"BaseUrl={foundryBaseUrl};ModelId={foundryModelId}");
        break;
    default:
        chat = ollama; // Default to Ollama
        break;
}

var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithEnvironment("AI:ChatDeploymentName", chatDeploymentName)
    .WithEnvironment("AI:Type", aiType.ToLower())
    .WithReference(chat, chatDeploymentName)
    .WaitFor(chat);

builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
