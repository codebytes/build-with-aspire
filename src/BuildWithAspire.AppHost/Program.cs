var builder = DistributedApplication.CreateBuilder(args);

var openAI = builder.AddAzureOpenAI("openai")
    .AddDeployment(new AzureOpenAIDeployment("chat", "gpt-4o", "2024-05-13", "Standard", 10));
    //.AddDeployment(new AzureOpenAIDeployment("chat", "gpt-4", "turbo-2024-04-09", "Standard", 10));
    //.AddDeployment(new AzureOpenAIDeployment("chat", "gpt-35-turbo", "0125", "Standard", 10));

var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithReference(openAI);

builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
