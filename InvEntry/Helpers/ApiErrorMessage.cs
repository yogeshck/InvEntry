using System;
using System.Text.Json;

namespace InvEntry.Helpers;

/// <summary>
/// Turns a failed API call into the text to show the user: the API's
/// { "message": "..." } body when present, otherwise the exception message.
/// </summary>
public static class ApiErrorMessage
{
    public static string From(Exception exception)
    {
        var text = exception.Message;
        var start = text.IndexOf('{');

        if (start >= 0)
        {
            try
            {
                using var json = JsonDocument.Parse(text[start..]);

                if (json.RootElement.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString() ?? text;
                }
            }
            catch (JsonException)
            {
                // Not a JSON body; fall back to the raw message.
            }
        }

        return text;
    }
}
