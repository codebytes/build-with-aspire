var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice");

builder.AddProject<Projects.BuildWithAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
