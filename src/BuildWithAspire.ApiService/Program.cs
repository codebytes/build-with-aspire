using System.Threading.RateLimiting;
using BuildWithAspire.ApiService.Data;
using BuildWithAspire.ApiService.Extensions;
using BuildWithAspire.ApiService.Models;
using BuildWithAspire.ApiService.Services;
using BuildWithAspire.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add PostgreSQL DbContext
builder.AddNpgsqlDbContext<ChatDbContext>("chatdb");

// Add AI services
builder.AddAIServices();

// Add HTTP client for MCP server communication with service discovery
// Don't set BaseAddress - let service discovery resolve it dynamically
builder.Services.AddHttpClient("mcpserver")
    .AddServiceDiscovery();

// Register MCP client and dynamic tool converter for Agent Framework integration
builder.Services.AddSingleton<IMcpClient, McpClient>();
builder.Services.AddSingleton<IDynamicMcpToolConverter, DynamicMcpToolConverter>();

// Add rate limiting for AI endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Rate limit for chat endpoints - 10 requests per minute
    options.AddFixedWindowLimiter("chat", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    // Rate limit for weather endpoint - 20 requests per minute (less expensive)
    options.AddFixedWindowLimiter("weather", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnetcore/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "BuildWithAspire API";
        document.Info.Description = "API for BuildWithAspire application with AI chat capabilities";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

builder.Services.AddTransient<ChatService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("BuildWithAspire API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// Enable rate limiting
app.UseRateLimiter();

// Apply migrations on startup
try
{
    app.Logger.LogInformation("Applying database migrations");
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var startTime = DateTime.UtcNow;

        try
        {
            // Check if database can be accessed
            var canConnect = await dbContext.Database.CanConnectAsync().ConfigureAwait(false);
            if (!canConnect)
            {
                app.Logger.LogWarning("Cannot connect to database. Skipping migrations.");
                return;
            }

            // Apply pending migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
            var pendingCount = pendingMigrations.Count();

            if (pendingCount > 0)
            {
                app.Logger.LogInformation("Applying {Count} pending migrations", pendingCount);
                await dbContext.Database.MigrateAsync().ConfigureAwait(false);
                var duration = DateTime.UtcNow - startTime;
                app.Logger.LogInformation("Database migrations applied successfully. Duration: {Duration}ms", duration.TotalMilliseconds);
            }
            else
            {
                var duration = DateTime.UtcNow - startTime;
                app.Logger.LogInformation("Database is up to date. Duration: {Duration}ms", duration.TotalMilliseconds);
            }
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P07")
        {
            // 42P07 = relation already exists - this is fine, table is already there
            app.Logger.LogInformation("Database tables already exist. Skipping migration creation.");
        }
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Database migration encountered an issue. Service will continue.");
}

app.MapGet("/weatherforecast", (IChatClient client, ILoggerFactory lf, AIConfiguration.AISettings settings) =>
{
    var logger = lf.CreateLogger("WeatherForecastEndpoint");

    // Create a weather agent using the Microsoft Agent Framework
    var weatherAgent = new Microsoft.Agents.AI.ChatClientAgent(
        client,
        new Microsoft.Agents.AI.ChatClientAgentOptions
        {
            Name = "WeatherAssistant",
            Instructions = "You are a helpful assistant that provides a description of the weather in one word based on the temperature."
        });

    async IAsyncEnumerable<WeatherForecast> GetForecasts()
    {
        for (int index = 1; index <= 5; index++)
        {
            var temperature = Random.Shared.Next(-20, 55);
            string summary;
            try
            {
                summary = await GetWeatherSummary(weatherAgent, temperature, logger, settings).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI summary generation failed (temp={Temp}, Provider={Provider}, Deployment={Deployment}, Model={Model})", temperature, settings.Provider, settings.DeploymentName, settings.Model);
                summary = GetFallbackSummary(temperature);
            }
            yield return new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                temperature,
                summary
            );
        }
    }

    return GetForecasts();

    static async Task<string> GetWeatherSummary(Microsoft.Agents.AI.AIAgent agent, int temp, ILogger logger, AIConfiguration.AISettings settings)
    {
        logger.LogDebug("Requesting AI weather summary (Provider={Provider}, Deployment={Deployment}, Model={Model}, Temp={Temp})", settings.Provider, settings.DeploymentName, settings.Model, temp);

        var response = await agent.RunAsync($"How would you describe the weather at temp {temp} in celsius? Provide the response in 1 word with no punctuation.").ConfigureAwait(false);

        var responseText = response.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            logger.LogWarning("Empty AI agent response (Temp={Temp}), using fallback", temp);
            return GetFallbackSummary(temp);
        }
        var trimmed = responseText.Trim();
        if (trimmed.Length > 20)
        {
            logger.LogWarning("AI response too long (Temp={Temp}, Length={Length}), using fallback", temp, trimmed.Length);
            return GetFallbackSummary(temp);
        }
        logger.LogDebug("AI agent response received: {Excerpt}", trimmed);

        return trimmed;
    }

    static string GetFallbackSummary(int temp) => temp switch
    {
        < 0 => "freezing",
        < 10 => "cold",
        < 20 => "cool",
        < 30 => "warm",
        _ => "hot"
    };
})
.WithName("GetWeatherForecast")
.WithOpenApi()
.RequireRateLimiting("weather");

// MCP tool call endpoint
app.MapPost("/mcp/call/{toolName}", async (string toolName, [FromBody] Dictionary<string, object?>? parameters, IMcpClient mcpClient, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("MCP tool call request received: {ToolName}", toolName);

        // Initialize MCP client if needed
        var initialized = await mcpClient.InitializeAsync().ConfigureAwait(false);
        if (!initialized)
        {
            logger.LogError("Failed to initialize MCP client");
            return Results.Problem("MCP client initialization failed", statusCode: 503);
        }

        // Call the MCP tool
        var result = await mcpClient.CallToolAsync(toolName, parameters).ConfigureAwait(false);

        logger.LogInformation("MCP tool {ToolName} completed. IsError: {IsError}", toolName, result.IsError);

        if (result.IsError)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error calling MCP tool: {ToolName}", toolName);

        // If it's an unknown tool error, list available tools
        if (ex.Message.Contains("Unknown tool", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var availableTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
                var toolNames = string.Join(", ", availableTools.Select(t => t.Name));
                logger.LogWarning("Unknown tool '{ToolName}'. Available tools: {AvailableTools}", toolName, toolNames);
                return Results.Problem(
                    $"Unknown tool '{toolName}'. Available tools: {toolNames}",
                    statusCode: 404);
            }
            catch
            {
                // If we can't list tools, just return the original error
            }
        }

        return Results.Problem($"Error calling MCP tool: {ex.Message}", statusCode: 500);
    }
})
.WithName("CallMcpTool")
.WithOpenApi()
.RequireRateLimiting("weather");

// MCP tools list endpoint
app.MapGet("/mcp/tools", async (IMcpClient mcpClient, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("MCP tools list request received");

        // Initialize MCP client if needed
        var initialized = await mcpClient.InitializeAsync().ConfigureAwait(false);
        if (!initialized)
        {
            logger.LogError("Failed to initialize MCP client");
            return Results.Problem("MCP client initialization failed", statusCode: 503);
        }

        // List available tools
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

        logger.LogInformation("Retrieved {ToolCount} MCP tools", tools.Length);

        return Results.Ok(tools);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error listing MCP tools");
        return Results.Problem($"Error listing MCP tools: {ex.Message}", statusCode: 500);
    }
})
.WithName("ListMcpTools")
.WithOpenApi();

