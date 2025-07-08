using System.Runtime.CompilerServices;

namespace BuildWithAspire.ApiService.UnitTests.Mocks;

/// <summary>
/// Mock chat client for testing purposes.
/// </summary>
internal class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("Mock", null);

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Return a simple mock response
        var response = new ChatResponse(new List<ChatMessage>
        {
            new(ChatRole.Assistant, "Mock response for testing")
        });

        return await Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var update = new ChatResponseUpdate();
        update.Contents.Add(new TextContent("Mock streaming response"));
        yield return update;

        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    public TService? GetService<TService>(object? serviceKey = null)
    {
        return default(TService);
    }
}