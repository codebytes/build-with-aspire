using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;

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
            //Write the response to the console
            combinedResponse += messageResponse;
        }

        // Add the message from the agent to the chat history
        history.AddAssistantMessage(combinedResponse);
        return combinedResponse;
    }
}