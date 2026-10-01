# Greenshot MCP server (greenshot-mcp.exe)

`greenshot-mcp.exe` lets AI tools (Claude Code, Claude Desktop, VS Code, Cursor and other MCP clients) see the
user's windows through Greenshot. It is a small [Model Context Protocol](https://modelcontextprotocol.io) server over
stdio, built with .NET 10 and Native AOT, and talks to the running Greenshot over the same named pipe as
`greenshot.com` and the browser extension (see [greenshot-proxy-and-integration.md](greenshot-proxy-and-integration.md)).

## Tools

| Tool | What it does |
|---|---|
| `list_windows` | Top-level windows in Z-order (id, title, process, class, bounds, minimized, active) and the displays |
| `capture_window` | Screenshot of one window by its id from `list_windows`, with its exact contents (also when it is covered), without activating it |
| `capture_region` | Screenshot of a part of the screen (`x,y,width,height` in screen coordinates), e.g. to see details at full resolution |
| `capture_screen` | Screenshot of all displays |
| your recipes | Every recipe with an enabled **AI tool** trigger |

`list_windows` is part of greenshot-mcp. All other tools are Greenshot recipes with an AI tool trigger, and the three
capture tools are built-in recipes. Each capture tool takes `ocr=true` to also return the recognized text and the
position of each line. Images are scaled to 1568 pixels on the longest side (the trigger's `MaxImageSize`, 0 keeps
the original size).

### Recipes as tools

An AI tool trigger (`"triggerType": "AiTool"`) offers a recipe to AI tools:

| Parameter | Meaning |
|---|---|
| `ToolName` | The tool name for the AI (1 to 64 letters, digits, `_` or `-`) |
| `Title` | The name people see |
| `Description` | What the tool does, for the AI |
| `ReadOnly` / `Destructive` | Hints for the AI tool (default: read only) |
| `Arguments` | Like the arguments of a Commandline trigger (name, variable, type, required, default, allowed values) |
| `MaxImageSize` | Longest side of the returned image, default 1568 |

Two argument types are only meant for AI tools:

* `Window`: the AI passes a window id from `list_windows` (e.g. `w7`), the recipe gets the window. Use it with the
  Source step's `WindowHandle` parameter (`SourceType` Window), e.g. `"WindowHandle": "${Window}"`.
* `Region`: `x,y,width,height` in screen coordinates; store it in `PreSuppliedRegion` and the Source step captures it.

The tool returns the recipe's final image, the text found by OCR, its stdout / stderr and its variables. A recipe for
an AI tool needs no destination, its result goes back to the AI tool (it can still save or upload, as configured).
greenshot-mcp checks the recipes every 30 seconds and after each tool call; when they changed it tells the AI tool
(`notifications/tools/list_changed`), so a new recipe shows up without restarting the AI tool.

Example: the built-in `capture_window` recipe as JSON.

```json
{
  "id": "recipe_ai_capture_window",
  "name": "AI tool: capture window",
  "triggers": [{
    "triggerType": "AiTool",
    "parameters": {
      "ToolName": "capture_window",
      "Title": "Capture window",
      "Description": "Screenshot of one window. Use list_windows first and pass the id of the window (e.g. w7).",
      "ReadOnly": true,
      "Arguments": [
        { "Name": "window", "Variable": "Window", "Type": "Window", "Required": true, "Description": "The id of the window from list_windows" },
        { "Name": "ocr", "Variable": "Ocr", "Type": "Boolean", "DefaultValue": "false", "Description": "true to also return the text" }
      ]
    }
  }],
  "nodes": [
    { "id": "acquire", "stepType": "Source", "parameters": { "SourceType": "Window", "WindowHandle": "${Window}", "CaptureMouseCursor": false, "DelayMs": 0 } },
    { "id": "ocr_wanted", "stepType": "Conditional", "parameters": { "Branches": [{ "Key": "ocr", "Expression": "${Ocr}" }] } },
    { "id": "ocr", "stepType": "Processors", "parameters": { "ProcessorIds": ["Windows10OcrProcessor"] } }
  ],
  "flow": {
    "startNodes": ["acquire"],
    "transitions": { "acquire": ["ocr_wanted"] },
    "conditionalTransitions": [{ "from": "ocr_wanted", "branch": "ocr", "to": "ocr" }]
  }
}
```

## Safety

* Greenshot doesn't trust what a connection says about itself. For the source `mcp` it checks the process on the
  other end of the pipe: it must be `greenshot-mcp.exe` from Greenshot's own directory (or, in Debug builds only, a path in
  `AiToolsMcpServerPaths`). The AI tool is the program which started `greenshot-mcp.exe`
  (cmd.exe / PowerShell in between are skipped), identified by its executable path and verified Authenticode signer.
* Each AI tool must be allowed by the user: the first request shows a question with the program's name, path and
  signer. Allowed programs are stored in `AiToolsAllowedClients` and can be removed in the settings (General, AI tools).
  A "No" is remembered until Greenshot restarts.
* The other sources are checked the same way: only Greenshot.exe and greenshot.com (source `cli`) and
  greenshot-proxy.exe (`url_scheme`, `open_with`, `native_messaging`) from Greenshot's directory may connect, so other
  programs can't talk to the pipe directly. The pipe also refuses network logons.
* AI tools can only list windows and run recipes with an AI tool trigger (`LIST_WINDOWS`, `LIST_AI_TOOLS`,
  `RUN_AI_TOOL`), and only through greenshot-mcp.exe: not from the command line, web pages (greenshot:// links) or the
  browser extension. An AI tool trigger can't be started any other way, and AI tools can't run command line recipes.
* Window ids instead of handles: an AI tool can only capture windows Greenshot listed to it. The ids belong to one AI
  tool and greenshot-mcp session, expire 10 minutes after the last `list_windows` that showed the window, and stop
  working when the window is closed.
* Windows of the processes in `AiToolsExcludedProcesses` (password managers by default) never get an id, can't be
  captured, and are blacked out in everything else a recipe started by an AI tool takes from the screen. These rules
  are in Greenshot itself, a recipe can't turn them off.
* Every capture shows a notification (`AiToolsNotifyOnCapture`, on by default).
* The connection uses its own source (`mcp`) with its own command whitelist: no EXIT, OPEN_FILE, settings or other
  commands, and no UNC paths in recipe arguments. The tool names and descriptions (`LIST_AI_TOOLS`) are available
  before the user allowed the AI tool, so it can show its tools; running any of them asks first.
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
`Greenshot.exe`, it starts Greenshot when it isn't running. To use a development build from another directory with a
Debug build of Greenshot, add that directory to `AiToolsMcpServerPaths` in greenshot.ini (ignored by Release builds).

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
