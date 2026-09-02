using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RustArchon.RconEmulator.Models;
using RustArchon.RconEmulator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ScriptedResponseProvider>();
builder.Services.AddSingleton<CommandLogger>();

var app = builder.Build();

app.UseWebSockets();

var jsonOptions = new JsonSerializerOptions { WriteIndented = false };

// A single catch-all handler, not routed endpoints - a real Rust server's WebRCON accepts a connection
// at literally any path (the path segment *is* the RCON password: ws://host:port/{password}, see
// RustArchon.Rcon.RustWebRconClient's connection URI), so there's no fixed route to map here. Whatever
// third-party client is being watched connects with whatever password it's configured with; this
// emulator accepts it unconditionally (logging what was sent, never validating it) so pointing any
// existing tool at it just works without needing to know or match a "real" password first.
app.Run(async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("This endpoint only accepts WebSocket (WebRCON) connections.");
        return;
    }

    var password = context.Request.Path.Value?.Trim('/') ?? string.Empty;
    var connectionId = Guid.NewGuid().ToString("N")[..8];
    var remoteEndpoint = $"{context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}";

    var responseProvider = context.RequestServices.GetRequiredService<ScriptedResponseProvider>();
    var commandLogger = context.RequestServices.GetRequiredService<CommandLogger>();

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    commandLogger.LogConnectionOpened(connectionId, remoteEndpoint, password);

    var buffer = new byte[8192];

    try
    {
        while (socket.State == WebSocketState.Open)
        {
            var messageBuilder = new StringBuilder();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(buffer, context.RequestAborted);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, context.RequestAborted);
                break;
            }

            var rawText = messageBuilder.ToString();
            RconRequest? request;

            try
            {
                request = JsonSerializer.Deserialize<RconRequest>(rawText, jsonOptions);
            }
            catch (JsonException ex)
            {
                commandLogger.LogMalformedFrame(connectionId, rawText, ex);
                continue;
            }

            if (request is null)
            {
                continue;
            }

            commandLogger.LogCommand(connectionId, request.Identifier, request.Message);

            var response = BuildResponse(request, responseProvider);
            var responseJson = JsonSerializer.Serialize(response, jsonOptions);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            await socket.SendAsync(responseBytes, WebSocketMessageType.Text, endOfMessage: true, context.RequestAborted);
        }
    }
    catch (OperationCanceledException)
    {
        // Request aborted (app shutting down, or the client vanished) - nothing more to do.
    }
    catch (WebSocketException ex)
    {
        commandLogger.LogConnectionClosed(connectionId, $"WebSocketException: {ex.Message}");
        return;
    }

    commandLogger.LogConnectionClosed(connectionId, "closed normally");
});

app.Run();

/// <summary>
/// Scripts a response for the handful of commands that need a plausible reply to keep a real client
/// happy (most tools poll <c>serverinfo</c>/<c>playerlist</c> right after connecting, and won't
/// meaningfully proceed without something sensible back) - anything else just gets logged and
/// acknowledged with an empty, generic reply, which is deliberate: the goal is observing what a client
/// sends for a given UI action, not simulating what running that command would actually do.
/// </summary>
static RconResponse BuildResponse(RconRequest request, ScriptedResponseProvider responseProvider)
{
    var command = request.Message.Trim();

    if (command.Equals("serverinfo", StringComparison.OrdinalIgnoreCase))
    {
        return new RconResponse { Identifier = request.Identifier, Message = responseProvider.GetServerInfoJson(), Type = "Generic" };
    }

    if (command.Equals("playerlist", StringComparison.OrdinalIgnoreCase))
    {
        return new RconResponse { Identifier = request.Identifier, Message = responseProvider.GetPlayerListJson(), Type = "Generic" };
    }

    return new RconResponse { Identifier = request.Identifier, Message = string.Empty, Type = "Generic" };
}
