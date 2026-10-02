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
| `get_recipe_schema` | The recipe JSON schema and example recipes (also the resource `greenshot-mcp://recipes/schema`) |
| `get_recipe_catalog` | What this Greenshot offers recipes: trigger types, step types with parameters, destinations (uploads marked), processors and the user's recipes; with `recipe_id` the JSON of one recipe |
| `validate_recipe` | Checks a recipe: errors, warnings, what it does in plain words and what the user will be asked to allow |
| `propose_recipe` | Shows a new recipe to the user, who decides whether it is saved |
| `update_recipe` | Shows a changed version of an existing recipe (also a built-in one) with the changed lines |
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
greenshot-mcp keeps a watch connection to Greenshot (`WATCH`): Greenshot tells it right away when the recipes or the
AI tools switch change, and greenshot-mcp tells the AI tool (`notifications/tools/list_changed`), so a new recipe shows
up without restarting the AI tool. It also checks every 30 seconds and after each tool call (older Greenshot versions
have no watch connection).

When Greenshot exits it sends every open connection `{"event":"shutdown","reason":...,"greenshot_running":false}`
(the browser extension shows Greenshot as offline right away):

* `exit`, the user closed Greenshot: greenshot-mcp keeps running, so the AI tool doesn't mark the server as failed, but
  doesn't start Greenshot again. Tool calls answer that Greenshot was closed, until Greenshot runs again; then it
  reconnects by itself.
* `update`, the Windows Restart Manager closes Greenshot for an installer (update or uninstall): greenshot-mcp exits,
  otherwise it would keep the installation directory locked. As a fallback the installer and the uninstaller close
  greenshot-mcp.exe themselves, and the uninstaller removes it.
* `session_end`, Windows logs off or shuts down.

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

### Recipes written by AI tools

The recipe editor is for advanced users; most people can describe what they want, and the AI tool writes the recipe.
The AI tool reads the schema and the catalog, checks its recipe with `validate_recipe` and then calls
`propose_recipe` (or `update_recipe`) with the recipe, the user's request and its explanation. Greenshot shows the
recipe in its recipe approval window and the tool call waits for the user's decision:

* A banner says the recipe was proposed by an AI tool, with the program's name, path and signer (checked as for every
  AI tool), and shows the request and explanation as the AI tool's words.
* "What it does" is written by Greenshot from the recipe itself, not by the AI tool: each step in plain words, with
  uploads (🌐), external programs (⚙) and file access (📁) marked.