// MCP health check endpoint to verify connectivity
app.MapGet("/mcp/health", async (IMcpClient mcpClient, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("MCP health check request received");

        // Try to initialize the MCP client
        var initialized = await mcpClient.InitializeAsync().ConfigureAwait(false);

        if (!initialized)
        {
            logger.LogWarning("MCP client initialization failed");
            return Results.Problem(
                "MCP server connection failed",
                statusCode: 503,
                title: "Service Unavailable");
        }

        // Try to list tools to verify full connectivity
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

        logger.LogInformation("MCP health check passed. Tools available: {ToolCount}", tools.Length);

        return Results.Ok(new
        {
            status = "healthy",
            mcpServerConnected = true,
            toolsAvailable = tools.Length,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MCP health check failed");
        return Results.Problem(
            $"MCP health check failed: {ex.Message}",
            statusCode: 503,
            title: "Service Unavailable");
    }
})
.WithName("McpHealthCheck")
.WithOpenApi();

// Conversation endpoints
app.MapGet("/conversations", async (ChatDbContext db) =>
{
    try
    {
        var conversations = await db.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.CreatedAt,
                c.UpdatedAt,
                MessageCount = c.Messages.Count()
            })
            .ToListAsync().ConfigureAwait(false);

        return Results.Ok(conversations);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error retrieving conversations");
        return Results.Problem("Failed to retrieve conversations", statusCode: 500);
    }
})
.WithName("GetConversations")
.WithOpenApi();

