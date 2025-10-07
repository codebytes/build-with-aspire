using BuildWithAspire.Abstractions;
using BuildWithAspire.ApiService.Models;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

namespace BuildWithAspire.ApiService.Services;

public class ChatService
{
    private readonly AIAgent _agent;
    private readonly ILogger<ChatService> _logger;
    private readonly AIConfiguration.AISettings _aiSettings;

    public ChatService(IChatClient chatClient, ILogger<ChatService> logger, AIConfiguration.AISettings aiSettings)
    {
        _logger = logger;
        _aiSettings = aiSettings;

        // Create an AI Agent using the Microsoft Agent Framework
        _agent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "ChatAssistant",
                Instructions = @"You are an AI demonstration application.
                    You are a helpful chatbot.
                    Respond to the user's input responsibly.
                    All responses should be safe for work."
            });
    }

    public async Task<string> ProcessMessage(string message)
    {
        _logger.LogInformation("Processing single message. MessageLength: {MessageLength}", message?.Length ?? 0);

        try
        {
            _logger.LogDebug("Running agent with user message");

            var startTime = DateTime.UtcNow;
            var response = await _agent.RunAsync(message ?? string.Empty).ConfigureAwait(false);
            var duration = DateTime.UtcNow - startTime;

            var combinedResponse = response.Text ?? string.Empty;

            _logger.LogInformation("AI response generated successfully. ResponseLength: {ResponseLength}, Duration: {Duration}ms",
                combinedResponse.Length, duration.TotalMilliseconds);

            // Log metrics for monitoring
            _logger.LogInformation("Agent metrics - Provider: {Provider}, Model: {Model}, ConversationLength: 1",
                _aiSettings.Provider, _aiSettings.Model);

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
            // Build conversation history for the agent
            var chatMessages = new List<ChatMessage>();

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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_aiSettings.TimeoutSeconds));

            // Run agent with conversation history
            var response = await _agent.RunAsync(chatMessages, cancellationToken: cts.Token).ConfigureAwait(false);
            var duration = DateTime.UtcNow - startTime;

            var combinedResponse = response.Text ?? string.Empty;

            _logger.LogInformation("AI conversation response generated successfully. InputMessages: {InputMessages}, ResponseLength: {ResponseLength}, Duration: {Duration}ms",
                messageCount, combinedResponse.Length, duration.TotalMilliseconds);

            // Log metrics for monitoring
            _logger.LogInformation("Agent metrics - Provider: {Provider}, Model: {Model}, ConversationLength: {ConversationLength}",
                _aiSettings.Provider, _aiSettings.Model, messageCount);

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
