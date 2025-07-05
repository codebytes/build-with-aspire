using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using BuildWithAspire.ApiService.Models;

namespace BuildWithAspire.ApiService.Services;

public class ChatService
{
    private readonly Kernel _kernel;

    public ChatService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> ProcessMessage(string message)
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var chatCompletionService = _kernel.GetRequiredService<IChatClient>()
               .AsChatCompletionService();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        ChatHistory history = [];
        history.AddSystemMessage(@"You are an AI demonstration application. 
            You are a helpful chatbot. 
            Respond to the user' input responsibly.
            All responses should be safe for work.");
        // Get user input
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
        return combinedResponse;
    }

    public async Task<string> ProcessMessagesWithHistory(List<ChatMessageRequest> messages)
    {
        try
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var chatCompletionService = _kernel.GetRequiredService<IChatClient>()
                   .AsChatCompletionService();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            ChatHistory history = [];
            history.AddSystemMessage(@"You are an AI demonstration application. 
                You are a helpful chatbot. 
                Respond to the user' input responsibly.
                All responses should be safe for work.");
            
            // Add all messages from history
            foreach (var msg in messages)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    history.AddUserMessage(msg.Content);
                }
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    history.AddAssistantMessage(msg.Content);
                }
            }
            
            // Get the response from the AI with timeout handling
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var response = chatCompletionService.GetStreamingChatMessageContentsAsync(history, kernel: _kernel, cancellationToken: cts.Token);

            string combinedResponse = string.Empty;
            await foreach (var messageResponse in response.WithCancellation(cts.Token))
            {
                combinedResponse += messageResponse;
            }

            return combinedResponse;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to process messages: {ex.Message}", ex);
        }
    }
}