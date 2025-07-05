using System.Net;
using System.Net.Http.Json;
using BuildWithAspire.Web.Models;

namespace BuildWithAspire.Web.Clients;

public class ConversationApiClient(HttpClient httpClient)
{
    public async Task<List<ConversationSummary>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<ConversationSummary>>("conversations", cancellationToken);
        return response ?? new List<ConversationSummary>();
    }

    public async Task<ConversationDetail?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"conversations/{id}", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ConversationDetail>(cancellationToken);
        return result;
    }

    public async Task<ConversationDetail> CreateConversationAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("conversations", new { name }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var conversation = await response.Content.ReadFromJsonAsync<ConversationDetail>(cancellationToken);
        return conversation!;
    }

    public async Task<MessageResponse> SendMessageAsync(Guid conversationId, string message, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"conversations/{conversationId}/messages", new { message }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var messageResponse = await response.Content.ReadFromJsonAsync<MessageResponse>(cancellationToken);
        return messageResponse!;
    }

    public async Task DeleteConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"conversations/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
} 