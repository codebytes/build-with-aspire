var builder = DistributedApplication.CreateBuilder(args);

var deploymentName = "chat";
var openai = builder.AddAzureOpenAI("openai")
    .AddDeployment(new AzureOpenAIDeployment(deploymentName, "gpt-4o", "2024-05-13", "GlobalStandard", 10));


var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithReference(openai)
    .WithEnvironment("AI_DeploymentName", deploymentName);

builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
