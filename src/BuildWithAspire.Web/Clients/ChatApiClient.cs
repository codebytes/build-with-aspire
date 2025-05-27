using Microsoft.Extensions.AI;
using System.Net.Http.Json;

namespace BuildWithAspire.Web.Clients;

public class ChatApiClient(HttpClient httpClient)
{
    public async Task<string> GetChatAsync(string chatId, string message, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"?chatId={chatId}&message={message}", cancellationToken);
        var chatResponse = response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync(cancellationToken)
            : throw new Exception(response.ReasonPhrase);
        return chatResponse ?? "No Response";
    }
    
    public async Task<List<ChatMessage>> GetChatHistoryAsync(string chatId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"?chatId={chatId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            throw new Exception(response.ReasonPhrase);
            
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessage>>(cancellationToken: cancellationToken);
        return messages ?? [];
    }
    
    public async Task<bool> CreateNewChatAsync(string chatId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/{chatId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            return false;
            
        var result = await response.Content.ReadFromJsonAsync<DeleteChatResult>(cancellationToken: cancellationToken);
        return result?.Success ?? false;
    }
    
    private class DeleteChatResult
    {
        public bool Success { get; set; }
    }
}

public class ChatMessage
{
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
}
