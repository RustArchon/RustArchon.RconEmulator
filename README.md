# RustArchon.RconEmulator

A minimal WebRCON server that speaks the same wire protocol as a real Rust dedicated server, for one
specific purpose: **watching what commands other RCON tools actually send**, rather than guessing at
command syntax from documentation.

This is not a test double for RustArchon's own client (`RustArchon.Rcon`/`RustArchon.Worker`) - it's a
"honeypot" you point a *third-party* RCON tool (RustAdmin, another web panel, a Discord bot, etc.) at,
then click through that tool's UI and watch exactly what raw command it issues for each action.

## Protocol

Matches Rust's real WebRCON protocol as implemented by `RustArchon.Rcon.RustWebRconClient`:

- **Connect**: `ws://<host>:<port>/<password>` - the password is the URL path segment, not a separate
  auth message. This emulator accepts *any* password unconditionally (and logs whatever was sent) -
  there's nothing to configure to match before pointing a tool at it.
- **Request** (client → server): `{"Identifier": <int>, "Message": "<command>", "Name": "<string>"}`
- **Response** (server → client): `{"Identifier": <int>, "Message": "<string>", "Type": "<string>", "Stacktrace": "<string>"}`

## Running it

```
dotnet run
```

Listens on `ws://localhost:28016` by default (see `Properties/launchSettings.json`). Point whatever
tool you're observing at that address with any password.

## What it does

- **Logs every command** it receives - to the console (as a warning-level line so it's never filtered
  out) and to an append-only `commands.log` file in the working directory, with a timestamp, a short
  per-connection id, the WebRCON `Identifier`, and the raw command text. This is the actual point of
  the tool.
- **Answers `serverinfo` and `playerlist`** with scripted responses read live from `Data/serverinfo.json`
  and `Data/playerlist.json` - edit either file and the very next request picks up the change, no
  restart needed. Many RCON tools poll one or both of these right after connecting and won't show much
  useful UI without a plausible reply, so these two are scripted while everything else isn't.
- **Acknowledges every other command** with an empty, generic response and moves on - it doesn't
  simulate what running the command would actually do (it has no game world to affect), only that a
  command was sent and received.

## What it doesn't do

It won't tell you whether a command actually *works* against a real Rust server, or what a command's
real side effects are - only that a given UI action sends a given raw string. Confirming real behavior
still needs a real (even if local/headless) Rust dedicated server.
