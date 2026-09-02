namespace RustArchon.RconEmulator.Models;

/// <summary>One outbound WebRCON response - see <see cref="RconRequest"/>'s remarks.</summary>
public class RconResponse
{
    public int Identifier { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// A real Rust server sends "Generic" for most command replies and "Chat"/"Error" for a few
    /// specific cases - the exact set isn't documented anywhere reliable. "Generic" is a safe default
    /// for anything this emulator doesn't specifically script a response for.
    /// </summary>
    public string Type { get; set; } = "Generic";

    public string Stacktrace { get; set; } = string.Empty;
}
