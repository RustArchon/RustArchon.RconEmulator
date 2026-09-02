namespace RustArchon.RconEmulator.Models;

/// <summary>
/// One inbound WebRCON command, exactly as a real Rust server's client protocol sends it - matches
/// RustArchon.Rcon's <c>WebRconRequest</c> shape (see that project's Messages/WebRconRequest.cs and
/// WebRconMessageBase.cs) field-for-field, but defined independently here rather than referenced from
/// it: this tool exists specifically to watch *other* clients' behavior, so it deliberately doesn't
/// depend on RustArchon's own client library - it only needs to speak the wire protocol, not link
/// against any particular implementation of it.
/// </summary>
public class RconRequest
{
    public int Identifier { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
