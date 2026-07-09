using BuildWithAspire.ApiService.Services;

namespace BuildWithAspire.ApiService.UnitTests.Services;

public class ChatServiceSanitizeTests
{
    [Fact]
    public void SanitizeResponse_StripsToolCallTemplate_KeepsAnswer()
    {
        // qwen2.5 / Hermes-style local models echo the tool-call template into the text channel
        // in addition to the structured tool_calls the serving layer already parsed.
        var raw = "<tool_call>\n{\"name\": \"squareRoot\", \"arguments\": {\"number\": 1764}}\n</tool_call>\nThe square root of 1764 is 42.";

        var cleaned = ChatService.SanitizeResponse(raw);

        Assert.Equal("The square root of 1764 is 42.", cleaned);
        Assert.DoesNotContain("tool_call", cleaned, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizeResponse_StripsMultipleToolCallBlocks()
    {
        var raw = "<tool_call>{\"name\":\"a\"}</tool_call>Line one.\n<tool_call>{\"name\":\"b\"}</tool_call>\nLine two.";

        var cleaned = ChatService.SanitizeResponse(raw);

        Assert.DoesNotContain("tool_call", cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Line one.", cleaned);
        Assert.Contains("Line two.", cleaned);
    }

    [Fact]
    public void SanitizeResponse_RemovesOrphanedToolCallTag()
    {
        var raw = "Partial answer.\n<tool_call>\n{\"name\": \"weather\"}";

        var cleaned = ChatService.SanitizeResponse(raw);

        Assert.DoesNotContain("<tool_call>", cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("Partial answer.", cleaned);
    }

    [Fact]
    public void SanitizeResponse_LeavesCleanTextUnchanged()
    {
        var raw = "The weather in Seattle is 55F and cloudy.";

        var cleaned = ChatService.SanitizeResponse(raw);

        Assert.Equal(raw, cleaned);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SanitizeResponse_HandlesNullOrEmpty(string? input)
    {
        Assert.Equal(string.Empty, ChatService.SanitizeResponse(input));
    }
}
