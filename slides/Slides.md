---
marp: true
theme: custom-default
footer: '@Chris_L_Ayers - https://chris-ayers.com'
---

![bg](./img/aspire_title.png)

---

<!--

# Aspiring .NET with OpenAI and Ollama

## Chris Ayers

![bg right](./img/dotnet-logo.png)

---

-->

![bg left:40%](./img/portrait.png)

## Chris Ayers

### Principal Software Engineer<br>Azure CXP AzRel<br>Microsoft

<i class="fa-brands fa-bluesky"></i> BlueSky: [@chris-ayers.com](https://bsky.app/profile/chris-ayers.com)
<i class="fa-brands fa-linkedin"></i> LinkedIn: - [chris\-l\-ayers](https://linkedin.com/in/chris-l-ayers/)
<i class="fa fa-window-maximize"></i> Blog: [https://chris-ayers\.com/](https://chris-ayers.com/)
<i class="fa-brands fa-github"></i> GitHub: [Codebytes](https://github.com/codebytes)
<i class="fa-brands fa-mastodon"></i> Mastodon: [@Chrisayers@hachyderm.io](https://hachyderm.io/@Chrisayers)
~~<i class="fa-brands fa-twitter"></i> Twitter: @Chris_L_Ayers~~

---

# <!-- fit --> Aspire is designed to improve the<br> experience of building cloud-native apps

---

# Why Aspire

<div class="columns3">
<div>

## Orchestration

- Service Management
- Configuration
- Service Discovery
- Health & Telemetry

</div>
<div>

## Integrations

- Azure Services
- Local Development
- Multi-Platform
- Community Ecosystem

</div>
<div>

## Tooling

- IDE Integration
- Dashboard
- AppHost & CLI

</div>
</div>

---

# Service Defaults & Features

<div class="columns">
<div>

- **Security Defaults**
  - Azure AD authentication
  - Secure secrets management
- **Resilience & Scalability**
  - Automatic retry policies
  - Circuit breakers for failure protection

</div>
<div>

- **Custom Commands**
  - Workflow automation
  - Database migrations, data seeding
  - Development environment reset
- **Launch Profiles**
  - Multiple environments (local, cloud, test)
  - Environment variable configuration

</div>
</div>

---

# Aspire Dashboard

<div class="columns">
<div>

![fit](./img/aspire-dashboard.png)

</div>
<div>

- Real-time Monitoring
- Interactive Debugging
- Resource Management
- Secret Management
- Copilot AI Debugging
- GenAI Visualizer

</div>
</div>

---

# Service Discovery and Configuration

- **Automatic Configuration**: AppHost passes settings to services
- **Implicit Discovery**: Services reference only what they need
- **Named Endpoints**: Multiple endpoints per service
- **Environment Variables**: Structured configuration strings

![bg right w:600px](./img/service-discovery.png)

---

# Testing in Aspire

- **Integration Testing**: Test multiple components together
- **Container Testing**: Test with containerized services
- **Emulator Support**: Use local emulators for cloud services
- **End-to-End Testing**: Test full application workflows

![bg left fit](./img/aspire-testing-diagram.png)

---

# Aspire Integrations

![Integrations](img/integrations.png)

---

# Aspire Integrations

<div class="columns">
<div>

## Hosting Integrations

- Provision and manage resources
- Automate infrastructure setup
- Cloud-native functionality
- Integrated logging and monitoring
- Authentication and authorization
- Configuration management

</div>
<div>

## Client Integrations

- Configure service connections
- Consume hosted services
- Service discovery
- Service defaults
- Integrated logging and monitoring

</div>
</div>

---

# Official Integrations

<div class="columns">
<div>

## Cloud-Agnostic

- **Databases**: PostgreSQL, MySQL, MongoDB, SQL Server
- **Messaging**: Kafka, RabbitMQ, NATS
- **AI & Observability**: Ollama, OpenAI, Foundry, OpenTelemetry

</div>
<div>

## Cloud-Specific

- **Azure Services**: OpenAI, Cosmos DB, SQL, Redis, Key Vault, App Service, ACR, App Config
- **AWS Support**: Via Hosting.AWS package

</div>
</div>

---

# Community Toolkit Integrations

<div class="columns">
<div>

## Multi-Language Support

- **JavaScript**: Bun, Deno
- **Systems Programming**: Go, Rust
- **Enterprise**: Java/Spring
- **Extensions**: Node.js, SQL, MongoDB, Redis

</div>
<div>

## Additional Services

- **AI & Search**: Ollama, Meilisearch
- **Data**: SQLite, Data API Builder
- **Web**: Azure Static Web Apps emulator
- **Customization**: Advanced configurations

</div>
</div>

---


# What's New in .NET Aspire 13.4

<div class="columns">
<div>

## ⚙️ CLI & Tooling

- **TypeScript AppHost** now GA
- **aspire deploy / publish** GA
- **AI agent readiness** via skills

## 🎨 Dashboard

- **AI Agents dialog**
- Structured search & trace filtering

</div>
<div>

## 🧩 App Model

- **Go** & **Bun** hosting integrations
- **Blazor WebAssembly** hosting (preview)

## 🚀 Deployment

- **Azure Container App Jobs** stable
- Richer **Kubernetes / AKS** APIs

</div>
</div>

---

# Compute Environments & Aspire CLI

<div class="columns">
<div>

## Multiple Environments

```csharp
var k8s = builder.AddKubernetesEnvironment("k8s");
var compose = builder.AddDockerComposeEnvironment("docker");

builder.AddProject<Projects.Api>("api")
    .WithComputeEnvironment(compose);

builder.AddProject<Projects.Frontend>("frontend")
    .WithComputeEnvironment(k8s);
```

</div>
<div>

## Aspire CLI

```bash
# Install
curl -sSL https://aspire.dev/install.sh | bash
dotnet tool install -g Aspire.Cli

# Commands
aspire new | run | add | update
aspire config | publish | deploy
aspire exec
```

</div>
</div>

---

# Azure Deployment

<div class="columns">
<div>

## Targets

- Azure App Service
- Azure Container Apps
- Kubernetes

## Azure Developer CLI

```bash
azd up         # Deploy everything
azd deploy     # App only
azd init       # Initialize
```

</div>
<div>

## Features

- Auto-detects app structure
- Environment variable mapping
- Native Aspire support
- Key Vault integration

</div>
</div>

---

# Interactive Parameter Prompting

<div class="columns">
<div>

```csharp
// Parameters without defaults trigger dashboard prompts
var apiKey = builder.AddParameter("api-key", secret: true);
var dbUrl = builder.AddParameter("database-url");

var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", apiKey)
    .WithEnvironment("DATABASE_URL", dbUrl);
```

</div>
<div>

## Features

- **Automatic prompting** for missing parameters
- **Rich form inputs** in dashboard
- **Secret masking** for sensitive data

**Input Types**: Text, Password, Choice, Boolean, Number

</div>
</div>

---

# Local to Cloud Integrations

| **Component** | **Local** | **Cloud** |
|--------------|----------|----------|
| **AI Models** | Ollama, Foundry local, LM Studio | Azure OpenAI, AI Foundry |
| **Models** | Llama 3.2, Qwen2.5, Phi-4 | GPT-5, GPT-5-mini, Claude 4 |
| **Vector DB** | Qdrant, Chroma, pgvector | Azure AI Search, Cosmos DB |

**Features**: Zero-friction transitions • Emulator support • Hybrid development • Auto configuration • Secret management

---

# Microsoft.Extensions.AI

| **API & Architecture** | **Description** | **Development** | **Description** |
|------------------------|-----------------|-----------------|-----------------|
| **Unified API** | Common interface for AI providers | **Local Development** | Connect to Ollama, LM Studio |
| **Pipeline Architecture** | Chain components efficiently | **Cloud Ready** | Same code for Azure OpenAI |
| **DI Integration** | Works with .NET service container | **Transport Options** | HTTP, gRPC, direct calls |
| **Provider-Agnostic** | Single interface, multiple backends | **Performance** | Auto-batching and throttling |

---

# AI Local Development

<div class="columns">
<div>

```csharp
// Local development with Ollama
builder.AddOllama("ollama")
  .WithModel("llama3");

// Add client to use the model
builder.AddOllamaApiClient()
  .AddChatClient();
```

```csharp
public class ExampleService(IChatClient chatClient)
{
    // Use chat client...
}
```

</div>
<div>

- **Zero Cost Development**: No API charges for local models
- **Privacy & Security**: All data stays on your machine
- **Offline Capability**: Work without internet connectivity
- **Fast Iteration**: No network latency for testing

</div>
</div>

---

# AI Cloud Development

<div class="columns">
<div>

```csharp
// Single line change for production
builder.AddAzureOpenAI("ai");

// Same client code works unchanged
builder.AddAzureOpenAIClient()
  .AddChatClient();
```

```csharp
public class ExampleService(IChatClient chatClient)
{
    // Use chat client...
}
```

</div>
<div>

- **Latest Models**: Access to GPT-5, Claude 4, and cutting-edge AI
- **Global Availability**: 99.9% SLA with worldwide deployment
- **Managed Infrastructure**: No server maintenance required
- **Advanced Features**: Function calling, vision, audio processing

</div>
</div>

---

# The Evolution of .NET AI Frameworks

<div class="columns">
<div>

## Previous Landscape

- **Semantic Kernel**: High-level orchestration framework
- **AutoGen**: Multi-agent conversation framework
- **Fragmented Ecosystem**: Different APIs, patterns, abstractions

</div>
<div>

## The Challenge

- **Inconsistent APIs**: Each framework had unique approaches
- **Integration Complexity**: Difficult to combine tools
- **Provider Lock-in**: Hard to switch AI providers

</div>
</div>

---

# Microsoft Agent Framework

<div class="columns">
<div>

## Unified Multi-Agent Platform

- **Now GA**: Unifies Semantic Kernel & AutoGen
- **Built on Microsoft.Extensions.AI**: Unified abstractions
- **Multi-Agent Orchestration**: Coordinate AI agents

</div>
<div>

## Key Features

- **Agent Types**: Task-based, conversational, autonomous
- **Tool Integration**: MCP tools & function calling
- **Provider Agnostic**: Works with any AI backend

</div>
</div>

---

# Model Context Protocol (MCP)

<div class="columns">
<div>

## Standardized Tool Access

- **Open protocol** for exposing tools to AI
- **Dynamic discovery**: agents load tools at runtime
- **Transport-agnostic**: stdio, HTTP, SSE

</div>
<div>

## In this Demo

- **MCP server** hosts Math, Weather, System & Text tools
- **MAF agent** discovers tools via `McpClientTool`
- **Local tool-calling** with Foundry Local (Qwen2.5)

</div>
</div>

---

![bg right](./img/dotnet-logo.png)

# DEMOS

---

# Questions ?

![bg right](./img/owl.png)

---

# Resources

<div class="columns">
<div>

## Links

- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [What's new in .NET Aspire 13.4](https://aspire.dev/whats-new/aspire-13-4/)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)
- [Aspire Samples](https://github.com/dotnet/aspire-samples)

</div>
<div>

## Follow Chris Ayers

![w:300px](./img/chris_ayers.svg)

<!--
<i class="fa-brands fa-bluesky"></i> BlueSky: [@chris-ayers.com](https://bsky.app/profile/chris-ayers.com)
<i class="fa-brands fa-linkedin"></i> LinkedIn: - [chris\-l\-ayers](https://linkedin.com/in/chris-l-ayers/)
<i class="fa fa-window-maximize"></i> Blog: [https://chris-ayers\.com/](https://chris-ayers.com/)
<i class="fa-brands fa-github"></i> GitHub: [Codebytes](https://github.com/codebytes)
<i class="fa-brands fa-mastodon"></i> Mastodon: [@Chrisayers@hachyderm.io](https://hachyderm.io/@Chrisayers)
~~<i class="fa-brands fa-twitter"></i> Twitter: @Chris_L_Ayers~~ -->

</div>
</div>

<!-- Needed for mermaid, can be anywhere in file except frontmatter -->
<script type="module">
  import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs';
  mermaid.initialize({ startOnLoad: true });
</script>

---

# Feedback

![bg fit](./img/aspiring_net_with_azure_open_ai_and_ollama-qr-code.png)