using System.Text.Json;

namespace RustArchon.RconEmulator.Services;

/// <summary>
/// Serves scripted replies for known commands from <c>Data/responses.json</c> - a flat
/// <c>{"command": "raw response text"}</c> map, re-read from disk on every lookup rather than cached
/// in memory. Deliberately not hot-reload-via-FileSystemWatcher: a plain re-read on each request is
/// simpler, and the request rate here (a connected client polling every so often) is far too low for
/// the extra disk read to matter - the point is that editing the file takes effect on the very next
/// request, with no restart needed, so whoever's watching a third-party client's behavior can adjust
/// what it sees between clicks in that client's UI.
/// </summary>
/// <remarks>
/// Not limited to <c>serverinfo</c>/<c>playerlist</c> - any command a real server has been observed
/// responding to can get an entry here (see the README's "when we know what a real server sends back"
/// note). Lookup is by the command's exact text, case-insensitively, trimmed - not a prefix or regex
/// match, since most scripted commands here are simple no-argument queries.
/// </remarks>
public class ScriptedResponseProvider(IWebHostEnvironment environment, ILogger<ScriptedResponseProvider> logger)
{
    private string ResponsesPath => Path.Combine(environment.ContentRootPath, "Data", "responses.json");

    public string? TryGetScriptedResponse(string command)
    {
        var responses = LoadResponses();
        return responses.TryGetValue(command.Trim(), out var response) ? response : null;
    }

    private Dictionary<string, string> LoadResponses()
    {
        try
        {
            var json = File.ReadAllText(ResponsesPath);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return parsed is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read/parse scripted responses file {Path} - no scripted responses available this request", ResponsesPath);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
