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
- **Answers any command listed in `Data/responses.json`** with the exact text scripted there, read
  live on every request - edit the file and the very next request picks up the change, no restart
  needed. It's a flat `{"command": "raw response text"}` map, matched by exact command text
  (case-insensitive, trimmed). Ships with real captured responses (see below) for `serverinfo`,
  `playerlist`, and the `server.description`/`server.url`/`server.headerimage`/`server.hostname`
  convar reads a lot of third-party tools query right after connecting.
- **Acknowledges every other, unscripted command** with an empty, generic response and moves on - it
  doesn't simulate what running the command would actually do (it has no game world to affect), only
  that a command was sent and received.

## When you learn what a real server sends back

Add an entry to `Data/responses.json`. The values in this repo weren't invented - they were captured
by sending the same commands to a real, live Rust server through RustArchon.Panel's Console tab and
copying the exact response text (including, for the convar reads, the `"convarname: value"` echo
format Rust itself uses - not something this tool adds). Prefer real captured text over a guess
whenever you have it; a plausible-looking fake is worse than an honest empty ack, since a real client
may key off some detail (whitespace, quoting, an unexpected field) a hand-written fake won't get
right.

**Exception: identifying fields in `playerlist`.** The captured entry there was a real, live player
on a real server - its `SteamID`, `DisplayName`, and `Address` were replaced with fabricated values
(a syntactically-plausible but unassigned SteamID64, an obviously-fake name, and an RFC 5737
TEST-NET-1 address) before committing. This matters in practice, not just in principle: a
`playerlist` entry with a real SteamID is enough for a connected third-party tool's "Ban" (or similar)
action to actually reach Steam's real ban systems against a real account. Every other field (position,
ping, connection duration, etc.) is the real captured value, since none of it identifies anyone.
Keep this substitution in mind for any future capture that includes a real player's identity - only
the identity needs faking, not the whole entry.

## What it doesn't do

It won't tell you whether a command actually *works* against a real Rust server, or what a command's
real side effects are - only that a given UI action sends a given raw string. Confirming real behavior
still needs a real (even if local/headless) Rust dedicated server.