* Each trigger has its own switch with what it means (e.g. "runs on its own every time you copy an image", "any web
  page you open can start this recipe"). For a recipe from an AI tool all switches start off.
* Uploads, external commands and file access each need their own permission; the recipe can't be saved without them.
* A change shows what is added and removed, and "View changed lines" shows a line diff. A change of a built-in recipe
  says so; the built-in recipe comes back with "Reset Default" in the recipe manager.
* "Open in the recipe editor after saving" opens the saved recipe for fine-tuning.

Greenshot saves only what it showed: it re-writes the recipe JSON itself, and that exact content is hashed, shown and
saved. New recipes go to `Recipes\AI\<id>.gsrecipe.json` next to greenshot.ini; a change of a recipe keeps its file
(when that file holds only this recipe). The approval (hash, switched-on triggers, allowed permissions, origin) is in
the encrypted trust store, not in the recipe file. When the file changes later, the user is asked again, and for a
file an AI tool wrote all switches start off again. A rejected proposal isn't shown again until Greenshot restarts,
and only one proposal is shown at a time.

AI tools can also propose an **automatic step** (`"kind": "extension"`, see [capture-recipes.md](capture-recipes.md)): steps
Greenshot adds to other recipes, like a border on every capture. `get_recipe_catalog` lists the slots (and which
recipes have them), the option types and the automatic steps; `validate_recipe` also says which recipes it would change.
The approval window shows "Changes other recipes" (which recipes now, also later ones, where and when). An approved
automatic step from an AI tool is saved switched off; the user switches it on in Settings > Recipes.

The recipe manager marks recipes written by AI tools (🤖, and the filter "AI"), shows triggers that are switched off
by the approval as "off, not approved", and its "Permissions" button opens the approval again to change them or to
revoke it. Without the recipe editor plug-in (it is optional, and not in the Light version), Settings > AI tools >
"Approved recipes" lists the recipes from files with who wrote them and their approval, with Details, Review and Revoke.
The AI tools tab is shown when "beta tester" is ticked on the Expert tab. It also shows whether greenshot-mcp.exe is
next to Greenshot.exe (✔, or ✘ with the download link; Debug builds also check the `AiToolsMcpServerPaths` locations)
and warns when its version isn't Greenshot's. The excluded applications are edited there as a list of names; case and
".exe" don't matter.

## Safety

* AI tools are opt-in: until "Let AI tools use Greenshot" is switched on (Settings > AI tools, `AiToolsEnabled`, off
  by default), Greenshot refuses every greenshot-mcp request without asking; only the version check answers, so the AI
  tool can tell the user how to switch it on. Administrators can preset it in `greenshot-defaults.ini` or lock it, on or
  off, in `greenshot-fixed.ini`. `AiToolsAllowRecipeProposals` (on by default) switches off recipe proposals alone.
* Greenshot doesn't trust what a connection says about itself. For the source `mcp` it checks the process on the
  other end of the pipe: it must be `greenshot-mcp.exe` from Greenshot's own directory (or, in Debug builds only, a path in
  `AiToolsMcpServerPaths`). The AI tool is the program which started `greenshot-mcp.exe`
  (cmd.exe / PowerShell in between are skipped), identified by its executable path and verified Authenticode signer.
