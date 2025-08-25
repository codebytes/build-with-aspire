using BuildWithAspire.MCPServer.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add common service defaults for Aspire integration
builder.AddServiceDefaults();

// Add MCP Server using official SDK pattern
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<WeatherTools>()
    .WithTools<SystemTools>()
    .WithTools<MathTools>();

// Add logging
builder.Logging.AddConsole();

var app = builder.Build();

// Map default endpoints for Aspire
app.MapDefaultEndpoints();

// Map MCP endpoints using official SDK
app.MapMcp();

// Run the MCP-compliant server
app.Run();
