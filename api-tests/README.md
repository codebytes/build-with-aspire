# API Testing Files

This directory contains HTTP request files for testing all APIs in the BuildWithAspire application.

## Files Overview

- **`apiservice.http`** - Comprehensive tests for the main API service
- **`mcpserver.http`** - Tests for the MCP (Model Context Protocol) server
- **`webfrontend.http`** - Tests for the web frontend endpoints

## How to Use

### 1. Using VS Code REST Client Extension

Install the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension for VS Code.

1. Open any `.http` file
2. Click the "Send Request" button above each request
3. View the response in the new tab

### 2. Update Base URLs

Before testing, update the base URLs in each file to match your current Aspire deployment:

1. Run `aspire run` 
2. Check the Aspire dashboard for current port assignments
3. Update the `@baseUrl` variables in each file

Example port assignments (these change on each run):
```
webfrontend: https://localhost:7051
apiservice: https://localhost:7287  
mcpserver: http://localhost:34055
```

### 3. Testing Workflows

#### API Service Testing
Use `apiservice.http` for comprehensive API testing:
1. Test weather endpoints
2. Create conversations
3. Send messages
4. Test chat functionality

#### MCP Server Testing  
Use `mcpserver.http` to test the weather tools directly.

## API Documentation

### Scalar Documentation (Interactive)

When running in development mode, visit these URLs in your browser:

- **API Service**: `https://localhost:7287/scalar/v1`
- **MCP Server**: `http://localhost:34055/scalar/v1`

### OpenAPI Specifications

- **API Service**: `https://localhost:7287/openapi/v1.json`
- **MCP Server**: `http://localhost:34055/openapi/v1.json`

## Common Test Scenarios

### 1. MCP Tool Testing
```http
# List available MCP tools
GET {{apiService}}/mcp/tools

# Call weather tools via API Service
POST {{apiService}}/mcp/call/GetCurrentWeather
POST {{apiService}}/mcp/call/GetWeatherForecast

# Direct MCP protocol calls to server (JSON-RPC)
POST {{mcpServer}}/mcp
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/call",
  "params": {
    "name": "GetCurrentWeather",
    "arguments": {}
  }
}
```

### 2. Conversation Flow
```http
# 1. Create conversation
POST {{apiService}}/conversations
{
  "name": "Test Chat"
}

# 2. Send message (use ID from step 1)
POST {{apiService}}/conversations/{id}/messages  
{
  "message": "Hello!"
}

# 3. Get conversation history
GET {{apiService}}/conversations/{id}
```

### 3. Health Monitoring
```http
GET {{apiService}}/health
GET {{mcpServer}}/health  
GET {{webFrontend}}/health
```

## Troubleshooting

### Port Issues
If requests fail, check that:
1. Aspire is running (`aspire run`)
2. Ports match the Aspire dashboard
3. Services are healthy (green status)

### SSL/TLS Issues
For HTTPS endpoints, you may need to:
1. Accept self-signed certificates in your browser
2. Use HTTP variants if available
3. Check Aspire certificate configuration

### Authentication
Most endpoints don't require authentication in development mode. For production deployments, you may need to add authorization headers.

## Contributing

When adding new endpoints:
1. Add them to the appropriate service file
2. Include proper documentation
3. Add examples with realistic data
4. Update this README if needed