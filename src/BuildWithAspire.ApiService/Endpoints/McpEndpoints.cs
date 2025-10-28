using BuildWithAspire.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuildWithAspire.ApiService.Endpoints;

/// <summary>
/// MCP (Model Context Protocol) tool integration endpoints.
/// </summary>
public static class McpEndpoints
{
    public static RouteGroupBuilder MapMcpEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/mcp");

        group.MapPost("/call/{toolName}", CallTool)
            .WithName("CallMcpTool")
            .WithOpenApi()
            .RequireRateLimiting("weather");

        group.MapGet("/tools", ListTools)
            .WithName("ListMcpTools")
            .WithOpenApi();

        group.MapGet("/health", HealthCheck)
            .WithName("McpHealthCheck")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> CallTool(
        string toolName,
        [FromBody] Dictionary<string, object?>? parameters,
        IMcpClient mcpClient,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("MCP tool call: {ToolName}", toolName);

            var initialized = await mcpClient.InitializeAsync().ConfigureAwait(false);
            if (!initialized)
            {
                logger.LogError("Failed to initialize MCP client");
                return Results.Problem("MCP client initialization failed", statusCode: 503);
            }

            var result = await mcpClient.CallToolAsync(toolName, parameters).ConfigureAwait(false);
            logger.LogInformation("MCP tool {ToolName} completed. IsError: {IsError}", toolName, result.IsError);

            return result.IsError ? Results.BadRequest(result) : Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling MCP tool: {ToolName}", toolName);

            if (ex.Message.Contains("Unknown tool", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var availableTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
                    var toolNames = string.Join(", ", availableTools.Select(t => t.Name));
                    logger.LogWarning("Unknown tool '{ToolName}'. Available: {AvailableTools}", toolName, toolNames);
                    return Results.Problem($"Unknown tool '{toolName}'. Available: {toolNames}", statusCode: 404);
                }
                catch
                {
                    // Fall through to generic error
                }
            }

            return Results.Problem($"Error calling MCP tool: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> ListTools(IMcpClient mcpClient, ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("MCP tools list request");

            var initialized = await mcpClient.InitializeAsync().ConfigureAwait(false);
            if (!initialized)
            {
                logger.LogError("Failed to initialize MCP client");
                return Results.Problem("MCP client initialization failed", statusCode: 503);
            }

            var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            logger.LogInformation("Retrieved {ToolCount} MCP tools", tools.Length);

            return Results.Ok(tools);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing MCP tools");
            return Results.Problem($"Error listing MCP tools: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> HealthCheck(IMcpClient mcpClient, ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("MCP health check");

            var initialized = await mcpClient.InitializeAsync().ConfigureAwait(false);
            if (!initialized)
            {
                logger.LogWarning("MCP client initialization failed");
                return Results.Problem("MCP server connection failed", statusCode: 503, title: "Service Unavailable");
            }

            var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            logger.LogInformation("MCP health check passed. Tools: {ToolCount}", tools.Length);

            return Results.Ok(new
            {
                status = "healthy",
                mcpServerConnected = true,
                toolsAvailable = tools.Length,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MCP health check failed");
            return Results.Problem($"MCP health check failed: {ex.Message}", statusCode: 503, title: "Service Unavailable");
        }
    }
}
