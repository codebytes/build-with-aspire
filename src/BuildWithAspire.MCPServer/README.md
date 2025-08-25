# BuildWithAspire MCP Server

This is a proper Model Context Protocol (MCP) server implementation for the BuildWithAspire application. It provides dynamic tool discovery and execution capabilities for AI models through the standardized MCP protocol.

## What is MCP?

The Model Context Protocol (MCP) is an open standard that enables seamless integration between AI applications and external data sources/tools. It allows AI models to:

- **Dynamically discover tools** available from MCP servers
- **Execute tools safely** with proper user consent and validation
- **Access real-time data** and services through standardized interfaces
- **Maintain security** through controlled tool execution

## Available Tools

This MCP server provides several tool categories:

### Weather Tools
- `GetCurrentWeather()` - Gets current weather information
- `GetWeatherForecast(maxDays)` - Gets weather forecast for 1-10 days
- `ConvertTemperature(temperature, fromUnit)` - Converts between Celsius and Fahrenheit

### System Tools
- `GetCurrentDateTime()` - Gets current date/time information
- `GetSystemInfo()` - Gets system information (OS, .NET version, etc.)
- `GenerateRandomNumber(min, max)` - Generates random numbers
- `EncodeToBase64(text)` - Encodes text to Base64
- `DecodeFromBase64(base64Text)` - Decodes Base64 to plain text

### Math Tools
- `Calculate(a, b, operation)` - Basic arithmetic operations
- `SquareRoot(number)` - Calculates square root
- `Power(baseNumber, exponent)` - Raises to power
- `GenerateFibonacci(terms)` - Generates Fibonacci sequence
- `IsPrime(number)` - Checks if number is prime

## Architecture

### MCP Server (BuildWithAspire.MCPServer)
- Implements proper MCP JSON-RPC 2.0 protocol
- Supports stdio transport for communication
- Provides tool discovery and execution capabilities
- Handles initialization and capability negotiation

### MCP Client Integration (BuildWithAspire.ApiService)
- `IMcpClient` interface for interacting with MCP servers
- `McpClient` implementation that communicates via stdio
- Integrated with `ChatService` for AI-powered tool usage
- Automatic detection of weather-related requests

### Dynamic Tool Usage
When a user asks weather-related questions, the AI will:
1. Detect weather keywords in the message
2. Query the MCP server for appropriate weather data
3. Include the real-time weather information in the AI response
4. Provide accurate, up-to-date information

## Testing the MCP Server

### Standalone MCP Server Test
```bash
cd src/BuildWithAspire.MCPServer
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"clientInfo":{"name":"test","version":"1.0"}}}' | dotnet run
```

### API Integration Tests
Once the application is running:

```bash
# List available MCP tools
curl http://localhost:5000/mcp/tools

# Call a specific tool
curl -X POST http://localhost:5000/mcp/call/GetCurrentWeather \
  -H "Content-Type: application/json" \
  -d '{}'

# Test weather integration in chat
curl "http://localhost:5000/chat?message=What's the current weather?"
```

## Development and Configuration

### Local Development
```json
{
  "servers": {
    "BuildWithAspire.MCPServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "path/to/BuildWithAspire.MCPServer"
      ]
    }
  }
}
```

### Key Features
- **Standards Compliant**: Implements MCP specification correctly
- **Type Safe**: Uses C# with proper typing and validation
- **Extensible**: Easy to add new tools and capabilities
- **Integrated**: Works seamlessly with .NET Aspire architecture
- **Secure**: Proper error handling and input validation

## Integration with AI Chat

The MCP server is integrated with the ChatService to provide enhanced AI capabilities:

```csharp
// Weather-related messages automatically trigger MCP tool usage
"What's the weather like?" 
// → Calls GetCurrentWeather() and includes real data in AI response

"Give me a 5-day forecast"
// → Calls GetWeatherForecast(5) and provides detailed forecast

"What's 15 degrees Celsius in Fahrenheit?"
// → Calls ConvertTemperature(15, "C") and provides conversion
```

## Protocol Compliance

This server implements the complete MCP specification:
- ✅ JSON-RPC 2.0 messaging
- ✅ Proper initialization handshake  
- ✅ Tool discovery (`tools/list`)
- ✅ Tool execution (`tools/call`)
- ✅ Error handling and validation
- ✅ Capability negotiation
- ✅ Stdio transport support

## Future Extensions

The MCP architecture makes it easy to add:
- Database query tools
- File system operations
- API integrations
- Custom business logic
- External service connectors

This demonstrates the power of MCP: once the protocol is implemented, adding new capabilities is as simple as creating new `[McpServerTool]` methods.