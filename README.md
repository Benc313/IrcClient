# IRC Client

A simple interactive IRC client written in C# using [Spectre.Console](https://spectreconsole.net/) for a modern terminal UI.
**Note:** This client only works with the custom IRC server provided in my profile.

## Features

- Connect to any IRC server and channel
- Colorful, user-friendly terminal interface
- Supports basic IRC commands: `/nick`, `/join`, `/msg`, `/list`, `/quit`
- Displays server messages, user joins/parts, and private messages

## Requirements

- .NET 8.0 SDK or newer
- Internet connection to connect to IRC servers

## Build

```sh
dotnet build
```

## Run
```sh
dotnet run --project IrcClient
```

## Usage
1. Start the application.
2. Enter the server address.
3.  Use the following commands in the chat:
   - /nick <new_nick> — Change your nickname
   - /join <#channel> — Join a different channel
   - /msg <user> <message> — Send a private message
   - /list — List available channels
   - /quit — Exit the client

Type any message and press Enter to send it to the current channel\.
