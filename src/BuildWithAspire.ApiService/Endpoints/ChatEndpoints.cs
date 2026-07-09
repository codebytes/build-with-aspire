using BuildWithAspire.ApiService.Data;
using BuildWithAspire.ApiService.Models;
using BuildWithAspire.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildWithAspire.ApiService.Endpoints;

/// <summary>
/// Chat conversation and message endpoints.
/// </summary>
public static class ChatEndpoints
{
    public static RouteGroupBuilder MapChatEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/conversations");

        group.MapGet("/", GetConversations)
            .WithName("GetConversations");

        group.MapGet("/{id}", GetConversation)
            .WithName("GetConversation");

        group.MapPost("/", CreateConversation)
            .WithName("CreateConversation");

        group.MapPost("/{id}/messages", SendMessage)
            .WithName("SendMessage")
            .RequireRateLimiting("chat");

        group.MapDelete("/{id}", DeleteConversation)
            .WithName("DeleteConversation");

        // Legacy endpoint for backward compatibility
        routes.MapGet("/chat", async (ChatService chatService, string message) =>
                await chatService.ProcessMessage(message).ConfigureAwait(false))
            .WithName("GetChat")
            .RequireRateLimiting("chat");

        return group;
    }

    private static async Task<IResult> GetConversations(ChatDbContext db, ILogger<Program> logger)
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
            logger.LogError(ex, "Error retrieving conversations");
            return Results.Problem("Failed to retrieve conversations", statusCode: 500);
        }
    }

    private static async Task<IResult> GetConversation(Guid id, ChatDbContext db, ILogger<Program> logger)
    {
        try
        {
            var conversation = await db.Conversations
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);

            return conversation == null ? Results.NotFound() : Results.Ok(conversation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving conversation: {ConversationId}", id);
            return Results.Problem("Failed to retrieve conversation", statusCode: 500);
        }
    }

    private static async Task<IResult> CreateConversation([FromBody] CreateConversationRequest request, ChatDbContext db, ILogger<Program> logger)
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
            logger.LogError(ex, "Error creating conversation: {ConversationName}", request.Name);
            return Results.Problem("Failed to create conversation", statusCode: 500);
        }
    }

    private static async Task<IResult> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request,
        ChatService chatService,
        ChatDbContext db,
        ILogger<Program> logger)
    {
        try
        {
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

            db.Messages.Add(userMessage);
            conversation.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync().ConfigureAwait(false);

            // Get conversation history
            var messages = await db.Messages
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageRequest { Role = m.Role, Content = m.Content })
                .ToListAsync().ConfigureAwait(false);

            // Get AI response
            string aiResponse;
            try
            {
                aiResponse = await chatService.ProcessMessagesWithHistory(messages).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI service error for conversation {ConversationId}", id);
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

            db.Messages.Add(assistantMessage);
            await db.SaveChangesAsync().ConfigureAwait(false);

            return Results.Ok(new { response = aiResponse });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SendMessage for conversation {ConversationId}", id);
            return Results.Problem("Internal server error", statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteConversation(Guid id, ChatDbContext db)
    {
        var conversation = await db.Conversations.FindAsync(id).ConfigureAwait(false);
        if (conversation == null)
        {
            return Results.NotFound();
        }

        db.Conversations.Remove(conversation);
        await db.SaveChangesAsync().ConfigureAwait(false);

        return Results.NoContent();
    }
}
