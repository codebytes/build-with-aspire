# Build with Aspire demo guide

This repository contains resources and examples demonstrating building with .NET Aspire, including a complete implementation of the Model Context Protocol (MCP) for AI agent tools.

For slides and related resources, see the [talk README](../README.md).

## Repository Content

This repository provides insights, best practices, and demonstrations for building with Aspire. Topics covered include:

- An introduction to Aspire.
- Features and benefits of using Aspire.
- Step-by-step guide to start a new project with Aspire.
- Advanced scenarios like optimizing performance and scalability.
- Publishing Aspire applications to various platforms.
- Passing configuration and secrets to Aspire applications.
- **MCP Server Implementation**: Complete implementation using the official MCP C# SDK with dynamic tool discovery over HTTPS/SSE

## Project Features

### MCP Server Integration

This project demonstrates a production-ready implementation of the Model Context Protocol (MCP):

- **Official MCP C# SDK**: Uses [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)
- **Dynamic Tool Discovery**: API service discovers tools from MCP server at runtime
- **HTTPS/SSE Transport**: Server-Sent Events for bidirectional communication
- **Service Discovery**: Aspire handles endpoint resolution between services
- **13 Built-in Tools**: Weather, System, and Math tools ready to use
- **Microsoft Agent Framework**: Integrated with AI agent for autonomous tool usage

### Architecture

```text
API Service → (HTTPS/SSE) → MCP Server → Tools
    ↓                            ↓
Chat Agent                  WeatherTools
(Microsoft Agent Framework) SystemTools
                            MathTools
```

### Documentation

- **[api-tests/](../api-tests/)**: HTTP test files for all endpoints
- **[MCP Server README](../src/BuildWithAspire.MCPServer/README.md)**: MCP Server implementation details

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Docker Desktop (for Aspire)
- Visual Studio Code or Visual Studio 2022

### Quick Start

```bash
# Clone the repository
git clone https://github.com/codebytes/build-with-aspire.git
cd build-with-aspire

# Run with .NET CLI
cd src/BuildWithAspire.AppHost
dotnet run

# Or press F5 in VS Code / Visual Studio
```

The Aspire Dashboard will open at `https://localhost:17020` showing all services.

### Service Endpoints

- **MCP Server**: http://localhost:8080
- **API Service**: http://localhost:5020
- **Aspire Dashboard**: https://localhost:17020

### Testing the MCP Server

```bash
# List available tools
curl http://localhost:5020/mcp/tools

# Check service health
curl http://localhost:8080/health
curl http://localhost:5020/health
```

Use the [api-tests/](../api-tests/) directory for more HTTP test examples.

## Legacy resource

The original [Aspirify](https://aspireify.net/) link is preserved for reference. The domain failed DNS lookup on 2026-09-21 and is currently unavailable. For official Aspire documentation and samples, see the [talk README](../README.md).
