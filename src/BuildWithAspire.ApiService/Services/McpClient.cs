using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text.Json;
using System.Text.Json.Serialization;
using McpTool = ModelContextProtocol.Protocol.Tool;
using McpCallToolResult = ModelContextProtocol.Protocol.CallToolResult;

namespace BuildWithAspire.ApiService.Services;

// Simple wrapper types to maintain API compatibility
public sealed class Tool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? InputSchema { get; set; } // JSON schema for tool parameters
}

public sealed class CallToolResult
{
    [JsonPropertyName("Content")]
    public McpContent[] Content { get; set; } = Array.Empty<McpContent>();

    [JsonPropertyName("IsError")]
    public bool IsError { get; set; }
}

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    public McpContent()
    {
    }

    public McpContent(string text)
    {
        Type = "text";
        Text = text;
    }
}

// Keep backward compatibility
public sealed class McpTextContent : McpContent
{
    public McpTextContent() : base()
    {
    }

    public McpTextContent(string text) : base(text)
    {
    }
}

/// <summary>
/// MCP-compliant client using the official C# SDK for dynamic tool discovery.
/// Connects to MCP server over HTTPS/SSE and loads tools dynamically.
/// </summary>
public interface IMcpClient : IAsyncDisposable
{
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    Task<Tool[]> ListToolsAsync(CancellationToken cancellationToken = default);
    Task<IList<AIFunction>> ListAIFunctionsAsync(CancellationToken cancellationToken = default);
    Task<CallToolResult> CallToolAsync(string toolName, object? parameters = null, CancellationToken cancellationToken = default);
    Task<string> GetWeatherInfoAsync(string request, CancellationToken cancellationToken = default);
}

public sealed class McpClient : IMcpClient
{
    private readonly ILogger<McpClient> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private ModelContextProtocol.Client.McpClient? _mcpClient;
    private bool _isInitialized;
    private Tool[] _cachedTools = Array.Empty<Tool>();

    public McpClient(
        ILogger<McpClient> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("mcpserver");
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return true;
        }

