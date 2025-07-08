using System.Net;
using BuildWithAspire.Web.Models;

namespace BuildWithAspire.Web.Clients;

public class ConversationApiClient(HttpClient httpClient, ILogger<ConversationApiClient> logger)
{
    public async Task<List<ConversationSummary>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching conversations list");

        try
        {
            var startTime = DateTime.UtcNow;
            var response = await httpClient.GetFromJsonAsync<List<ConversationSummary>>("conversations", cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            var conversations = response ?? new List<ConversationSummary>();
            logger.LogInformation("Successfully fetched conversations. Count: {ConversationCount}, Duration: {Duration}ms",
                conversations.Count, duration.TotalMilliseconds);

            return conversations;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch conversations");
            throw;
        }
    }

    public async Task<ConversationDetail?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching conversation details. ConversationId: {ConversationId}", id);

        try
        {
            var startTime = DateTime.UtcNow;
            var response = await httpClient.GetAsync($"conversations/{id}", cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("Conversation not found. ConversationId: {ConversationId}, Duration: {Duration}ms",
                    id, duration.TotalMilliseconds);
                return null;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ConversationDetail>(cancellationToken);

            logger.LogInformation("Successfully fetched conversation details. ConversationId: {ConversationId}, MessageCount: {MessageCount}, Duration: {Duration}ms",
                id, result?.Messages?.Count ?? 0, duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch conversation details. ConversationId: {ConversationId}", id);
            throw;
        }
    }

    public async Task<ConversationDetail> CreateConversationAsync(string name, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating new conversation. Name: {ConversationName}", name);

        try
        {
            var startTime = DateTime.UtcNow;
            var response = await httpClient.PostAsJsonAsync("conversations", new { name }, cancellationToken);
            response.EnsureSuccessStatusCode();
            var conversation = await response.Content.ReadFromJsonAsync<ConversationDetail>(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            logger.LogInformation("Successfully created conversation. ConversationId: {ConversationId}, Name: {ConversationName}, Duration: {Duration}ms",
                conversation!.Id, name, duration.TotalMilliseconds);

            return conversation!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create conversation. Name: {ConversationName}", name);
            throw;
        }
    }

    public async Task<MessageResponse> SendMessageAsync(Guid conversationId, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending message to conversation. ConversationId: {ConversationId}, MessageLength: {MessageLength}",
            conversationId, message?.Length ?? 0);

        try
        {
            var startTime = DateTime.UtcNow;
            var response = await httpClient.PostAsJsonAsync($"conversations/{conversationId}/messages", new { message }, cancellationToken);
            response.EnsureSuccessStatusCode();
            var messageResponse = await response.Content.ReadFromJsonAsync<MessageResponse>(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            logger.LogInformation("Successfully sent message and received response. ConversationId: {ConversationId}, ResponseLength: {ResponseLength}, Duration: {Duration}ms",
                conversationId, messageResponse!.Response?.Length ?? 0, duration.TotalMilliseconds);

            return messageResponse!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message. ConversationId: {ConversationId}, MessageLength: {MessageLength}",
                conversationId, message?.Length ?? 0);
            throw;
        }
    }

    public async Task DeleteConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting conversation. ConversationId: {ConversationId}", id);

        try
        {
            var startTime = DateTime.UtcNow;
            var response = await httpClient.DeleteAsync($"conversations/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var duration = DateTime.UtcNow - startTime;

            logger.LogInformation("Successfully deleted conversation. ConversationId: {ConversationId}, Duration: {Duration}ms",
                id, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete conversation. ConversationId: {ConversationId}", id);
            throw;
        }
    }
}