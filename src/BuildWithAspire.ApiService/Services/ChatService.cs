using BuildWithAspire.ApiService.Models;
using BuildWithAspire.ApiService.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace BuildWithAspire.ApiService.Services;

public sealed class ChatService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatService> _logger;
    private readonly string _aiProvider;
    private readonly IMcpClient? _mcpClient;
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly int _timeoutMinutes;
    private readonly bool _mcpEnabled;

    public ChatService(IChatClient chatClient, ILogger<ChatService> logger, Kernel kernel, IChatCompletionService chatCompletion, IServiceProvider serviceProvider, IConfiguration configuration, IMcpClient? mcpClient = null)
    {
        _chatClient = chatClient;
        _logger = logger;
        _mcpClient = mcpClient;
        _kernel = kernel;
        _chatCompletion = chatCompletion;
        
        // Read configuration for timeouts and MCP settings
        _timeoutMinutes = configuration.GetValue<int>("AI:TimeoutMinutes", 10);
        _mcpEnabled = configuration.GetValue<bool>("MCP:ServerEnabled", _mcpClient != null);
        _aiProvider = configuration["AI:Provider"] ?? "ollama";

        // Add MCP tools plugin if MCP client is available and enabled
        if (_mcpClient != null && _mcpEnabled)
        {
            try
            {
                var pluginLogger = serviceProvider.GetRequiredService<ILogger<McpToolsPlugin>>();
                _kernel.Plugins.AddFromObject(new McpToolsPlugin(_mcpClient, pluginLogger), "McpTools");
                _logger.LogInformation("MCP Tools plugin added to Semantic Kernel for AI provider {Provider}", _aiProvider);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize MCP Tools plugin. Continuing without MCP integration.");
            }
        }
        else if (!_mcpEnabled)
        {
            _logger.LogInformation("MCP integration disabled by configuration");
        }
    }

    public async Task<string> ProcessMessage(string message)
    {
        _logger.LogInformation("Processing single message with Semantic Kernel. MessageLength: {MessageLength}", message?.Length ?? 0);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_timeoutMinutes));
            // Fast path: FoundryLocal currently returns 400 with SK OpenAI connector; use IChatClient directly
            if (_aiProvider.Equals("foundrylocal", StringComparison.OrdinalIgnoreCase))
            {
                var messages = new List<ChatMessage>
                {
                    new(ChatRole.System, "You are an AI demonstration application. You are a helpful chatbot. Respond responsibly."),
                    new(ChatRole.User, message ?? string.Empty)
                };

                // Weather enrichment via MCP tools (manual tool invocation since SK tool calling disabled here)
                if (IsWeatherRelated(message) && _mcpClient != null && _mcpEnabled)
                {
                    try
                    {
                        var weatherData = await _mcpClient.GetWeatherInfoAsync(message!, cts.Token).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(weatherData))
                        {
                            // Insert as an additional system message so model can ground response
                            messages.Insert(1, new ChatMessage(ChatRole.System, $"Weather tool data (fresh):\n{weatherData}\nUse this structured weather info when formulating your answer."));
                            _logger.LogInformation("Injected weather tool data into FoundryLocal prompt. Length={Length}", weatherData.Length);
                        }
                    }
                    catch (Exception wxEx)
                    {
                        _logger.LogWarning(wxEx, "Weather MCP tool invocation failed in FoundryLocal fast path");
                    }
                }
                var startDirect = DateTime.UtcNow;
                var resultDirect = await _chatClient.GetResponseAsync(messages, new ChatOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokens = 1000
                }, cts.Token).ConfigureAwait(false);
                var durationDirect = DateTime.UtcNow - startDirect;
                var textDirect = resultDirect?.Messages?.LastOrDefault()?.Text ?? string.Empty;
                _logger.LogInformation("FoundryLocal direct response generated. Length={Length}, Duration={Duration}ms", textDirect.Length, durationDirect.TotalMilliseconds);
                return textDirect;
            }
            // Create chat history for Semantic Kernel
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(@"You are an AI demonstration application.
                You are a helpful chatbot with access to various MCP tools exposed via Semantic Kernel functions.
                Available tool categories: Weather (current, forecast, convert temperature), System (datetime, system info, random number, base64 encode/decode), Math (arithmetic calculate, square root, power, fibonacci, prime check).
                Use tools whenever they can provide fresh, structured facts instead of guessing. Always prefer tool results over assumptions (e.g., for weather, calculations, encodings, primes).
                Only call tools relevant to the user's request; do not call unnecessary tools.
                Summarize tool outputs clearly in natural language and include key numbers or results.
                Respond responsibly and keep answers safe for work.");

            chatHistory.AddUserMessage(message ?? string.Empty);

            var startTime = DateTime.UtcNow;
            // cts already declared above for FoundryLocal fast path or will be created below for SK path

            var response = await ExecuteWithFoundryLocalFallbackAsync(chatHistory, cts.Token).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            var combinedResponse = response.Content ?? string.Empty;

            _logger.LogInformation("AI response with function calling generated successfully. ResponseLength: {ResponseLength}, Duration: {Duration}ms",
                combinedResponse.Length, duration.TotalMilliseconds);

            // Log if any functions were called
            if (response.Metadata?.ContainsKey("Usage") == true)
            {
                _logger.LogInformation("Semantic Kernel response included function calls and usage information");
            }

            return combinedResponse;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI request timed out after {TimeoutMinutes} minutes. MessageLength: {MessageLength}", _timeoutMinutes, message?.Length ?? 0);
            throw new InvalidOperationException($"The AI request timed out after {_timeoutMinutes} minutes. Please try again.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process single message with Semantic Kernel. MessageLength: {MessageLength}", message?.Length ?? 0);
            throw new InvalidOperationException($"Failed to process message: {ex.Message}", ex);
        }
    }

    public async Task<string> ProcessMessagesWithHistory(List<ChatMessageRequest> messages)
    {
        var messageCount = messages?.Count ?? 0;
        _logger.LogInformation("Processing messages with history using Semantic Kernel. MessageCount: {MessageCount}", messageCount);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_timeoutMinutes));
            // Fast path: FoundryLocal direct client usage to avoid 400 with SK OpenAI connector
            if (_aiProvider.Equals("foundrylocal", StringComparison.OrdinalIgnoreCase))
            {
                var directMessages = new List<ChatMessage>
                {
                    new(ChatRole.System, "You are an AI demonstration application. You are a helpful chatbot. Respond responsibly.")
                };

                // Keep only last user message for now (simplify until multi-turn validated)
                var lastUser = messages?.LastOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
                if (lastUser != null)
                {
                    // Weather enrichment for multi-turn FoundryLocal path
                    if (IsWeatherRelated(lastUser.Content) && _mcpClient != null && _mcpEnabled)
                    {
                        try
                        {
                            var weatherData = await _mcpClient.GetWeatherInfoAsync(lastUser.Content, cts.Token).ConfigureAwait(false);
                            if (!string.IsNullOrWhiteSpace(weatherData))
                            {
                                directMessages.Add(new ChatMessage(ChatRole.System, $"Weather tool data (fresh):\n{weatherData}\nIncorporate this weather information into your reply when relevant."));
                                _logger.LogInformation("Injected weather tool data into FoundryLocal multi-turn prompt. Length={Length}", weatherData.Length);
                            }
                        }
                        catch (Exception wxEx)
                        {
                            _logger.LogWarning(wxEx, "Weather MCP tool invocation failed in FoundryLocal multi-turn fast path");
                        }
                    }
                    directMessages.Add(new ChatMessage(ChatRole.User, lastUser.Content));
                }

                var startDirect = DateTime.UtcNow;
                var resultDirect = await _chatClient.GetResponseAsync(directMessages, new ChatOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokens = 1000
                }, cts.Token).ConfigureAwait(false);
                var durationDirect = DateTime.UtcNow - startDirect;
                var textDirect = resultDirect?.Messages?.LastOrDefault()?.Text ?? string.Empty;
                _logger.LogInformation("FoundryLocal direct conversation response generated. InputMessages={InputMessages}, Length={Length}, Duration={Duration}ms", messageCount, textDirect.Length, durationDirect.TotalMilliseconds);
                return textDirect;
            }
            // Create chat history for Semantic Kernel
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(@"You are an AI demonstration application.
                You are a helpful chatbot with access to various MCP tools exposed via Semantic Kernel functions.
                Available tool categories: Weather (current, forecast, convert temperature), System (datetime, system info, random number, base64 encode/decode), Math (arithmetic calculate, square root, power, fibonacci, prime check).
                Use tools whenever they can provide fresh, structured facts instead of guessing. Always prefer tool results over assumptions.
                Only call tools that directly support the user's goal; minimize redundant calls.
                Summarize tool outputs clearly in natural language and include key numbers or results.
                Respond responsibly and keep answers safe for work.");

            var userMessages = 0;
            var assistantMessages = 0;

            // Add all messages from history with validation
            AuthorRole? lastRole = null;
            foreach (var msg in messages ?? [])
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    // If we have consecutive user messages, only keep the latest one
                    if (lastRole == AuthorRole.User && chatHistory.Count > 1)
                    {
                        // Remove the previous user message to avoid consecutive user messages
                        var lastMessage = chatHistory.LastOrDefault();
                        if (lastMessage?.Role == AuthorRole.User)
                        {
                            chatHistory.RemoveAt(chatHistory.Count - 1);
                            userMessages--;
                            _logger.LogWarning("Removed duplicate consecutive user message to fix conversation structure");
                        }
                    }

                    chatHistory.AddUserMessage(msg.Content);
                    userMessages++;
                    lastRole = AuthorRole.User;
                }
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    chatHistory.AddAssistantMessage(msg.Content);
                    assistantMessages++;
                    lastRole = AuthorRole.Assistant;
                }
            }

            // Log the conversation structure
            _logger.LogInformation("Processing conversation with {TotalMessages} total messages. Chat structure: {ChatStructure}",
                chatHistory.Count, string.Join(" -> ", chatHistory.Select(m => m.Role.ToString())));

            var startTime = DateTime.UtcNow;
            // Create cancellation token for SK path
            // (Already created above; keep code if future refactor splits logic.)

            var response = await ExecuteWithFoundryLocalFallbackAsync(chatHistory, cts.Token).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            var combinedResponse = response.Content ?? string.Empty;

            _logger.LogInformation("AI conversation with function calling generated successfully. InputMessages: {InputMessages}, ResponseLength: {ResponseLength}, Duration: {Duration}ms",
                messageCount, combinedResponse.Length, duration.TotalMilliseconds);

            // Log if any functions were called
            if (response.Metadata?.ContainsKey("Usage") == true)
            {
                _logger.LogInformation("Semantic Kernel conversation response included function calls and usage information");
            }

            return combinedResponse;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI conversation request timed out after {TimeoutMinutes} minutes. MessageCount: {MessageCount}", _timeoutMinutes, messageCount);
            throw new InvalidOperationException($"The AI request timed out after {_timeoutMinutes} minutes. Please try again with a shorter conversation.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process conversation messages with Semantic Kernel. MessageCount: {MessageCount}", messageCount);
            throw new InvalidOperationException($"Failed to process messages: {ex.Message}", ex);
        }
    }

    private static bool IsWeatherRelated(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        var weatherKeywords = new[]
        {
            "weather", "temperature", "forecast", "rain", "sunny", "cloudy", "hot", "cold",
            "degrees", "celsius", "fahrenheit", "storm", "snow", "wind", "humidity"
        };

        var lowerMessage = message.ToLower();
    return weatherKeywords.Any(lowerMessage.Contains);
    }

    private OpenAIPromptExecutionSettings CreateExecutionSettings(bool disableTools = false)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7f,
            MaxTokens = 1000
        };

        // Only enable tool calling when not disabled and provider supports it
        if (!disableTools && !_aiProvider.Equals("foundrylocal", StringComparison.OrdinalIgnoreCase))
        {
            settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
        }

        return settings;
    }

    private async Task<ChatMessageContent> ExecuteWithFoundryLocalFallbackAsync(ChatHistory chatHistory, CancellationToken ct)
    {
        var primarySettings = CreateExecutionSettings();
        try
        {
            return await _chatCompletion.GetChatMessageContentAsync(chatHistory, primarySettings, _kernel, ct).ConfigureAwait(false);
        }
        catch (Microsoft.SemanticKernel.HttpOperationException ex) when (_aiProvider.Equals("foundrylocal", StringComparison.OrdinalIgnoreCase))
        {
            // Retry once without tool calling if 400 (likely unsupported parameter)
            if (ex.Message.Contains("400") && primarySettings.ToolCallBehavior is not null)
            {
                _logger.LogWarning(ex, "FoundryLocal returned 400 with tool calling enabled. Retrying without tool calls.");
                var fallbackSettings = CreateExecutionSettings(disableTools: true);
                return await _chatCompletion.GetChatMessageContentAsync(chatHistory, fallbackSettings, _kernel, ct).ConfigureAwait(false);
            }
            throw;
        }
    }
}
