namespace BuildWithAspire.McpServer.Tools;

/// <summary>
/// Time tool for MCP that provides current date and time information.
/// </summary>
public class TimeTool
{
    public string Name => "get_time";
    public string Description => "Get current date and time information in various formats";

    public object GetParametersSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                timezone = new
                {
                    type = "string",
                    description = "Timezone name (e.g., 'UTC', 'America/New_York', 'Europe/London') - defaults to UTC",
                    @default = "UTC"
                },
                format = new
                {
                    type = "string",
                    description = "Date/time format - 'iso', 'local', 'unix', or 'detailed'",
                    @default = "iso"
                }
            },
            required = System.Array.Empty<string>() // No required parameters
        };
    }

    public async Task<object> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var timezone = parameters.GetValueOrDefault("timezone", "UTC")?.ToString() ?? "UTC";
        var format = parameters.GetValueOrDefault("format", "iso")?.ToString() ?? "iso";

        // Handle JsonElement properly for timezone
        if (parameters.GetValueOrDefault("timezone") is System.Text.Json.JsonElement timezoneElement)
        {
            if (timezoneElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                timezone = timezoneElement.GetString() ?? "UTC";
            }
        }

        // Handle JsonElement properly for format
        if (parameters.GetValueOrDefault("format") is System.Text.Json.JsonElement formatElement)
        {
            if (formatElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                format = formatElement.GetString() ?? "iso";
            }
        }

        var utcNow = DateTime.UtcNow;
        var localNow = DateTime.Now;

        // Try to get timezone specific time
        DateTime targetTime = utcNow;
        string timezoneDisplay = timezone;
        
        try
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            targetTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZoneInfo);
            timezoneDisplay = timeZoneInfo.DisplayName;
        }
        catch
        {
            // Fallback to UTC if timezone not found
            timezone = "UTC";
            timezoneDisplay = "Coordinated Universal Time";
        }

        await Task.Delay(50, cancellationToken).ConfigureAwait(false); // Simulate some processing time

        var result = new
        {
            timezone = timezone,
            timezone_display = timezoneDisplay,
            format_requested = format,
            current_time = format.ToLowerInvariant() switch
            {
                "iso" => (object)targetTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
                "local" => (object)targetTime.ToString("F", System.Globalization.CultureInfo.InvariantCulture),
                "unix" => (object)((DateTimeOffset)targetTime).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                "detailed" => (object)new
                {
                    iso = targetTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
                    local = targetTime.ToString("F", System.Globalization.CultureInfo.InvariantCulture),
                    unix_timestamp = ((DateTimeOffset)targetTime).ToUnixTimeSeconds(),
                    day_of_week = targetTime.DayOfWeek.ToString(),
                    day_of_year = targetTime.DayOfYear,
                    week_of_year = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                        targetTime, 
                        System.Globalization.CalendarWeekRule.FirstDay, 
                        DayOfWeek.Sunday)
                },
                _ => (object)targetTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture)
            },
            utc_time = utcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
            generated_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture)
        };

        return result;
    }
}