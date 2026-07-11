using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BuildWithAspire.MCPServer.Tools;

/// <summary>
/// Text manipulation and analysis tools for MCP clients.
/// All operations are deterministic, read-only, and side-effect free, which makes
/// them ideal for demonstrating tool-calling with small local models.
/// </summary>
[McpServerToolType]
public sealed class TextTools
{
    private static readonly char[] SentenceTerminators = ['.', '!', '?'];

    private readonly ILogger<TextTools> _logger;

    public TextTools(ILogger<TextTools> logger)
    {
        _logger = logger;
    }

    [McpServerTool(Name = "countText", Title = "Text Statistics", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Counts the number of characters, words, lines, and sentences in the supplied text.")]
    public TextStatistics CountText(
        [Description("The text to analyze")] string text)
    {
        _logger.LogInformation("MCP Tool 'countText' called with text length={Length}", text?.Length ?? 0);

        if (string.IsNullOrEmpty(text))
        {
            return new TextStatistics(0, 0, 0, 0, 0);
        }

        var characters = text.Length;
        var charactersNoWhitespace = text.Count(c => !char.IsWhiteSpace(c));
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var lines = text.Split('\n').Length;
        var sentences = text.Split(SentenceTerminators, StringSplitOptions.RemoveEmptyEntries)
            .Count(s => !string.IsNullOrWhiteSpace(s));

        return new TextStatistics(characters, charactersNoWhitespace, words, lines, sentences);
    }

    [McpServerTool(Name = "transformCase", Title = "Transform Text Case", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Transforms text casing. Supported modes: 'upper', 'lower', 'title', 'reverse'.")]
    public TextTransformResult TransformCase(
        [Description("The text to transform")] string text,
        [Description("Transform mode: 'upper', 'lower', 'title', or 'reverse'")] string mode = "upper")
    {
        _logger.LogInformation("MCP Tool 'transformCase' called with mode={Mode}", mode);

        text ??= string.Empty;
        var normalizedMode = mode?.Trim().ToLowerInvariant() ?? "upper";

        switch (normalizedMode)
        {
            case "upper":
                return new TextTransformResult(text.ToUpperInvariant(), normalizedMode, true, "Converted to upper case");
            case "lower":
                return new TextTransformResult(text.ToLowerInvariant(), normalizedMode, true, "Converted to lower case");
            case "title":
                var title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
                return new TextTransformResult(title, normalizedMode, true, "Converted to title case");
            case "reverse":
                var reversed = new string(text.Reverse().ToArray());
                return new TextTransformResult(reversed, normalizedMode, true, "Reversed the text");
            default:
                return new TextTransformResult(text, normalizedMode, false, $"Unknown mode: {mode}. Use 'upper', 'lower', 'title', or 'reverse'.");
        }
    }

    [McpServerTool(Name = "slugify", Title = "URL Slug Generator", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Converts text into a URL-friendly slug (lowercase, hyphen-separated, alphanumeric).")]
    public SlugResult Slugify(
        [Description("The text to convert into a slug")] string text)
    {
        _logger.LogInformation("MCP Tool 'slugify' called with text length={Length}", text?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(text))
        {
            return new SlugResult(string.Empty, "Empty input provided");
        }

        var builder = new StringBuilder(text.Length);
        var previousWasHyphen = false;

        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousWasHyphen = false;
            }
            else if (!previousWasHyphen)
            {
                builder.Append('-');
                previousWasHyphen = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return new SlugResult(slug, $"Generated slug from {text.Length} characters");
    }
}

public record TextStatistics(
    int Characters,
    int CharactersNoWhitespace,
    int Words,
    int Lines,
    int Sentences
);

public record TextTransformResult(
    string Result,
    string Mode,
    bool Success,
    string Message
);

public record SlugResult(
    string Slug,
    string Message
);
