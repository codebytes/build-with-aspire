using BuildWithAspire.McpServer;
using BuildWithAspire.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

// Add MCP Server
builder.Services.AddSingleton<WeatherTool>();
builder.Services.AddSingleton<TimeTool>();
builder.Services.AddSingleton<McpServer>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "MCP Server is running");

// Add MCP endpoints
app.MapPost("/mcp/tools/call", async (McpToolCallRequest request, McpServer mcpServer) =>
{
    try
    {
        var result = await mcpServer.CallToolAsync(request).ConfigureAwait(false);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Tool call failed: {ex.Message}");
    }
});

app.MapGet("/mcp/tools", (McpServer mcpServer) =>
{
    return Results.Ok(mcpServer.GetAvailableTools());
});

app.Run();

// Make Program class accessible for testing
public partial class Program { }