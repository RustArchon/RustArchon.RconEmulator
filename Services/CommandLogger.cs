using System.Text;

namespace RustArchon.RconEmulator.Services;

/// <summary>
/// The actual point of this tool: records every command a connected client sends, so pointing a
/// third-party RCON tool (RustAdmin, another web panel, etc.) at this emulator and clicking through
/// its UI reveals exactly what raw WebRCON command each action issues - console output for watching
/// live, plus an append-only file so a session can be reviewed afterward.
/// </summary>
public class CommandLogger
{
    private readonly ILogger<CommandLogger> _logger;
    private readonly string _logFilePath;
    private readonly object _fileLock = new();

    public CommandLogger(IWebHostEnvironment environment, ILogger<CommandLogger> logger)
    {
        _logger = logger;
        _logFilePath = Path.Combine(environment.ContentRootPath, "commands.log");
    }

    public void LogConnectionOpened(string connectionId, string remoteEndpoint, string password)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] CONNECT  conn={connectionId} from={remoteEndpoint} password=\"{password}\"";
        _logger.LogInformation(
            "Client connected: {ConnectionId} from {RemoteEndpoint} (password used: \"{Password}\")",
            connectionId, remoteEndpoint, password);
        AppendToFile(line);
    }

    public void LogConnectionClosed(string connectionId, string? reason)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] CLOSE    conn={connectionId} reason=\"{reason}\"";
        _logger.LogInformation("Client disconnected: {ConnectionId} ({Reason})", connectionId, reason ?? "unknown");
        AppendToFile(line);
    }

    public void LogCommand(string connectionId, int identifier, string command)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] COMMAND  conn={connectionId} id={identifier} message=\"{command}\"";

        // The one log line this tool exists for - kept at a level that always shows regardless of the
        // configured minimum, so it's never accidentally filtered out.
        _logger.LogWarning("[COMMAND] {ConnectionId} #{Identifier}: {Command}", connectionId, identifier, command);
        AppendToFile(line);
    }

    public void LogMalformedFrame(string connectionId, string rawText, Exception ex)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] MALFORMED conn={connectionId} raw=\"{rawText}\" error=\"{ex.Message}\"";
        _logger.LogWarning(ex, "Malformed frame from {ConnectionId}: {RawText}", connectionId, rawText);
        AppendToFile(line);
    }

    private void AppendToFile(string line)
    {
        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append to command log file {Path}", _logFilePath);
        }
    }
}
