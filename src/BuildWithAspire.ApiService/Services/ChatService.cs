using BuildWithAspire.Abstractions;
using BuildWithAspire.ApiService.Models;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

namespace BuildWithAspire.ApiService.Services;

public class ChatService
{
    private AIAgent? _agent;
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatService> _logger;
    private readonly AIConfiguration.AISettings _aiSettings;
    private readonly IDynamicMcpToolConverter _toolConverter;
    private IEnumerable<AIFunction>? _tools;
    private bool _isInitialized;

    public ChatService(
        IChatClient chatClient,
        IDynamicMcpToolConverter toolConverter,
        ILogger<ChatService> logger,
        AIConfiguration.AISettings aiSettings)
    {
        _chatClient = chatClient;
        _toolConverter = toolConverter;
        _logger = logger;
        _aiSettings = aiSettings;
        _isInitialized = false;

        _logger.LogInformation("ChatService initialized, tools will be loaded dynamically from MCP server on first use");
    }

    /// <summary>
    /// Ensures the agent is initialized with dynamic tools from the MCP server
    /// </summary>
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized && _agent != null)
        {
            return;
        }

        _logger.LogInformation("Initializing AI Agent with dynamic MCP tools...");

        // Dynamically load tools from MCP server
        _tools = await _toolConverter.GetAllToolsAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Loaded {ToolCount} tools from MCP server", _tools?.Count() ?? 0);

        // Create a chat client with function invocation enabled and tools configured via middleware
        var toolEnabledClient = _chatClient.AsBuilder()
            .Use((chatMessages, options, next, cancellationToken) =>
            {
                // Inject tools into ChatOptions for every request
                if (options is not null && options.Tools?.Count is null or 0)
                {
                    options.Tools = _tools?.Select(t => (AITool)t).ToList();
                    _logger.LogInformation("Middleware: Injected {ToolCount} tools into ChatOptions", options.Tools?.Count ?? 0);
                }

                var toolCount = options?.Tools?.Count ?? 0;
                _logger.LogInformation("Middleware: Sending request to model with {ToolCount} tools available", toolCount);
                var result = next(chatMessages, options, cancellationToken);
                _logger.LogInformation("Middleware: Received response from model");
                return result;
            })
            .UseFunctionInvocation()
            .Build();

        // Create an AI Agent using the Microsoft Agent Framework with dynamic tools
        _agent = new ChatClientAgent(
            toolEnabledClient,
            new ChatClientAgentOptions
            {
                Name = "ChatAssistant",
                Instructions = @"You are an AI demonstration application.
                    You are a helpful chatbot with access to various tools dynamically discovered from the MCP server.
                    Use the available tools when appropriate to provide accurate information.
                    When a user asks for something that a tool can provide (like a random number, weather, calculations, etc.), USE THE TOOL instead of making up an answer.
                    Respond to the user's input responsibly.
                    All responses should be safe for work."
            });

        _isInitialized = true;
        _logger.LogInformation("AI Agent initialized with {ToolCount} MCP tools available", _tools?.Count() ?? 0);
    }

    public async Task<string> ProcessMessage(string message)
    {
        _logger.LogInformation("Processing single message. MessageLength: {MessageLength}", message?.Length ?? 0);

        try
        {
            // Ensure agent is initialized with dynamic MCP tools
            await EnsureInitializedAsync().ConfigureAwait(false);

            if (_agent == null)
            {
                throw new InvalidOperationException("AI Agent failed to initialize");
            }

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
            // Ensure agent is initialized with dynamic MCP tools
            await EnsureInitializedAsync().ConfigureAwait(false);

            if (_agent == null)
            {
                throw new InvalidOperationException("AI Agent failed to initialize");
            }

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
