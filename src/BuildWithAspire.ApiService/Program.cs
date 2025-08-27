using BuildWithAspire.ApiService.Data;
using BuildWithAspire.ApiService.Extensions;
using BuildWithAspire.ApiService.Models;
using BuildWithAspire.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Scalar.AspNetCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add PostgreSQL DbContext
builder.AddNpgsqlDbContext<ChatDbContext>("chatdb");

// Add AI services using official Aspire integrations
builder.AddAIServices();

// Learn more about configuring OpenAPI at https://aka.ms/aspnetcore/openapi
builder.Services.AddEndpointsApiExplorer();

// Configure JSON options to ensure proper serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Keep original property names
    options.SerializerOptions.WriteIndented = true;
});

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

// Register ChatService with simplified dependencies
builder.Services.TryAddTransient<ChatService>();

// Register HttpClient for MCP client with service discovery
builder.Services.AddHttpClient<IMcpClient, McpClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "BuildWithAspire-MCP-Client/1.0");
    // Base address will be set via service discovery to mcpserver
    client.BaseAddress = new Uri("https+http://mcpserver/");
}).AddServiceDiscovery();

// Register MCP client as scoped to ensure proper HttpClient disposal
builder.Services.TryAddScoped<IMcpClient, McpClient>();

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

// Apply migrations on startup
try
{
    app.Logger.LogInformation("Initializing database connection and schema");
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var startTime = DateTime.UtcNow;

        // Ensure database schema exists
        var wasCreated = await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
        var duration = DateTime.UtcNow - startTime;

        if (wasCreated)
        {
            app.Logger.LogInformation("Database schema created successfully. Duration: {Duration}ms", duration.TotalMilliseconds);
        }
        else
        {
            app.Logger.LogInformation("Database schema already exists. Duration: {Duration}ms", duration.TotalMilliseconds);
        }
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database initialization failed. Service will continue without database.");
}

// All tool-specific endpoints have been removed in favor of generic MCP endpoints:
// - Use /mcp/tools to list all available tools
// - Use /mcp/call/{toolName} to call any tool with parameters
// - Use /mcp/tools/metadata for detailed tool metadata
// This approach allows for dynamic tool discovery without hardcoded endpoints

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
.WithOpenApi();

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
    .WithOpenApi();

// MCP integration test endpoints
app.MapGet("/mcp/tools", async (IMcpClient mcpClient) =>
{
    try
    {
        await mcpClient.InitializeAsync().ConfigureAwait(false);
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
        return Results.Ok(new { tools = tools.Select(t => t.Name).ToArray(), count = tools.Length });
    }
    catch (Exception ex)
    {
        return Results.Problem($"MCP error: {ex.Message}", statusCode: 500);
    }
})
.WithName("ListMcpTools")
.WithOpenApi();

app.MapPost("/mcp/call/{toolName}", async (string toolName, object? parameters, IMcpClient mcpClient, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("MCP tool call: {ToolName} with parameters: {Parameters}", toolName, parameters);
        
        await mcpClient.InitializeAsync().ConfigureAwait(false);
        var result = await mcpClient.CallToolAsync(toolName, parameters).ConfigureAwait(false);
        
        logger.LogInformation("MCP tool result - IsError: {IsError}, Content count: {ContentCount}", 
            result.IsError, result.Content?.Length ?? 0);
        
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MCP tool call failed for tool {ToolName}", toolName);
        return Results.Problem($"MCP tool call error: {ex.Message}", statusCode: 500);
    }
})
.WithName("CallMcpTool")
.WithOpenApi();

// MCP tools metadata endpoint
app.MapGet("/mcp/tools/metadata", async (IMcpClient mcpClient) =>
{
    try
    {
        await mcpClient.InitializeAsync().ConfigureAwait(false);
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
        return Results.Ok(new { tools = tools, count = tools.Length });
    }
    catch (Exception ex)
    {
        return Results.Problem($"MCP metadata error: {ex.Message}", statusCode: 500);
    }
})
.WithName("ListMcpToolMetadata")
.WithOpenApi();


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
