using BuildWithAspire.MCPServer.Tools;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add common service defaults for Aspire integration
builder.AddServiceDefaults();

// Configure AI services for weather descriptions (optional)
builder.Services.AddSingleton<IChatClient>(provider =>
{
    // For development, we'll use a simple mock or null client
    // In production, this would connect to your AI service
    return null!; // WeatherTools handles null gracefully
});

// Register MCP tool classes
builder.Services.AddSingleton<WeatherTools>();
builder.Services.AddSingleton<SystemTools>();
builder.Services.AddSingleton<MathTools>();

// Add logging
builder.Logging.AddConsole();

var app = builder.Build();

// Map default endpoints for Aspire
app.MapDefaultEndpoints();

// Add health check endpoint for MCP client connectivity
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow, server = "MCP-Server" });
})
.WithName("HealthCheck")
.WithOpenApi();

// Add OpenAPI for development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Logger.LogInformation("MCP server configured successfully with weather, system, and math tools");

// Run the MCP-compliant server
app.Run();
