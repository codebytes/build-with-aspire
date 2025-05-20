using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var useLocalAI = builder.Configuration.GetValue<bool>("UseLocalAI");
var chatDeploymentName = builder.Configuration["chatDeploymentName"] ?? "chat";

var openai = builder.AddAzureOpenAI("openai")
    .AddDeployment(new AzureOpenAIDeployment(chatDeploymentName, "gpt-4o", "2024-11-20", "GlobalStandard", 10));

var ollama = builder.AddOllama("ollama")
                .WithDataVolume()
                .WithOpenWebUI()
                //.WithContainerRuntimeArgs("--gpus=all")
                .AddModel("chat", "llama3.2");

IResourceBuilder<IResourceWithConnectionString> chat = useLocalAI ? ollama : openai;

var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithEnvironment("AI:ChatDeploymentName", chatDeploymentName)
    .WithEnvironment("AI:Type", useLocalAI ? "ollama" : "azureOpenAi")
    .WithReference(chat, chatDeploymentName)
    .WaitFor(chat);

builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
