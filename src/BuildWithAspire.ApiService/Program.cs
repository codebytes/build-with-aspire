using BuildWithAspire.ApiService.Data;
using BuildWithAspire.ApiService.Endpoints;
using BuildWithAspire.ApiService.Extensions;
using BuildWithAspire.ApiService.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ChatDbContext>("chatdb");
builder.AddAIServices();

// Add HTTP client for MCP server
builder.Services.AddHttpClient("mcpserver").AddServiceDiscovery();

// Register MCP services
builder.Services.AddSingleton<IMcpClient, McpClient>();
builder.Services.AddSingleton<IDynamicMcpToolConverter, DynamicMcpToolConverter>();

// Add rate limiting
builder.Services.AddApiRateLimiting();

// Add OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "BuildWithAspire API";
        document.Info.Description = "API with AI chat capabilities";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

builder.Services.AddTransient<ChatService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("BuildWithAspire API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();
app.UseRateLimiter();

// Apply database migrations
await app.ApplyMigrationsAsync().ConfigureAwait(false);

// Map all endpoints
app.MapWeatherEndpoints();
app.MapMcpEndpoints();
app.MapChatEndpoints();

app.Run();

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make Program accessible for testing
public partial class Program { }
