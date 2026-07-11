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

// Add MCP server with HTTP/SSE transport following official SDK patterns
// The SDK uses StatefulSessionManager internally to handle session IDs via "Mcp-Session-Id" header
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "BuildWithAspire MCP Server",
        Version = "1.0.0"
    };
})
.WithHttpTransport(httpOptions =>
{
    // Configure session management following official SDK patterns
    // The SDK automatically handles session IDs via the "Mcp-Session-Id" header
    // Sessions are created on the first POST request and reused for subsequent requests
    httpOptions.IdleTimeout = TimeSpan.FromHours(2); // Default session timeout
    httpOptions.MaxIdleSessionCount = 10_000; // Maximum idle sessions to track

    // Optional: Configure per-session options
    // httpOptions.ConfigureSessionOptions = async (context, options, cancellationToken) =>
    // {
    //     // Customize McpServerOptions per session if needed
    // };

    // Optional: Custom session lifecycle handler
    // httpOptions.RunSessionHandler = async (context, mcpServer, cancellationToken) =>
    // {
    //     // Custom logic before/after session runs
    //     await mcpServer.RunAsync(cancellationToken);
    // };
})
.WithTools<WeatherTools>()
.WithTools<SystemTools>()
.WithTools<MathTools>()
.WithTools<TextTools>();

// Add logging
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Information;
});

var app = builder.Build();

// Map default endpoints for Aspire
app.MapDefaultEndpoints();

// Add health check endpoint for MCP client connectivity
app.MapGet("/health", () =>
{
    return Results.Ok(new {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        server = "MCP-Server",
        transport = "Streamable HTTP (with session management)",
        sessionSupport = "Yes - via Mcp-Session-Id header"
    });
})
.WithName("HealthCheck");

// Add diagnostic endpoint to show session and transport information
app.MapGet("/debug/info", () =>
{
    return Results.Ok(new {
        message = "MCP Server with Streamable HTTP Transport",
        transport = new
        {
            type = "Streamable HTTP",
            sessionManagement = "Stateful",
            sessionIdHeader = "Mcp-Session-Id",
            sseEndpoint = "/sse (Server-Sent Events for notifications)",
            messageEndpoint = "POST / (JSON-RPC requests/responses)"
        },
        sessionBehavior = new
        {
            creation = "First POST request creates a new session and returns Mcp-Session-Id header",
            reuse = "Subsequent requests with Mcp-Session-Id header reuse the same session",
            idleTimeout = "2 hours",
            maxIdleSessions = 10000
        },
        note = "Session IDs are managed automatically by the SDK's StatefulSessionManager"
    });
})
.WithName("DebugInfo");

// Add diagnostic endpoint to list registered tools (for debugging). Tool metadata is
// enumerated directly from the DI-registered McpServerTool instances, so this endpoint
// stays accurate automatically as tools are added or removed.
app.MapGet("/debug/tools", (IEnumerable<McpServerTool> registeredTools) =>
{
    var tools = registeredTools
        .Select(t => new
        {
            name = t.ProtocolTool.Name,
            title = t.ProtocolTool.Title,
            description = t.ProtocolTool.Description,
            annotations = t.ProtocolTool.Annotations is { } a
                ? new
                {
                    readOnly = a.ReadOnlyHint,
                    idempotent = a.IdempotentHint,
                    destructive = a.DestructiveHint,
                    openWorld = a.OpenWorldHint
                }
                : null
        })
        .OrderBy(t => t.name, StringComparer.Ordinal)
        .ToArray();

    return Results.Ok(new {
        message = "MCP tools registered with camelCase names (following official SDK conventions)",
        count = tools.Length,
        tools = tools,
        usage = new
        {
            protocol = "JSON-RPC 2.0",
            listTools = "POST / with method 'tools/list'",
            callTool = "POST / with method 'tools/call' and params: { name, arguments }",
            sessionId = "Include 'Mcp-Session-Id' header for session continuity"
        }
    });
})
.WithName("DebugTools");

// Add OpenAPI for development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map MCP endpoints following official SDK patterns:
// - POST / : JSON-RPC request/response endpoint (creates/reuses sessions via Mcp-Session-Id header)
// - GET /  : Server-Sent Events (SSE) endpoint for server-to-client notifications
// - DELETE / : Session cleanup endpoint
// The SDK automatically handles session management via StatefulSessionManager
app.MapMcp();

app.Logger.LogInformation("=== BuildWithAspire MCP Server ===");
app.Logger.LogInformation("Transport: Streamable HTTP with session management");
app.Logger.LogInformation("Session ID Header: Mcp-Session-Id (managed by SDK)");
app.Logger.LogInformation("Endpoints:");
app.Logger.LogInformation("  - POST /   : JSON-RPC requests (auto-creates session on first request)");
app.Logger.LogInformation("  - GET /    : SSE notifications stream");
app.Logger.LogInformation("  - DELETE / : Session cleanup");
app.Logger.LogInformation("Tools: Weather, System, Math, Text");
app.Logger.LogInformation("Session Timeout: 2 hours idle");

// Run the MCP-compliant server
app.Run();
