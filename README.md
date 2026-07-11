# Build with Aspire

This repository contains resources and examples demonstrating building with .NET Aspire, including a complete implementation of the Model Context Protocol (MCP) for AI agent tools.

## Slides

The slides for the related talk can be found at:\
[http://chris-ayers.com/build-with-aspire/](http://chris-ayers.com/build-with-aspire/)

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

```
API Service → (HTTPS/SSE) → MCP Server → Tools
    ↓                            ↓
Chat Agent                  WeatherTools
(Microsoft Agent Framework) SystemTools
                            MathTools
```

### Documentation

- **[api-tests/](./api-tests/)**: HTTP test files for all endpoints
- **[MCP Server README](./src/BuildWithAspire.MCPServer/README.md)**: MCP Server implementation details

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
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

Use the [api-tests/](./api-tests/) directory for more HTTP test examples.

## Resources

- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [MCP Specification](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)
- [Aspirify](https://aspireify.net/)
- [Aspire Samples](https://github.com/dotnet/aspire-samples)

## Connect with Chris Ayers

Feel free to connect with Chris Ayers on social media and visit his blog for more information on Playwright and other topics:

- Twitter: [@Chris_L_Ayers](https://twitter.com/Chris_L_Ayers)
- Mastodon: [@Chrisayers@hachyderm.io](https://hachyderm.io/@Chrisayers)
- LinkedIn: [chris-l-ayers](https://linkedin.com/in/chris-l-ayers/)
- Blog: [https://chris-ayers.com/](https://chris-ayers.com/)
- GitHub: [Codebytes](https://github.com/codebytes)

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more information.