app.MapGet("/conversations/{id}", async (Guid id, ChatDbContext db) =>
{
    try
    {
        var conversation = await db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);

        if (conversation == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(conversation);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error retrieving conversation with ID: {ConversationId}", id);
        return Results.Problem("Failed to retrieve conversation", statusCode: 500);
    }
})
.WithName("GetConversation")
.WithOpenApi();

// Diagnostics endpoint

app.MapPost("/conversations", async ([FromBody] CreateConversationRequest request, ChatDbContext db) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("Conversation name cannot be empty");
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync().ConfigureAwait(false);

        return Results.Created($"/conversations/{conversation.Id}", conversation);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error creating conversation: {ConversationName}", request.Name);
        return Results.Problem("Failed to create conversation", statusCode: 500);
    }
})
.WithName("CreateConversation")
.WithOpenApi();

app.MapPost("/conversations/{id}/messages", async (Guid id, [FromBody] SendMessageRequest request, ChatService chatService, ChatDbContext db) =>
{
    try
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest("Message cannot be empty");
        }

        var conversation = await db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);

        if (conversation == null)
        {
            return Results.NotFound("Conversation not found");
        }

        // Add user message
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = id,
            Role = "user",
            Content = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        // Add to DbContext directly instead of through navigation property
        db.Messages.Add(userMessage);
        conversation.UpdatedAt = DateTime.UtcNow;

        // Save user message first
        await db.SaveChangesAsync().ConfigureAwait(false);

        // Prepare history for AI - get all messages for this conversation including the new one
        var messages = await db.Messages
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageRequest
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToListAsync().ConfigureAwait(false);

        // Get AI response with timeout handling
        string aiResponse;
        try
        {
            aiResponse = await chatService.ProcessMessagesWithHistory(messages).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "AI service error for conversation {ConversationId}", id);
            return Results.Problem("Failed to get AI response", statusCode: 500);
        }

        // Add assistant message
        var assistantMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = id,
            Role = "assistant",
            Content = aiResponse,
            CreatedAt = DateTime.UtcNow
        };

        // Add to DbContext directly instead of through navigation property
        db.Messages.Add(assistantMessage);
        await db.SaveChangesAsync().ConfigureAwait(false);

        return Results.Ok(new { response = aiResponse });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error in SendMessage endpoint for conversation {ConversationId}", id);
        return Results.Problem("Internal server error", statusCode: 500);
    }
})
.WithName("SendMessage")
.WithOpenApi()
.RequireRateLimiting("chat");

app.MapDelete("/conversations/{id}", async (Guid id, ChatDbContext db) =>
{
    var conversation = await db.Conversations.FindAsync(id).ConfigureAwait(false);
    if (conversation == null)
    {
        return Results.NotFound();
    }

    db.Conversations.Remove(conversation);
    await db.SaveChangesAsync().ConfigureAwait(false);

    return Results.NoContent();
})
.WithName("DeleteConversation")
.WithOpenApi();

// Keep the original chat endpoint for backward compatibility
app.MapGet("/chat", async (ChatService chatService, string message) => await chatService.ProcessMessage(message).ConfigureAwait(false))
    .WithName("GetChat")
    .WithOpenApi()
    .RequireRateLimiting("chat");

app.Run();

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Request DTOs
public record CreateConversationRequest(string Name);
public record SendMessageRequest(string Message);

// Make Program class accessible for testing
public partial class Program { }
