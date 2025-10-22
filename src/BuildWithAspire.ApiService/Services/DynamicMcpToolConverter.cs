using Microsoft.Extensions.AI;

namespace BuildWithAspire.ApiService.Services;

/// <summary>
/// Converts MCP server tools dynamically to AIFunction instances for the Microsoft Agent Framework.
/// This allows tools to be discovered and used without hardcoding them in the API service.
/// Following MCP SDK best practices by using McpClientTool which inherits from AIFunction.
/// </summary>
public class DynamicMcpToolConverter : IDynamicMcpToolConverter
{
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<DynamicMcpToolConverter> _logger;
    private IEnumerable<AIFunction>? _cachedFunctions;

    public DynamicMcpToolConverter(IMcpClient mcpClient, ILogger<DynamicMcpToolConverter> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    /// <summary>
    /// Dynamically discovers all tools from the MCP server as AIFunction instances.
    /// Uses the MCP SDK's McpClientTool which inherits from AIFunction, following best practices.
    /// </summary>
    public async Task<IEnumerable<AIFunction>> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        // Return cached functions if available
        if (_cachedFunctions != null)
        {
            return _cachedFunctions;
        }

        try
        {
            // Use the MCP client's method that returns McpClientTool instances (which inherit from AIFunction)
            // This follows the MCP SDK best practices from https://github.com/modelcontextprotocol/csharp-sdk
            var aiFunctions = await _mcpClient.ListAIFunctionsAsync(cancellationToken).ConfigureAwait(false);

            _cachedFunctions = aiFunctions;

            _logger.LogInformation("Successfully loaded {FunctionCount} MCP tools as AIFunctions from server", aiFunctions.Count);

            return _cachedFunctions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover MCP tools as AIFunctions");
            return Enumerable.Empty<AIFunction>();
        }
    }

    /// <summary>
    /// Clears the cached functions, forcing a refresh on next call
    /// </summary>
    public void ClearCache()
    {
        _cachedFunctions = null;
        _logger.LogInformation("Cleared cached AIFunctions, will refresh from MCP server on next request");
    }
}
