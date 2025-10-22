using Microsoft.Extensions.AI;

namespace BuildWithAspire.ApiService.Services;

/// <summary>
/// Interface for converting MCP server tools dynamically to AIFunction instances
/// </summary>
public interface IDynamicMcpToolConverter
{
    /// <summary>
    /// Dynamically discovers all tools from the MCP server and converts them to AIFunction instances
    /// </summary>
    Task<IEnumerable<AIFunction>> GetAllToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the cached functions, forcing a re-discovery on the next call
    /// </summary>
    void ClearCache();
}
