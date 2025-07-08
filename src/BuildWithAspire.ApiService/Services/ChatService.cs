using BuildWithAspire.ApiService.Configuration;
using BuildWithAspire.ApiService.Models;
using Microsoft.Extensions.AI;

namespace BuildWithAspire.ApiService.Services;

public class ChatService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatService> _logger;
    private readonly AIConfiguration.AISettings _aiSettings;

    public ChatService(IChatClient chatClient, ILogger<ChatService> logger, AIConfiguration.AISettings aiSettings)
    {
        _chatClient = chatClient;
        _logger = logger;
        _aiSettings = aiSettings;
    }

    public async Task<string> ProcessMessage(string message)
    {
        _logger.LogInformation("Processing single message. MessageLength: {MessageLength}", message?.Length ?? 0);

        try
        {
            // Use injected IChatClient directly for better token usage tracking
            var chatMessages = new List<ChatMessage>
            {
                new(ChatRole.System, @"You are an AI demonstration application.
                    You are a helpful chatbot.
                    Respond to the user' input responsibly.
                    All responses should be safe for work."),
                new(ChatRole.User, message ?? string.Empty)
            };

            _logger.LogDebug("Added user message to chat history");

            var startTime = DateTime.UtcNow;
            var response = await _chatClient.GetResponseAsync(chatMessages).ConfigureAwait(false);
            var duration = DateTime.UtcNow - startTime;

            var combinedResponse = response.Text ?? string.Empty;

            // Log token usage if available
            if (response.Usage != null)
            {
                _logger.LogInformation("AI response generated successfully. ResponseLength: {ResponseLength}, Duration: {Duration}ms, InputTokens: {InputTokens}, OutputTokens: {OutputTokens}, TotalTokens: {TotalTokens}",
                    combinedResponse.Length, duration.TotalMilliseconds,
                    response.Usage.InputTokenCount, response.Usage.OutputTokenCount, response.Usage.TotalTokenCount);

                // Log cost-focused metrics for monitoring
                _logger.LogInformation("Token usage metrics - Provider: {Provider}, Model: {Model}, InputTokens: {InputTokens}, OutputTokens: {OutputTokens}, ConversationLength: 1",
                    _aiSettings.Provider, _aiSettings.Model, response.Usage.InputTokenCount, response.Usage.OutputTokenCount);
            }
            else
            {
                _logger.LogInformation("AI response generated successfully. ResponseLength: {ResponseLength}, Duration: {Duration}ms",
                    combinedResponse.Length, duration.TotalMilliseconds);
            }

            return combinedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process single message. MessageLength: {MessageLength}", message?.Length ?? 0);
            throw new InvalidOperationException($"Failed to process message: {ex.Message}", ex);
        }
    }

    public async Task<string> ProcessMessagesWithHistory(List<ChatMessageRequest> messages)
    {
        var messageCount = messages?.Count ?? 0;
        _logger.LogInformation("Processing messages with history. MessageCount: {MessageCount}", messageCount);

        try
        {
            // Use injected IChatClient directly for better token usage tracking
            var chatMessages = new List<ChatMessage>
            {
                new(ChatRole.System, @"You are an AI demonstration application.
                    You are a helpful chatbot.
                    Respond to the user' input responsibly.
                    All responses should be safe for work.")
            };

            var userMessages = 0;
            var assistantMessages = 0;

            // Add all messages from history
            foreach (var msg in messages ?? [])
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    chatMessages.Add(new ChatMessage(ChatRole.User, msg.Content));
                    userMessages++;
                }
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    chatMessages.Add(new ChatMessage(ChatRole.Assistant, msg.Content));
                    assistantMessages++;
                }
            }

            _logger.LogDebug("Chat history prepared. UserMessages: {UserMessages}, AssistantMessages: {AssistantMessages}",
                userMessages, assistantMessages);

            var startTime = DateTime.UtcNow;
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            // Use non-streaming response to get usage information
            var response = await _chatClient.GetResponseAsync(chatMessages, cancellationToken: cts.Token).ConfigureAwait(false);
            var duration = DateTime.UtcNow - startTime;

            var combinedResponse = response.Text ?? string.Empty;

            // Log token usage if available
            if (response.Usage != null)
            {
                _logger.LogInformation("AI conversation response generated successfully. InputMessages: {InputMessages}, ResponseLength: {ResponseLength}, Duration: {Duration}ms, InputTokens: {InputTokens}, OutputTokens: {OutputTokens}, TotalTokens: {TotalTokens}",
                    messageCount, combinedResponse.Length, duration.TotalMilliseconds,
                    response.Usage.InputTokenCount, response.Usage.OutputTokenCount, response.Usage.TotalTokenCount);

                // Log cost-focused metrics for monitoring
                _logger.LogInformation("Token usage metrics - Provider: {Provider}, Model: {Model}, InputTokens: {InputTokens}, OutputTokens: {OutputTokens}, ConversationLength: {ConversationLength}",
                    _aiSettings.Provider, _aiSettings.Model, response.Usage.InputTokenCount, response.Usage.OutputTokenCount, messageCount);
            }
            else
            {
                _logger.LogInformation("AI conversation response generated successfully. InputMessages: {InputMessages}, ResponseLength: {ResponseLength}, Duration: {Duration}ms",
                    messageCount, combinedResponse.Length, duration.TotalMilliseconds);
            }

            return combinedResponse;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI conversation request timed out. MessageCount: {MessageCount}", messageCount);
            throw new InvalidOperationException("The AI request timed out. Please try again with a shorter conversation.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process conversation messages. MessageCount: {MessageCount}", messageCount);
            throw new InvalidOperationException($"Failed to process messages: {ex.Message}", ex);
        }
    }
}
