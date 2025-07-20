using BuildWithAspire.McpServer.Tools;

namespace BuildWithAspire.McpServer;

/// <summary>
/// MCP tool call request structure.
/// </summary>
public class McpToolCallRequest
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// MCP Server that manages tool calls.
/// </summary>
public class McpServer
{
    private readonly Dictionary<string, Func<Dictionary<string, object>, CancellationToken, Task<object>>> _tools;
    private readonly Dictionary<string, (string Description, object Schema)> _toolMetadata;

    public McpServer(WeatherTool weatherTool, TimeTool timeTool)
    {
        _tools = new Dictionary<string, Func<Dictionary<string, object>, CancellationToken, Task<object>>>
        {
            [weatherTool.Name] = weatherTool.ExecuteAsync,
            [timeTool.Name] = timeTool.ExecuteAsync
        };

        _toolMetadata = new Dictionary<string, (string Description, object Schema)>
        {
            [weatherTool.Name] = (weatherTool.Description, weatherTool.GetParametersSchema()),
            [timeTool.Name] = (timeTool.Description, timeTool.GetParametersSchema())
        };
    }

    public async Task<object> CallToolAsync(McpToolCallRequest request)
    {
        if (!_tools.TryGetValue(request.Name, out var tool))
        {
            throw new InvalidOperationException($"Tool '{request.Name}' not found");
        }

        return await tool(request.Arguments, CancellationToken.None).ConfigureAwait(false);
    }

    public object GetAvailableTools()
    {
        return new
        {
            tools = _toolMetadata.Select(kvp => new
            {
                name = kvp.Key,
                description = kvp.Value.Description,
                inputSchema = kvp.Value.Schema
            }).ToList()
        };
    }
}