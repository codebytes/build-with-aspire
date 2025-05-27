using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using StackExchange.Redis;
using System.Text.Json;
using AuthorRole = Microsoft.SemanticKernel.ChatCompletion.AuthorRole;

namespace BuildWithAspire.ApiService.Services;

public class ChatService
{
    private readonly Kernel _kernel;
    private readonly IConnectionMultiplexer _redis;
    private static readonly string SystemPrompt = @"You are an AI demonstration application. 
            You are a helpful chatbot. 
            Respond to the user' input responsibly.
            All responses should be safe for work.";

    public ChatService(Kernel kernel, IConnectionMultiplexer redis)
    {
        _kernel = kernel;
        _redis = redis;
    }

    public async Task<string> ProcessMessage(string chatId, string message)
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var chatCompletionService = _kernel.GetRequiredService<IChatClient>()
               .AsChatCompletionService();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        // Load chat history from Redis or create a new one
        ChatHistory history = await GetChatHistoryAsync(chatId) ?? [];
        
        // If history is empty, add the system message
        if (history.Count == 0)
        {
            history.AddSystemMessage(SystemPrompt);
        }

        // Add user message to history
        history.AddUserMessage(message);

        // Get the response from the AI
        var response = chatCompletionService.GetStreamingChatMessageContentsAsync(history, kernel: _kernel);

        string combinedResponse = string.Empty;
        await foreach (var messageResponse in response)
        {
            combinedResponse += messageResponse;
        }

        // Add the message from the agent to the chat history
        history.AddAssistantMessage(combinedResponse);

        // Save updated history to Redis
        await SaveChatHistoryAsync(chatId, history);
        
        return combinedResponse;
    }

    public async Task<List<ChatMessageModel>> GetChatHistoryMessagesAsync(string chatId)
    {
        var history = await GetChatHistoryAsync(chatId);
        if (history == null || history.Count == 0)
        {
            return [];
        }

        var messages = new List<ChatMessageModel>();
        foreach (var msg in history)
        {
            // Skip system messages in the UI display
            if (msg.Role == AuthorRole.System)
                continue;
                
            messages.Add(new ChatMessageModel
            {
                Role = ConvertToAIRole(msg.Role),
                Content = msg.Content ?? string.Empty
            });
        }
        
        return messages;
    }
    
    public async Task<bool> DeleteChatHistoryAsync(string chatId)
    {
        var db = _redis.GetDatabase();
        return await db.KeyDeleteAsync($"chat:{chatId}");
    }

    private async Task<ChatHistory?> GetChatHistoryAsync(string chatId)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"chat:{chatId}");
        
        if (value.IsNullOrEmpty)
        {
            return null;
        }
        
        try
        {
            // Deserialize the chat history from JSON
            var messages = JsonSerializer.Deserialize<List<SerializableChatMessage>>(value.ToString());
            var history = new ChatHistory();
            
            foreach (var msg in messages!)
            {
                history.Add(new ChatMessageContent(ConvertToAuthorRole(msg.Role), msg.Content));
            }
            
            return history;
        }
        catch
        {
            return null;
        }
    }
    
    private async Task SaveChatHistoryAsync(string chatId, ChatHistory history)
    {
        var db = _redis.GetDatabase();
        
        // Convert to a serializable form
        var messages = history.Select(msg => new SerializableChatMessage
        {
            Role = ConvertToAIRole(msg.Role),
            Content = msg.Content ?? string.Empty
        }).ToList();
        
        var json = JsonSerializer.Serialize(messages);
        await db.StringSetAsync($"chat:{chatId}", json);
    }    private static ChatRole ConvertToAIRole(AuthorRole authorRole)
    {
        if (authorRole == AuthorRole.System) return ChatRole.System;
        if (authorRole == AuthorRole.User) return ChatRole.User;
        if (authorRole == AuthorRole.Assistant) return ChatRole.Assistant;
        return ChatRole.User;
    }

    private static AuthorRole ConvertToAuthorRole(ChatRole chatRole)
    {
        if (chatRole == ChatRole.System) return AuthorRole.System;
        if (chatRole == ChatRole.User) return AuthorRole.User;
        if (chatRole == ChatRole.Assistant) return AuthorRole.Assistant;
        return AuthorRole.User;
    }
    
    private class SerializableChatMessage
    {
        public ChatRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}

public class ChatMessageModel
{
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
}