using System.Text.Json;

namespace BuildWithAspire.ApiService.Services;

/// <summary>
/// Service to interact with MCP tools.
/// </summary>
public class McpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpClientService> _logger;

    public McpClientService(HttpClient httpClient, ILogger<McpClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get available MCP tools.
    /// </summary>
    public async Task<McpToolsResponse?> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching available MCP tools");
            var response = await _httpClient.GetAsync("/mcp/tools", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<McpToolsResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            _logger.LogInformation("Retrieved {ToolCount} MCP tools", result?.Tools?.Count ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available MCP tools");
            return null;
        }
    }

    /// <summary>
    /// Call an MCP tool.
    /// </summary>
    public async Task<object?> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling MCP tool: {ToolName} with {ArgumentCount} arguments", toolName, arguments.Count);
            
            var request = new McpToolCallRequest 
            { 
                Name = toolName, 
                Arguments = arguments 
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/mcp/tools/call", content, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<object>(responseContent);
            
            _logger.LogInformation("Successfully called MCP tool: {ToolName}", toolName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MCP tool: {ToolName}", toolName);
            throw new InvalidOperationException($"Failed to call MCP tool '{toolName}': {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Response structure for MCP tools listing.
/// </summary>
public class McpToolsResponse
{
    public List<McpToolInfo> Tools { get; set; } = new();
}

/// <summary>
/// Information about an MCP tool.
/// </summary>
public class McpToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? InputSchema { get; set; }
}

/// <summary>
/// Request structure for calling MCP tools.
/// </summary>
public class McpToolCallRequest
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
}