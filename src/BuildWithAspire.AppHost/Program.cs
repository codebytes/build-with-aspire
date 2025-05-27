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
    case "foundry":
        // For GitHub and Foundry, we'll handle configuration directly in the API service
        // Create a dummy resource to maintain the same structure
        chat = builder.AddConnectionString("ai-connection");
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
