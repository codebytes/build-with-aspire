using System.ComponentModel;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BuildWithAspire.MCPServer.Tools;

/// <summary>
/// System utilities and information tools for MCP clients.
/// Provides safe system information and basic operations.
/// </summary>
[McpServerToolType]
public sealed class SystemTools
{
    private readonly ILogger<SystemTools> _logger;

    public SystemTools(ILogger<SystemTools> logger)
    {
        _logger = logger;
    }

    [McpServerTool(Name = "getCurrentDateTime")]
    [Description("Gets the current date and time information.")]
    public DateTimeInfo GetCurrentDateTime()
    {
        _logger.LogInformation("MCP Tool 'getCurrentDateTime' called");
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;

        return new DateTimeInfo(
            LocalTime: now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            UtcTime: utcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone: TimeZoneInfo.Local.DisplayName,
            UnixTimestamp: ((DateTimeOffset)now).ToUnixTimeSeconds()
        );
    }

    [McpServerTool(Name = "getSystemInfo")]
    [Description("Gets basic system information including OS and .NET version.")]
    public SystemInfo GetSystemInfo()
    {
        _logger.LogInformation("MCP Tool 'getSystemInfo' called");
        return new SystemInfo(
            OperatingSystem: Environment.OSVersion.ToString(),
            MachineName: Environment.MachineName,
            DotNetVersion: Environment.Version.ToString(),
            ProcessorCount: Environment.ProcessorCount,
            WorkingDirectory: Environment.CurrentDirectory
        );
    }

    [McpServerTool(Name = "generateRandomNumber")]
    [Description("Generates a random number within the specified range.")]
    public RandomNumber GenerateRandomNumber(
        [Description("Minimum value (inclusive)")] int min = 1,
        [Description("Maximum value (exclusive)")] int max = 100)
    {
        _logger.LogInformation("MCP Tool 'generateRandomNumber' called with min={Min}, max={Max}", min, max);
        if (min >= max)
        {
            max = min + 1;
        }

        var value = Random.Shared.Next(min, max);
        return new RandomNumber(value, min, max - 1);
    }

    [McpServerTool(Name = "encodeToBase64")]
    [Description("Encodes text to Base64 format.")]
    public EncodingResult EncodeToBase64(
        [Description("Text to encode")] string text)
    {
        _logger.LogInformation("MCP Tool 'encodeToBase64' called with text length={Length}", text?.Length ?? 0);
        if (string.IsNullOrEmpty(text))
        {
            return new EncodingResult("", "base64", "Empty input provided");
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var encoded = Convert.ToBase64String(bytes);
            return new EncodingResult(encoded, "base64", $"Encoded {text.Length} characters to Base64");
        }
        catch (Exception ex)
        {
            return new EncodingResult("", "base64", $"Encoding failed: {ex.Message}");
        }
    }

    [McpServerTool(Name = "decodeFromBase64")]
    [Description("Decodes Base64 text to plain text.")]
    public EncodingResult DecodeFromBase64(
        [Description("Base64 text to decode")] string base64Text)
    {
        _logger.LogInformation("MCP Tool 'decodeFromBase64' called with base64 text length={Length}", base64Text?.Length ?? 0);
        if (string.IsNullOrEmpty(base64Text))
        {
            return new EncodingResult("", "plain", "Empty input provided");
        }

        try
        {
            var bytes = Convert.FromBase64String(base64Text);
            var decoded = Encoding.UTF8.GetString(bytes);
            return new EncodingResult(decoded, "plain", $"Decoded {base64Text.Length} Base64 characters");
        }
        catch (Exception ex)
        {
            return new EncodingResult("", "plain", $"Decoding failed: {ex.Message}");
        }
    }
}

public record DateTimeInfo(
    string LocalTime,
    string UtcTime,
    string TimeZone,
    long UnixTimestamp
);

public record SystemInfo(
    string OperatingSystem,
    string MachineName,
    string DotNetVersion,
    int ProcessorCount,
    string WorkingDirectory
);

public record RandomNumber(int Value, int MinRange, int MaxRange);

public record EncodingResult(string Result, string Format, string Message);
