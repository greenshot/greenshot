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

* Greenshot doesn't trust what a connection says about itself. For the source `mcp` it checks the process on the
  other end of the pipe: it must be `greenshot-mcp.exe` from Greenshot's own directory (or a path in
  `AiToolsMcpServerPaths`, for development builds). The AI tool is the program which started `greenshot-mcp.exe`
  (cmd.exe / PowerShell in between are skipped), identified by its executable path and verified Authenticode signer.
* Each AI tool must be allowed by the user: the first request shows a question with the program's name, path and
  signer. Allowed programs are stored in `AiToolsAllowedClients` and can be removed in the settings (General, AI tools).
  A "No" is remembered until Greenshot restarts.
* `LIST_WINDOWS` and `CAPTURE` are only available to the `mcp` source, not to the command line, web pages
  (greenshot:// links) or the browser extension.
* Windows of the processes in `AiToolsExcludedProcesses` (password managers by default) are never listed or captured,
  and are blacked out in screen and region captures.
* Every capture shows a notification (`AiToolsNotifyOnCapture`, on by default).
* The connection uses its own source (`mcp`) with its own command whitelist: no EXIT, OPEN_FILE, settings or other
  commands, and no UNC paths in recipe arguments.
* Limits: a program running under the same Windows account could still capture the screen without Greenshot, or
  inject into an allowed program. The checks keep web pages, extensions and other tools from using Greenshot as a
  screenshot API, and make sure the user's consent goes to a named program. An AI tool started through `node.exe`
  (e.g. an npm-installed client) is identified as Node.js.

## Build and use

```
dotnet publish src/Greenshot.Mcp -c Release -r win-x64
```

Native AOT needs the "Desktop development with C++" workload of Visual Studio. If the publish fails with
"'vswhere.exe' is not recognized", add `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` to the PATH.

The native `greenshot-mcp.exe` is in `src/Greenshot.Mcp/bin/Release/net10.0-windows/win-x64/publish`. Placed next to
`Greenshot.exe`, it starts Greenshot when it isn't running. To use a development build from another directory, add
that directory to `AiToolsMcpServerPaths` in greenshot.ini.

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
