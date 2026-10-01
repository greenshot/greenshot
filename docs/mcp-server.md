# Greenshot MCP server (greenshot-mcp.exe)

`greenshot-mcp.exe` lets AI tools (Claude Code, Claude Desktop, VS Code, Cursor and other MCP clients) see the
user's windows through Greenshot. It is a small [Model Context Protocol](https://modelcontextprotocol.io) server over
stdio, built with .NET 10 and Native AOT, and talks to the running Greenshot over the same named pipe as
`greenshot.com` and the browser extension (see [greenshot-proxy-and-integration.md](greenshot-proxy-and-integration.md)).

## Tools

| Tool | What it does |
|---|---|
| `list_windows` | Top-level windows in Z-order (handle, title, process, class, bounds, minimized, active) and the displays |
| `capture` | Screenshot of a window (`handle`, `title` or `process`), the active window, the screen / one display, or a region; returns the PNG plus title, process and bounds, optionally OCR text |
| `list_recipes` | The recipes with a command line trigger, with their arguments |
| `describe_recipe` | Triggers, inputs, outputs and steps of one recipe |
| `run_recipe` | Runs a recipe with arguments and returns its JSON result |

A window capture uses Windows Graphics Capture when the system supports it, so it returns the exact contents of the
window even when other windows cover it. Images are scaled to 1568 pixels on the longest side by default
(`max_size`, 0 keeps the original size); capture a `region` to see details at full resolution.

## Safety

* AI tools can't do anything until the user allows them: the first request shows a question in Greenshot
  (`AllowAiTools` in the `[Core]` section of greenshot.ini, off by default). A "No" is remembered until Greenshot restarts.
* Windows of the processes in `AiToolsExcludedProcesses` (password managers by default) are never listed or captured,
  and are blacked out in screen and region captures.
* Every capture shows a notification (`AiToolsNotifyOnCapture`, on by default).
* The connection uses its own source (`mcp`) with its own command whitelist: no EXIT, OPEN_FILE, settings or other
  commands, and no UNC paths in recipe arguments.

## Build and use

```
dotnet publish src/Greenshot.Mcp -c Release -r win-x64
```

The native `greenshot-mcp.exe` is in `src/Greenshot.Mcp/bin/Release/net10.0-windows/win-x64/publish`. Placed next to
`Greenshot.exe`, it starts Greenshot when it isn't running.

Claude Code:

```
claude mcp add greenshot -- "C:\Program Files\Greenshot\greenshot-mcp.exe"
```

Other clients (e.g. Claude Desktop `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "greenshot": { "command": "C:\\Program Files\\Greenshot\\greenshot-mcp.exe" }
  }
}
```
