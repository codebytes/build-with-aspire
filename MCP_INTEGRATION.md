# Model Context Protocol (MCP) Integration in Aspire

## Overview

This project demonstrates the integration of Model Context Protocol (MCP) with .NET Aspire, providing a standardized way for AI models to discover and use tools dynamically. MCP enables interoperable AI model hosting and serving by providing a consistent interface for tool discovery and execution.

## What is MCP?

Model Context Protocol (MCP) is a standard that:
- Enables AI models to discover available tools dynamically
- Provides a standardized interface for calling tools
- Allows for interoperable AI model hosting and serving
- Supports tool composition and chaining

## Benefits of Using MCP in Aspire

### 1. **Dynamic Tool Discovery**
- AI models can discover available tools at runtime
- No need to hardcode tool endpoints or interfaces
- Tools can be added or removed without changing AI model code

### 2. **Service Orchestration**
- Aspire manages MCP server lifecycle and discovery
- Built-in health checks and monitoring
- Automatic service resolution and load balancing

### 3. **Scalability**
- MCP servers can be scaled independently
- Multiple tool instances can be deployed
- Built-in telemetry and observability

### 4. **Developer Experience**
- Standardized tool development patterns
- Easy testing and debugging
- Consistent API documentation

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   AI Service    │────│   MCP Server    │────│   Tool Suite    │
│                 │    │                 │    │                 │
│ - Chat Service  │    │ - Tool Registry │    │ - Weather Tool  │
│ - AI Models     │    │ - Request/Resp  │    │ - Time Tool     │
│ - MCP Client    │    │ - Validation    │    │ - Custom Tools  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                    ┌─────────────────┐
                    │ Aspire AppHost  │
                    │                 │
                    │ - Service Disc. │
                    │ - Health Checks │
                    │ - Telemetry     │
                    └─────────────────┘
```

## Implementation Details

### MCP Server (`BuildWithAspire.McpServer`)

The MCP Server hosts available tools and provides the following endpoints:

#### Available Tools
```http
GET /mcp/tools
```
Returns a list of all available tools with their descriptions and schemas.

#### Call Tool
```http
POST /mcp/tools/call
Content-Type: application/json

{
  "name": "get_weather",
  "arguments": {
    "location": "New York",
    "days": 3
  }
}
```

### Available Tools

#### 1. Weather Tool (`get_weather`)
Provides weather forecasts for specified locations.

**Parameters:**
- `location` (string, required): The location to get weather for
- `days` (integer, optional): Number of days to forecast (1-7, default: 5)

**Example Usage:**
```json
{
  "name": "get_weather",
  "arguments": {
    "location": "San Francisco",
    "days": 3
  }
}
```

#### 2. Time Tool (`get_time`)
Provides current date and time information with timezone support.

**Parameters:**
- `timezone` (string, optional): Timezone name (default: "UTC")
- `format` (string, optional): Format type - "iso", "local", "unix", or "detailed" (default: "iso")

**Example Usage:**
```json
{
  "name": "get_time",
  "arguments": {
    "timezone": "America/New_York",
    "format": "detailed"
  }
}
```

### AI Service Integration

The API Service (`BuildWithAspire.ApiService`) includes:

1. **MCP Client Service**: Communicates with the MCP Server
2. **Smart Tool Detection**: Automatically detects when users ask for weather or time information
3. **Context Enrichment**: Adds tool results to AI conversation context

#### Intelligent Tool Usage

The AI service analyzes user messages and automatically calls appropriate MCP tools:

- **Weather queries**: "What's the weather in Paris?" → calls `get_weather` tool
- **Time queries**: "What time is it?" → calls `get_time` tool
- **Regular chat**: Processes normally without tool calls

### Aspire Integration

In `BuildWithAspire.AppHost`, the MCP Server is registered as:

```csharp
// Add MCP Server service
var mcpServer = builder.AddProject<Projects.BuildWithAspire_McpServer>("mcpserver")
    .WithExternalHttpEndpoints();

// Reference MCP Server from API Service
var apiService = builder.AddProject<Projects.BuildWithAspire_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithAIModel(aiService)
    .WithReference(chatDb)
    .WithReference(mcpServer)  // Enables service discovery
    .WaitFor(chatDb)
    .WaitFor(mcpServer);
```

## Testing the MCP Implementation

### 1. Demo Endpoint
Visit `/mcp/demo` to see all available tools and example calls:

```http
GET https://localhost:5001/mcp/demo
```

### 2. Chat Integration
Create a conversation and ask for weather or time:

```http
POST https://localhost:5001/conversations/{id}/messages
Content-Type: application/json

{
  "message": "What's the weather like in London for the next 3 days?"
}
```

### 3. Direct Tool Calls
Call MCP tools directly through the MCP server:

```http
POST https://localhost:5124/mcp/tools/call
Content-Type: application/json

{
  "name": "get_weather",
  "arguments": {
    "location": "Tokyo",
    "days": 5
  }
}
```

## Extending the MCP Implementation

### Adding New Tools

1. **Create Tool Class**: Implement the tool interface in `Tools/` directory
2. **Register Tool**: Add to `McpServer` constructor
3. **Update Documentation**: Add tool description and examples

Example tool structure:
```csharp
public class CustomTool
{
    public string Name => "tool_name";
    public string Description => "Tool description";
    
    public object GetParametersSchema() => new { /* schema */ };
    
    public async Task<object> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        // Tool implementation
    }
}
```

### AI Model Integration

To integrate with different AI providers:

1. **Tool Calling**: Use AI model's native tool calling capabilities
2. **Schema Conversion**: Convert MCP schemas to AI-specific formats
3. **Response Processing**: Parse tool results and integrate into conversations

## Best Practices

### 1. **Tool Design**
- Keep tools focused and single-purpose
- Provide comprehensive parameter validation
- Include detailed error handling
- Use appropriate timeout values

### 2. **Error Handling**
- Graceful degradation when tools are unavailable
- Meaningful error messages for users
- Proper logging and telemetry

### 3. **Performance**
- Cache tool metadata when possible
- Use async patterns throughout
- Implement proper timeout and cancellation support

### 4. **Security**
- Validate all tool parameters
- Implement rate limiting if needed
- Sanitize tool outputs before returning to AI

## Monitoring and Observability

The MCP implementation includes:

- **Health Checks**: Built-in Aspire health monitoring
- **Telemetry**: OpenTelemetry integration for tracing
- **Logging**: Structured logging throughout the pipeline
- **Metrics**: Tool call success rates and performance metrics

## Migration from Hardcoded Tools

The original weather endpoint (`/weatherforecast`) remains available, demonstrating both approaches:

- **Legacy**: Direct API endpoints with hardcoded functionality
- **MCP**: Dynamic tool discovery and execution

This allows for gradual migration and comparison of approaches.

## Future Enhancements

Potential extensions to the MCP implementation:

1. **Tool Chaining**: Allow tools to call other tools
2. **Dynamic Loading**: Load tools from external assemblies
3. **Version Management**: Support multiple versions of tools
4. **Tool Marketplace**: Discover and install community tools
5. **Advanced Schemas**: Support complex parameter types and validation

## Conclusion

The MCP integration demonstrates how modern AI applications can benefit from standardized tool protocols. By combining MCP with .NET Aspire, we achieve:

- **Discoverability**: AI models can find and use tools dynamically
- **Interoperability**: Standard interfaces enable tool reuse
- **Scalability**: Aspire provides enterprise-grade hosting
- **Maintainability**: Clear separation of concerns and testable components

This implementation serves as a foundation for building sophisticated AI applications with rich tool ecosystems.