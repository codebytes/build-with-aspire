using BuildWithAspire.ApiService.Data;
using BuildWithAspire.ApiService.Extensions;
using BuildWithAspire.ApiService.Models;
using BuildWithAspire.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
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