* Each AI tool must be allowed by the user: the first request shows a question with the program's name, path and
  signer. Allowed programs are stored in `AiToolsAllowedClients` and can be removed in the settings (AI tools tab).
  A "No" (or closing the question) is remembered until Greenshot restarts; the AI tools tab lists those programs with
  "Allow" and "Ask Again". The question stays on top but doesn't take the keyboard, has no default button, ignores
  Escape, and its buttons only react after a second, so a keystroke meant for the AI tool can't answer it. The log says
  how it was answered. A helper without a name of its own (Antigravity's `resources\bin\language_server.exe`) is shown
  with its application's name, "Antigravity (language_server)"; the helper's path is what is allowed.
* The other sources are checked the same way: only Greenshot.exe and greenshot.com (source `cli`) and
  greenshot-proxy.exe (`url_scheme`, `open_with`, `native_messaging`) from Greenshot's directory may connect, so other
  programs can't talk to the pipe directly. The pipe also refuses network logons.
* AI tools can only list windows, run recipes with an AI tool trigger (`LIST_WINDOWS`, `LIST_AI_TOOLS`,
  `RUN_AI_TOOL`) and propose recipes (`RECIPE_CATALOG`, `VALIDATE_RECIPE`, `PROPOSE_RECIPE`), and only through greenshot-mcp.exe: not from the command line, web pages (greenshot:// links) or the
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

## Install

`greenshot-mcp.exe` is not part of the installer or the portable version, it is a separate download on the
[releases page](https://github.com/greenshot/greenshot/releases): `Greenshot-MCP-<version>-win-x64.zip`. The zip holds
nothing but the single, self-contained `greenshot-mcp.exe`: the zip carries the version in its name, while the exe keeps
the exact name Greenshot trusts (`greenshot-mcp.exe`, see below), so it doesn't have to be renamed.

1. Use the zip with the same version as Greenshot.
2. Extract `greenshot-mcp.exe` into the directory of `Greenshot.exe`: `C:\Program Files\Greenshot` for an installed
   Greenshot (this needs administrator rights), or the directory of the portable version.
   Greenshot Light (its installer and its portable version) has no AI tools: it doesn't answer greenshot-mcp.
3. Connect your AI tool (below) with the full path of `greenshot-mcp.exe`.

It has to be in the same directory as `Greenshot.exe`:

* Greenshot only answers `greenshot-mcp.exe` from its own directory (it checks the process on the other end of the
  pipe), so a copy elsewhere is refused.
* `greenshot-mcp.exe` starts `Greenshot.exe` from its own directory when Greenshot isn't running.

Nothing else is needed: it is a native executable, it doesn't need the .NET runtime.

## Use it

Ask the AI tool in your own words, for example:

* "Look at my Visual Studio window and tell me what the error says." (`list_windows`, then `capture_window`)
* "Take a screenshot of the region with the chart and read the numbers." (`capture_region` with OCR)
* "Make a recipe: Ctrl+Shift+U captures a region, adds a red border and uploads it to Imgur." The AI tool writes the
  recipe, checks it and proposes it; Greenshot shows it and you decide (see [Recipes written by AI tools](#recipes-written-by-ai-tools)).
* "Add a tool that captures the active window and returns the text." The AI tool proposes a recipe with an AI tool
  trigger; after you approve it, the AI tool has the new tool.

AI tools are off by default: switch on "Let AI tools use Greenshot" in Settings > AI tools first. The first time,
Greenshot asks whether the AI tool may use it. Every capture shows a notification.

## Connect an AI tool

Every MCP client starts `greenshot-mcp.exe` itself (stdio), so it only needs the path of the executable. The examples
use the default install location `C:\Program Files\Greenshot\greenshot-mcp.exe`; in JSON files the backslashes are
doubled. Restart the AI tool or reload its MCP servers after changing a configuration file.

The first time the AI tool uses a Greenshot tool, Greenshot asks whether that program may use it. The answer is
stored in the Greenshot settings (AI tools tab), where it can be removed again. Before that, switch on "Let AI
tools use Greenshot" on the same tab: it is off by default.

### Claude Code

```
claude mcp add --scope user greenshot -- "C:\Program Files\Greenshot\greenshot-mcp.exe"
```

`--scope user` makes Greenshot available in all projects; without it the server is only added to the current project.
To share it with a team, put it in the project's `.mcp.json`:

```json
{
  "mcpServers": {
    "greenshot": { "command": "C:\\Program Files\\Greenshot\\greenshot-mcp.exe" }
  }
}
```

Check it with `claude mcp list` or `/mcp` in a session.

### Claude Desktop

Settings, Developer, Edit Config opens `claude_desktop_config.json` (usually `%APPDATA%\Claude\claude_desktop_config.json`).
Add the server and restart Claude Desktop:

```json
{
  "mcpServers": {
    "greenshot": { "command": "C:\\Program Files\\Greenshot\\greenshot-mcp.exe" }
  }
}
```

### Google Antigravity

Antigravity 2.0, the Antigravity IDE and the `agy` CLI share `%USERPROFILE%\.gemini\config\mcp_config.json`
(a workspace can have its own `.agents\mcp_config.json`). In the IDE: the `...` menu at the top of the agent panel,
MCP Servers, Manage MCP Servers, View raw config.

```json
{
  "mcpServers": {
    "greenshot": {
      "command": "C:\\Program Files\\Greenshot\\greenshot-mcp.exe",
      "args": []
    }
  }
}
```

Then Settings, Customizations, Installed MCP Servers, Refresh (in the CLI: `/mcp`). When removing the server again,
also delete its cached copy in `%USERPROFILE%\.gemini\antigravity*\mcp\`.

### Gemini CLI

```
gemini mcp add --scope user greenshot "C:\Program Files\Greenshot\greenshot-mcp.exe"
```

or add the same `mcpServers` entry as above to `%USERPROFILE%\.gemini\settings.json`. Check it with `/mcp`.

### VS Code (GitHub Copilot agent mode)

Run "MCP: Add Server" from the command palette (Command (stdio), the path of `greenshot-mcp.exe`, name `greenshot`),
or create `.vscode\mcp.json` in the workspace. VS Code uses `servers` instead of `mcpServers`:

```json
{
  "servers": {
    "greenshot": {
      "type": "stdio",
      "command": "C:\\Program Files\\Greenshot\\greenshot-mcp.exe"
    }
  }
}
```

### Cursor and Windsurf

The same `mcpServers` entry as for Claude Desktop, in `%USERPROFILE%\.cursor\mcp.json` (all projects) or `.cursor\mcp.json`
(one project) for Cursor, and in `%USERPROFILE%\.codeium\windsurf\mcp_config.json` for Windsurf.

### OpenAI Codex

```
codex mcp add greenshot -- "C:\Program Files\Greenshot\greenshot-mcp.exe"
```

or in `%USERPROFILE%\.codex\config.toml`:

```toml
[mcp_servers.greenshot]
command = 'C:\Program Files\Greenshot\greenshot-mcp.exe'
```

### Other MCP clients

Any client that supports local (stdio) MCP servers works: the command is the path of `greenshot-mcp.exe`, without
arguments or environment variables. Greenshot identifies the AI tool by the program which started `greenshot-mcp.exe`.
A client that runs on Node.js (e.g. installed with npm) shows up as "Node.js" in Greenshot's question and settings.

## For developers

### Debug builds

A Debug build of Greenshot doesn't publish greenshot-mcp. Build `src/Greenshot.Mcp` (it is in the solution) and
point the AI tool at the build output, e.g. `src\Greenshot.Mcp\bin\Debug\net10.0-windows\greenshot-mcp.exe` (this
needs the .NET 10 runtime). Because that isn't Greenshot's directory, allow it in greenshot.ini of the Debug build:

```ini
[Core]
AiToolsMcpServerPaths=D:\code\greenshot\src\Greenshot.Mcp\bin\Debug\net10.0-windows
```

`AiToolsMcpServerPaths` takes directories or full paths of `greenshot-mcp.exe`, and is ignored by Release builds. A
development build of greenshot-mcp can't start Greenshot (there is no `Greenshot.exe` next to it), so start Greenshot
first, e.g. from Visual Studio.

To test the tools without an AI tool, use the [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```
npx @modelcontextprotocol/inspector "D:\code\greenshot\src\Greenshot.Mcp\bin\Debug\net10.0-windows\greenshot-mcp.exe"
```

The Inspector (node.exe) is then the AI tool Greenshot asks about.

### Release builds

A Release build of the solution publishes greenshot-mcp as its very last step (target `PublishMcpServer` in
`Greenshot-Installer.csproj`): a single native exe in `src\Greenshot\bin\Release\net480`, next to `Greenshot.exe`.
It is published after `checksum.SHA256`, the SBOM, the installers and the portable versions, so it is in none of them;
a greenshot-mcp.exe of an earlier build is removed before the checksums are made. The release workflow uploads it and
attaches `Greenshot-MCP-<version>-win-x64.zip`, which holds only that exe, to the GitHub release.

Native AOT needs the "Desktop development with C++" workload of Visual Studio. If the publish fails with
"'vswhere.exe' is not recognized", add `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` to the PATH. To build
a Release without it, pass `/p:SkipMcpPublish=true`. To publish only greenshot-mcp:

```
dotnet publish src/Greenshot.Mcp -c Release -r win-x64
```

The result is in `src/Greenshot.Mcp/bin/Release/net10.0-windows/win-x64/publish`.

### How it fits together

* `src/Greenshot.Mcp`: the MCP server (ModelContextProtocol SDK, stdio). `GreenshotTools` (`list_windows`) and
  `RecipeAuthoringTools` (schema, catalog, validate, propose, update) are fixed tools; `RecipeToolSync` turns the
  recipes with an AI tool trigger into tools (`RecipeTool`). The recipe schema and examples are embedded from `docs/`.
* Each tool call is one request over Greenshot's named pipe (`GreenshotConnection`), with the source `mcp`. On the
  Greenshot side `IpcSecurityDispatcher` checks the whitelist and the consent, `AiToolIpcHandler` handles windows and
  AI tool recipes, `AiRecipeIpcHandler` the recipe catalog and proposals.
* Tests: `Greenshot.Tests/Ipc/AiToolAccessTests.cs`, `AiRecipeIpcHandlerTests.cs` and `Recipes/RecipeApprovalTests.cs`.
