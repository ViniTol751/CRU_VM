using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RDO.app.Services;

/// <summary>NDJSON debug sink for agent session (do not log secrets).</summary>
internal static class DebugAgentLog
{
    private const string LogPath = @"c:\dev\TesteAPI\debug-b44346.log";

    internal static void Write(string hypothesisId, string location, string message, object? data = null, string? runId = null)
    {
        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["sessionId"] = "b44346",
                ["hypothesisId"] = hypothesisId,
                ["location"] = location,
                ["message"] = message,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["data"] = data,
                ["runId"] = runId
            };
            File.AppendAllText(LogPath, JsonSerializer.Serialize(payload) + Environment.NewLine);
        }
        catch { /* never break app */ }
    }
}
