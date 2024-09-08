var builder = DistributedApplication.CreateBuilder(args);

var chatDeploymentName = "chat";
var openai = builder.AddAzureOpenAI("openai")
    .AddDeployment(new AzureOpenAIDeployment(chatDeploymentName, "gpt-4o", "2024-05-13", "GlobalStandard", 10));

var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);

builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