        try
        {
            _logger.LogInformation("Initializing MCP Client connection to server");

            // DEBUG: Log ALL configuration keys to understand what Aspire is injecting
            var allKeys = _configuration.AsEnumerable()
                .Where(k => !string.IsNullOrEmpty(k.Key))
                .OrderBy(k => k.Key)
                .ToList();

            _logger.LogInformation("Total configuration keys: {Count}", allKeys.Count);

            // Log service-related keys
            var serviceKeys = allKeys.Where(k => k.Key.Contains("service", StringComparison.OrdinalIgnoreCase)).ToList();
            _logger.LogInformation("Found {Count} service-related configuration keys", serviceKeys.Count);
            foreach (var key in serviceKeys.Take(30))
            {
                _logger.LogInformation("Config Key: {Key} = {Value}", key.Key, key.Value ?? "(null)");
            }

            // Get the MCP server URL from Aspire service discovery
            // Try multiple possible formats that Aspire might use
            var mcpServerUrl =
                // Standard Aspire format with double underscores
                _configuration["services__mcpserver__http__0"] ??
                _configuration["services__mcpserver__https__0"] ??
                // Alternative format with single underscores
                _configuration["services_mcpserver_http_0"] ??
                _configuration["services_mcpserver_https_0"] ??
                // Colon format (legacy)
                _configuration["services:mcpserver:http:0"] ??
                _configuration["services:mcpserver:https:0"] ??
                // Direct endpoint format
                _configuration["mcpserver:http:0"] ??
                _configuration["mcpserver:https:0"] ??
                // Connection string format
                _configuration.GetConnectionString("mcpserver");

            if (string.IsNullOrEmpty(mcpServerUrl))
            {
                _logger.LogError("MCP server URL not found in configuration. Service discovery may not be working.");
                _logger.LogWarning("Tried multiple configuration key formats");

                // Log ALL keys containing 'mcp' for debugging
                var mcpKeys = allKeys.Where(k => k.Key.Contains("mcp", StringComparison.OrdinalIgnoreCase)).ToList();
                if (mcpKeys.Any())
                {
                    _logger.LogInformation("Found {Count} keys containing 'mcp':", mcpKeys.Count);
                    foreach (var key in mcpKeys)
                    {
                        _logger.LogInformation("  {Key} = {Value}", key.Key, key.Value ?? "(null)");
                    }
                }
                else
                {
                    _logger.LogError("NO configuration keys found containing 'mcp'. Service reference may be missing in AppHost.");
                }

                return false;
            }

            _logger.LogInformation("Using Aspire service discovery URL for MCP server: {McpServerUrl}", mcpServerUrl);

            // Use root endpoint for Streamable HTTP transport (not /sse)
            // The MCP server uses Streamable HTTP which communicates via POST to /
            var endpoint = new Uri(mcpServerUrl.TrimEnd('/') + "/");
            _logger.LogInformation("Connecting to MCP server via Streamable HTTP: {Endpoint}", endpoint);

            // Create HTTP transport for Streamable HTTP connection
            // NOTE: Do NOT use HttpTransportMode.Sse - the server uses Streamable HTTP (POST/GET)
            // Session management is handled automatically by the SDK via Mcp-Session-Id header
            var transport = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = endpoint,
                    Name = "BuildWithAspire API Client"
                    // TransportMode defaults to Streamable HTTP (POST/GET) when not specified
                },
                _httpClient,
                ownsHttpClient: false
            );

            // Create and connect MCP client
            _mcpClient = await ModelContextProtocol.Client.McpClient.CreateAsync(
                transport,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            _logger.LogInformation("MCP Client connected successfully to server: {ServerName} v{ServerVersion}",
                _mcpClient.ServerInfo.Name, _mcpClient.ServerInfo.Version);

            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP Client connection. Check if MCP server is running and accessible.");
            return false;
        }
    }

    public async Task<Tool[]> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _mcpClient == null)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_mcpClient == null)
        {
            _logger.LogWarning("MCP Client not initialized, returning empty tool list");
            return Array.Empty<Tool>();
        }

        try
        {
            // Use cached tools if available
            if (_cachedTools.Length > 0)
            {
                return _cachedTools;
            }

            // List tools from MCP server dynamically
            var mcpTools = await _mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            _cachedTools = mcpTools.Select(t =>
            {
                _logger.LogDebug("MCP Tool discovered: {ToolName} - {Description}", t.Name, t.Description);

                return new Tool
                {
                    Name = t.Name,
                    Description = t.Description ?? string.Empty,
                    // Note: InputSchema is embedded in the McpClientTool (AIFunction) metadata
                    // We don't need to extract it here - it will be used by the AI framework automatically
                    InputSchema = null
                };
            }).ToArray();

            _logger.LogInformation("Dynamically loaded {ToolCount} MCP tools from server", _cachedTools.Length);
            return _cachedTools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list MCP tools");
            return Array.Empty<Tool>();
        }
    }

    public async Task<IList<AIFunction>> ListAIFunctionsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _mcpClient == null)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_mcpClient == null)
        {
            _logger.LogWarning("MCP Client not initialized, returning empty AIFunction list");
            return Array.Empty<AIFunction>();
        }

        try
        {
            // Use the MCP SDK's ListToolsAsync which returns McpClientTool instances
            // McpClientTool inherits from AIFunction, so we can use them directly!
            var mcpClientTools = await _mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Retrieved {ToolCount} MCP tools as AIFunctions from server", mcpClientTools.Count);

            // McpClientTool already implements AIFunction, so we can return directly
            return mcpClientTools.Cast<AIFunction>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list MCP tools as AIFunctions");
            return Array.Empty<AIFunction>();
        }
    }

    public async Task<CallToolResult> CallToolAsync(string toolName, object? parameters = null, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _mcpClient == null)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_mcpClient == null)
        {
            _logger.LogWarning("MCP Client not initialized");
            return new CallToolResult
            {
                Content = [new McpContent("MCP Client not initialized")],
                IsError = true
            };
        }

        try
        {
            _logger.LogInformation("Calling MCP tool: {ToolName}", toolName);

            // Convert parameters object to dictionary for MCP
            var paramDict = new Dictionary<string, object?>();
            if (parameters != null)
            {
                var paramsJson = JsonSerializer.Serialize(parameters);
                paramDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(paramsJson) ?? new();
            }

            // Call tool on MCP server
            var mcpResult = await _mcpClient.CallToolAsync(
                toolName,
                paramDict,
                progress: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // Convert MCP result to our CallToolResult format
            var content = mcpResult.Content.Select(c =>
            {
                if (c is ModelContextProtocol.Protocol.TextContentBlock textBlock)
                {
                    return new McpContent(textBlock.Text);
                }
                return new McpContent(c.ToString() ?? string.Empty);
            }).ToArray();

            return new CallToolResult
            {
                Content = content,
                IsError = mcpResult.IsError == true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MCP tool: {ToolName}", toolName);
            return new CallToolResult
            {
                Content = [new McpContent($"Error calling tool: {ex.Message}")],
                IsError = true
            };
        }
    }

    public async Task<string> GetWeatherInfoAsync(string request, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _mcpClient == null)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            // Determine which weather tool to call based on request
            // Use camelCase names as per MCP SDK conventions
            var toolName = request.ToLowerInvariant().Contains("forecast")
                ? "getWeatherForecast"
                : "getCurrentWeather";

            var result = await CallToolAsync(toolName, null, cancellationToken).ConfigureAwait(false);

            if (result.IsError)
            {
                return "Unable to fetch weather information";
            }

            return result.Content.FirstOrDefault()?.Text ?? "No weather data available";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weather information");
            return "Error fetching weather information";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_mcpClient != null)
        {
            await _mcpClient.DisposeAsync().ConfigureAwait(false);
            _mcpClient = null;
        }

        GC.SuppressFinalize(this);
    }
}
