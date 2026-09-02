namespace RustArchon.RconEmulator.Services;

/// <summary>
/// Serves the raw JSON text for scripted <c>serverinfo</c>/<c>playerlist</c> replies, re-reading each
/// file from disk on every call rather than caching it in memory. Deliberately not hot-reload-via-
/// FileSystemWatcher: a plain re-read on each request is simpler, and the request rate here (a
/// connected client polling every so often) is far too low for the extra disk read to matter - the
/// point is that editing Data/serverinfo.json or Data/playerlist.json takes effect on the very next
/// request, with no restart needed, so whoever's watching a third-party client's behavior can adjust
/// what it sees (e.g. bump player count, change the map name) between clicks in that client's UI.
/// </summary>
public class ScriptedResponseProvider(IWebHostEnvironment environment, ILogger<ScriptedResponseProvider> logger)
{
    private string ServerInfoPath => Path.Combine(environment.ContentRootPath, "Data", "serverinfo.json");
    private string PlayerListPath => Path.Combine(environment.ContentRootPath, "Data", "playerlist.json");

    public string GetServerInfoJson() => ReadOrFallback(ServerInfoPath, "{}");

    public string GetPlayerListJson() => ReadOrFallback(PlayerListPath, "[]");

    private string ReadOrFallback(string path, string fallback)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read scripted response file {Path} - falling back to {Fallback}", path, fallback);
            return fallback;
        }
    }
}
