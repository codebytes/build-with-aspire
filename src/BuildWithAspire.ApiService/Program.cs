using BuildWithAspire.ApiService.Data;
using BuildWithAspire.ApiService.Extensions;
using BuildWithAspire.ApiService.Models;
using BuildWithAspire.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;
using System.Text.Json;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add PostgreSQL DbContext
builder.AddNpgsqlDbContext<ChatDbContext>("chatdb");

// Add AI services
builder.AddAIServices();

builder.Services.AddKernel();

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

// Add MCP Client Service to communicate with MCP Server
builder.Services.AddHttpClient<McpClientService>(client =>
{
    // This will be configured by Aspire service discovery to point to the MCP server
    client.BaseAddress = new Uri("http://mcpserver");
});

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

app.MapGet("/weatherforecast", (IChatClient client) =>
{
    async IAsyncEnumerable<WeatherForecast> GetForecasts()
    {
        for (int index = 1; index <= 5; index++)
        {
            var temperature = Random.Shared.Next(-20, 55);
            var summary = await GetWeatherSummary(client, temperature).ConfigureAwait(false);
            yield return new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                temperature,
                summary
            );
        }
    }

    return GetForecasts();

    static async Task<string> GetWeatherSummary(IChatClient client, int temp)
    {
        List<ChatMessage> conversation = new()
        {
             // System messages represent instructions or other guidance about how the assistant should behave
            new(ChatRole.System, "You are a helpful assistant that provides a description of the weather in one word based on the temperature."),
            // User messages represent user input, whether historical or the most recent input
            new(ChatRole.User, $"How would you describe the weather at temp {temp} in celcius? Provide the response in 1 word with no punctuation.")
        };
        var completion = await client.GetResponseAsync(conversation).ConfigureAwait(false);

        return $"{completion.Text}";
    }
})
.WithName("GetWeatherForecast")
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

app.MapPost("/conversations/{id}/messages", async (Guid id, [FromBody] SendMessageRequest request, ChatService chatService, McpClientService mcpClient, ChatDbContext db) =>
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

        // Check if the user is asking for weather or time information
        var message = request.Message.ToLowerInvariant();
        string aiResponse;
        
        if (message.Contains("weather") || message.Contains("forecast"))
        {
            // Try to call MCP weather tool
            try
            {
                // Extract location from user message (simple approach)
                var location = "New York"; // Default location
                if (message.Contains(" in "))
                {
                    var locationPart = message.Substring(message.IndexOf(" in ") + 4);
                    var words = locationPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 0)
                    {
                        location = string.Join(" ", words.Take(2)); // Take up to 2 words for location
                    }
                }

                var weatherResult = await mcpClient.CallToolAsync("get_weather", new Dictionary<string, object>
                {
                    ["location"] = location,
                    ["days"] = 3
                }).ConfigureAwait(false);

                if (weatherResult != null)
                {
                    var weatherJson = System.Text.Json.JsonSerializer.Serialize(weatherResult, new JsonSerializerOptions { WriteIndented = true });
                    
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

                    // Add the weather data to the context
                    messages.Add(new ChatMessageRequest
                    {
                        Role = "system",
                        Content = $"Weather data from MCP tool: {weatherJson}. Use this data to provide a helpful weather summary to the user."
                    });

                    aiResponse = await chatService.ProcessMessagesWithHistory(messages).ConfigureAwait(false);
                }
                else
                {
                    aiResponse = "I'm sorry, I couldn't get the weather information right now. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Failed to get weather from MCP tool");
                aiResponse = "I'm sorry, I encountered an error while trying to get weather information.";
            }
        }
        else if (message.Contains("time") || message.Contains("date"))
        {
            // Try to call MCP time tool
            try
            {
                var timeResult = await mcpClient.CallToolAsync("get_time", new Dictionary<string, object>
                {
                    ["timezone"] = "UTC",
                    ["format"] = "detailed"
                }).ConfigureAwait(false);

                if (timeResult != null)
                {
                    var timeJson = System.Text.Json.JsonSerializer.Serialize(timeResult, new JsonSerializerOptions { WriteIndented = true });
                    
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

                    // Add the time data to the context
                    messages.Add(new ChatMessageRequest
                    {
                        Role = "system",
                        Content = $"Current time data from MCP tool: {timeJson}. Use this data to provide helpful time/date information to the user."
                    });

                    aiResponse = await chatService.ProcessMessagesWithHistory(messages).ConfigureAwait(false);
                }
                else
                {
                    aiResponse = "I'm sorry, I couldn't get the current time information right now.";
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Failed to get time from MCP tool");
                aiResponse = "I'm sorry, I encountered an error while trying to get time information.";
            }
        }
        else
        {
            // Regular chat processing
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
            try
            {
                aiResponse = await chatService.ProcessMessagesWithHistory(messages).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "AI service error for conversation {ConversationId}", id);
                return Results.Problem("Failed to get AI response", statusCode: 500);
            }
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

// Add MCP tools demonstration endpoint
app.MapGet("/mcp/demo", async (McpClientService mcpClient) =>
{
    try
    {
        var tools = await mcpClient.GetAvailableToolsAsync().ConfigureAwait(false);
        
        var weatherDemo = await mcpClient.CallToolAsync("get_weather", new Dictionary<string, object>
        {
            ["location"] = "San Francisco",
            ["days"] = 2
        }).ConfigureAwait(false);

        var timeDemo = await mcpClient.CallToolAsync("get_time", new Dictionary<string, object>
        {
            ["timezone"] = "UTC",
            ["format"] = "detailed"
        }).ConfigureAwait(false);

        return Results.Ok(new
        {
            message = "MCP Tools Demo",
            available_tools = tools?.Tools?.Select(t => new { t.Name, t.Description }),
            weather_demo = weatherDemo,
            time_demo = timeDemo
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"MCP Demo failed: {ex.Message}");
    }
})
.WithName("McpDemo")
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
