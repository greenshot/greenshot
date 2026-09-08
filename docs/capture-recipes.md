# Greenshot Capture Recipes Guide

Greenshot features a modular, recipe-driven capture pipeline. Instead of hardcoded capture flows, every screenshot workflow is an ordered sequence of configurable steps defined as a **Capture Recipe**.

Recipes can be written in code or provided as external `.json` files. External JSON recipes can create new custom capture workflows or securely override Greenshot's built-in recipes.

---

## 1. Core Architecture: Decoupling Triggers from Recipes

In Greenshot:
- **A Trigger** defines *when and how* a capture is initiated (e.g. pressing a hotkey like `PrintScreen`, clicking a systray menu item, or receiving a clipboard image).
- **A Recipe** defines *what sequence of steps* is executed once triggered (e.g. acquiring pixels, showing an interactive selection rectangle, adding a border, playing feedback, running OCR, exporting to destinations).

Triggers and recipes are completely decoupled. Any trigger can run any recipe, and a single recipe can be executed by multiple triggers or invoked manually.

---

## 2. Configuration Precedence Explained

When a recipe step executes, it often needs settings such as whether to capture the mouse pointer, how long to delay, what color border to draw, or whether to play the shutter sound.

Greenshot resolves every parameter using a strict **three-tier precedence model**:

```
┌─────────────────────────────────────────────────────────────┐
│ Priority 1: Runtime Context Override                        │
│   (Forced for this single run by CLI, Trigger, or API)      │
├─────────────────────────────────────────────────────────────┤
│ Priority 2: Step Parameter Pre-definition                   │
│   (Explicitly hardcoded in the recipe JSON or C# code)      │
├─────────────────────────────────────────────────────────────┤
│ Priority 3: Dynamic User Configuration Evaluation           │
│   (Omitted/null in JSON → evaluated live from greenshot.ini)│
└─────────────────────────────────────────────────────────────┘
```

### Priority 1: Runtime Context Override
* **What it means**: An explicit parameter passed into `context.Properties` specifically for *one single capture execution*.
* **When it occurs**:
  - A user presses a special hotkey configured to suppress the mouse pointer.
  - A command-line switch like `--no-mouse` or `--region 100,100,500,400` is supplied.
  - A plugin or programmatic caller calls `CaptureHelper.CaptureRegion(captureMouse: false)`.
* **Why it exists**: When an external event or caller explicitly requests specific behavior for *this exact shot*, it overrides both the recipe definition and global preferences.

### Priority 2: Step Parameter Pre-definition
* **What it means**: Explicitly hardcoding a parameter value on a step within the recipe JSON or code.
* **When it occurs**:
  - In an OCR recipe, you *never* want the mouse pointer obscuring text, so the step explicitly defines:
    ```json
    {
      "stepType": "Source",
      "parameters": {
        "CaptureMouseCursor": false
      }
    }
    ```
  - In a border recipe, you explicitly specify a 5-pixel red border:
    ```json
    {
      "stepType": "Border",
      "parameters": {
        "Width": 5,
        "Color": "#FF0000"
      }
    }
    ```
* **Why it exists**: The recipe author designed this specific workflow to have fixed, predictable behavior regardless of the user's general preferences.

### Priority 3: Dynamic User Configuration Evaluation
* **What it means**: Leaving a parameter omitted (or `null`) in the recipe JSON. At runtime, the step dynamically reads the live setting from `greenshot.ini` (`CoreConfig`).
* **When it occurs**:
  - The `ImmediateFeedback` step is declared with no parameters:
    ```json
    {
      "stepType": "ImmediateFeedback"
    }
    ```
    At the moment the screenshot is taken, Greenshot checks `CoreConfig.PlayCameraSound`. If the user turned off camera sounds in Greenshot's Settings dialog, no sound is played; if they turned it on, the sound plays.
  - The `Source` step omits `"CaptureMouseCursor"`. Greenshot dynamically evaluates `CoreConfig.CaptureMousepointer`.
  - The `Destinations` step omits `"DestinationDesignations"`. Greenshot exports to whatever destinations the user currently has selected in `CoreConfig.OutputDestinations` (e.g. Editor, Clipboard, File).
* **Why it exists**: Recipes do not become outdated when users change their general preferences in the Settings dialog. Standard recipes stay fully synchronized with user settings automatically.

---
> [!IMPORTANT]
> **Beta Feature Gating (`IsBetaTester`)**:
> The external recipe engine, custom recipe loading, systray recipe menu, and interactive approval prompts are gated behind the `IsBetaTester` flag.
> To enable external recipes, ensure your `greenshot.ini` contains:
> ```ini
> [Core]
> IsBetaTester=True
> ```
> When `IsBetaTester` is `False`, Greenshot operates strictly with its built-in hardcoded capture recipes.

---

## 3. Modular Triggers: Connecting Recipes to Hotkeys & Menus

Triggers define how and when a recipe executes. In Greenshot, triggers are modular first-class entities configured directly within the recipe's `triggers` array.

A single recipe can define **multiple triggers**—for example, both a systray context menu item and a global keyboard shortcut:

```json
"triggers": [
  {
    "triggerType": "ContextMenu",
    "name": "Systray Menu Entry",
    "parameters": {
      "menuItemText": "Region with Blue Border",
      "group": "Recipes"
    }
  },
  {
    "triggerType": "Hotkey",
    "name": "Keyboard Shortcut",
    "parameters": {
      "hotkey": "Ctrl + Shift + B"
    }
  }
]
```

### Supported Trigger Types
- **`ContextMenu` / `Systray`**: Registers an entry in Greenshot's systray context menu. If the recipe also defines a `Hotkey` trigger, the menu item automatically displays the shortcut accelerator (e.g. `Region with Blue Border   Ctrl+Shift+B`).
- **`Hotkey`**: Registers an OS-level global hotkey (e.g. `"Ctrl + Shift + B"`, `"Alt + PrintScreen"`).
- **`Clipboard`**: Monitors the Windows clipboard and fires automatically when an image is copied (`"OnImageCopied": true`).
- **`Manual`**: Explicitly manual trigger invoked via CLI or API.

### How Triggerless Recipes Run
If a recipe has an empty or omitted `triggers` list:
1. **Systray Context Menu**: If `showInContextMenu` is `true`, the recipe appears in Greenshot's systray context menu.
2. **Command-Line Interface (CLI)**: Running `greenshot.exe /recipe:recipe_region_blue_border`.
3. **Plugins & API**: Programmatic invocation via `CapturePipeline.Instance.ExecuteAsync(recipe)`.

---

## 4. Security Architecture & Threat Model

> [!CAUTION]
> **Why Unrestricted Configuration is Dangerous**:
> If an unprivileged malicious process running in the user session could silently write to `greenshot.ini` or drop recipe files, it could attempt:
> 1. **Arbitrary Code Execution (RCE)**: Specifying an `ExternalCommand` step to launch malicious payloads (`powershell.exe`, reverse shells) under Greenshot's process.
> 2. **Silent Surveillance / Spyware**: Taking fullscreen captures with sound and notifications disabled (`PlaySound: false`, `ShowNotification: false`), quietly saving screenshots to a hidden directory (`FileNoDialog`).
> 3. **Clipboard Sniffing**: Attaching a clipboard trigger to secretly intercept sensitive passwords copied from password managers.
> 4. **Workflow Hijacking**: Overriding `recipe_region` so standard captures are secretly copied to an attacker destination while showing the normal crosshair UI.

### Defense-in-Depth Protections
To completely neutralize these attack vectors, Greenshot implements multiple defensive layers:

1. **Dedicated File Extension (`.gsrecipe.json`)**:
   External recipes must use the `.gsrecipe.json` extension (e.g. `blue_border.gsrecipe.json`), preventing accidental execution of generic JSON files.
2. **Interactive Trust Prompt (Modern WPF Dialog)**:
   When Greenshot detects an external recipe file for the first time, it does **not** execute it blindly. Greenshot presents a modern Fluent WPF dialog showing the recipe's name, version, file path, SHA-256 fingerprint, attached triggers, and execution steps. The UI dynamically supports Windows Dark and Light modes.
3. **Cryptographic SHA-256 Hash Pinning (DPAPI-Protected)**:
   Upon user approval, the recipe file's SHA-256 hash is recorded in a protected binary trust store (`%LOCALAPPDATA%\Greenshot\recipe_trust.dat`) encrypted via **Windows DPAPI (`ProtectedData.Protect`)** combined with an **application-specific HMAC salt**. Other user processes cannot forge this cryptographic approval. If the file is altered, the hash mismatch blocks execution until re-approved.
4. **Mandatory Authorization for `ExternalCommand`**:
   Recipes containing `ExternalCommand` steps are flagged with a high-visibility security badge. The user must explicitly check a confirmation box (*"I understand the security risks and authorize this recipe to execute external commands"*) before approval can be granted.
5. **Enterprise Lockdown (`greenshot-fixed.ini`)**:
   In managed corporate environments, `RecipeFiles` can be configured in `%ProgramFiles%\Greenshot\greenshot-fixed.ini`. Because `%ProgramFiles%` requires Windows Administrator (UAC) elevation to write to, low-privilege malware cannot tamper with it.

---

## 5. Formal JSON Schema Contract

Greenshot recipes adhere to the **Draft-07 JSON Schema** located at:
[`docs/recipe.schema.json`](recipe.schema.json)

Link this schema in your `.gsrecipe.json` files for instant editor autocomplete, validation, and hover documentation:

```json
{
  "$schema": "./recipe.schema.json",
  "version": "1.0",
  "id": "my_recipe",
  "name": "My Custom Recipe",
  "triggers": [ ... ],
  "steps": [ ... ]
}
```

### Root Recipe Properties

| Property | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `$schema` | `string` | No | Path or URL to `recipe.schema.json` |
| `version` | `string` | **Yes** | Recipe schema version (e.g. `"1.0"`) |
| `id` | `string` | **Yes** | Unique identifier (e.g. `recipe_region` to override default region capture) |
| `name` | `string` | **Yes** | Human-readable title displayed in menus |
| `description` | `string` | No | Description of what the flow does |
| `enabled` | `boolean` | No | Whether the recipe is active (default: `true`) |
| `triggers` | `array` | No | List of modular trigger objects attached to this recipe |
| `steps` | `array` | **Yes** | Ordered list of step objects (minimum 1) |

---

## 6. Step Types & Parameters Reference

### Step: `Source`
Acquires raw pixels, restores/activates target windows, and aligns display DPI.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `SourceType` | `string` | `"Region"` | One of: `"Region"`, `"Window"`, `"ActiveWindow"`, `"FullScreen"`, `"LastRegion"`, `"Clipboard"`, `"File"`, `"TextOcr"` |
| `WindowTitle` | `string` | `null` | Exact or substring title of a specific window to target, restore, bring to front, and capture |
| `WindowTitlePattern` | `string` | `null` | Regular expression pattern used to match target window title |
| `ProcessName` | `string` | `null` | Target process executable name (e.g. `"notepad"`, `"Greenshot"`) |
| `MatchCase` | `boolean` | `false` | Whether title or regex pattern matching is case-sensitive |
| `CaptureMouseCursor` | `boolean` | `null` | Pre-defines mouse capture. If omitted (`null`), dynamically evaluates `CoreConfig.CaptureMousepointer` |
| `DelayMs` | `integer` | `null` | Milliseconds to wait before capture. If omitted (`null`), dynamically evaluates `CoreConfig.CaptureDelay` |
| `AlignDpi` | `boolean` | `true` | Aligns bitmap resolution to match physical display DPI |
| `ScreenCaptureMode` | `string` | `null` | Screen mode: `"Auto"`, `"Fixed"`, `"FullScreen"` |
| `WindowCaptureMode` | `string` | `null` | Window mode: `"Auto"`, `"AsDisplayed"`, `"GDI"`, `"Aero"`, `"AeroTransparent"` |

### Step: `InteractiveSelection`
Presents the interactive selection rectangle or window picker overlay.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `SelectionMode` | `string` | `"Region"` | One of: `"Region"`, `"Window"`, `"Text"` |
| `AllowWindowSnapping` | `boolean` | `true` | Whether the selection rectangle snaps to windows under the cursor |

### Step: `Effect`
Applies an image effect directly to the capture surface. Multiple `Effect` steps can be sequenced.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Effect` | `string` | `"Border"` | One of: `"Border"`, `"DropShadow"`, `"TornEdge"`, `"Invert"`, `"Grayscale"`, `"Monochrome"`, `"Adjust"`, `"Rotate"`, `"Resize"`, `"ResizeCanvas"`, `"ReduceColors"`, `"RemoveTransparency"` |
| `Width` | `integer` | `2` | Used for `"Border"` (thickness) or `"Resize"` (width in pixels) |
| `Height` | `integer` | `null` | Used for `"Resize"`: target height in pixels |
| `Percentage` | `number` | `null` | Used for `"Resize"`: scaling percentage (e.g. `50` for 50%) |
| `MaintainAspectRatio` | `boolean` | `true` | Used for `"Resize"`: preserve original aspect ratio |
| `Color` | `string` | `"#000000"` | Used for `"Border"`, `"ResizeCanvas"`, `"RemoveTransparency"`: color hex or name |
| `Darkness` | `number` | `0.6` | Used for `"DropShadow"` and `"TornEdge"`: shadow darkness (0.0 to 1.0) |
| `ShadowSize` | `integer` | `7` | Used for `"DropShadow"` and `"TornEdge"`: shadow blur/size in pixels |
| `ToothHeight` | `integer` | `12` | Used for `"TornEdge"`: height of paper teeth in pixels |
| `HorizontalToothRange` | `integer` | `20` | Used for `"TornEdge"`: horizontal tooth interval |
| `VerticalToothRange` | `integer` | `20` | Used for `"TornEdge"`: vertical tooth interval |
| `GenerateShadow` | `boolean` | `true` | Used for `"TornEdge"`: whether to render drop shadow along torn edge |
| `Edges` | `array / string` | `[true,true,true,true]` | Used for `"TornEdge"`: which edges to tear (e.g. `"top,bottom"` or `[true, false, true, false]`) |
| `Threshold` | `integer` | `128` | Used for `"Monochrome"`: black/white luminance threshold (0 - 255) |
| `Brightness` | `number` | `1.0` | Used for `"Adjust"`: brightness multiplier (1.0 = normal) |
| `Contrast` | `number` | `1.0` | Used for `"Adjust"`: contrast multiplier (1.0 = normal) |
| `Gamma` | `number` | `1.0` | Used for `"Adjust"`: gamma multiplier (1.0 = normal) |
| `Angle` | `integer` | `90` | Used for `"Rotate"`: rotation angle in degrees (`90`, `-90`, `270`) |
| `Margin` | `integer` | `0` | Used for `"ResizeCanvas"`: uniform border padding around image |
| `Colors` | `integer` | `256` | Used for `"ReduceColors"`: maximum number of quantized colors |

### Step: `TextEffect`
Runs OCR text recognition, locates occurrences matching regex patterns, and places effect containers (`Blur`, `Pixelize`, `Highlight`, `Redact`, `Magnify`) directly over matched coordinates.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Pattern` | `string` | `null` | Single regular expression pattern to detect (e.g. `"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}"`) |
| `Patterns` | `array of string` | `null` | Array of regex patterns to match (e.g. credit cards, API keys, emails) |
| `Effect` | `string` | `"Pixelize"` | One of: `"Pixelize"`, `"Blur"`, `"Highlight"`, `"Redact"`, `"Blackout"`, `"Magnify"` |
| `Scope` | `string` | `"Auto"` | `"Auto"` (maps regex match to exact word bounds), `"Word"` (matches word tokens), or `"Line"` (covers whole line) |
| `BlurRadius` | `integer` | `10` | Used for `"Blur"`: blur radius in pixels |
| `PixelSize` | `integer` | `5` | Used for `"Pixelize"`: obfuscation pixel block size |
| `FillColor` | `string` | `"#FFFF00"` / `"#000000"` | Color used for `"Highlight"` (yellow) or `"Redact"` (black) |
| `MagnificationFactor` | `integer` | `2` | Used for `"Magnify"`: magnification scale factor |
| `PaddingHorizontal` | `integer` | `10` | Percentage to grow matched bounding box horizontally |
| `PaddingVertical` | `integer` | `20` | Percentage to grow matched bounding box vertically |
| `OffsetHorizontal` | `integer` | `0` | Pixel horizontal offset for effect container |
| `OffsetVertical` | `integer` | `0` | Pixel vertical offset for effect container |
| `MatchCase` | `boolean` | `false` | Whether regex matching is case-sensitive |

### Step: `ImmediateFeedback`
Dispatches immediate sensory feedback upon pixel acquisition.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `PlaySound` | `boolean` | `null` | Pre-defines shutter sound. If omitted (`null`), dynamically evaluates `CoreConfig.PlayCameraSound` |

### Step: `Processors`
Executes image and metadata processors (e.g. OCR, TitleFix).

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `ProcessorIds` | `array of string` | `null` | Explicit list of processor designations to run (e.g. `["Windows10OcrProcessor"]`). If omitted (`null`), runs all active registered processors |

### Step: `Destinations`
Dispatches export to one or more output destinations.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `DestinationDesignations` | `array of string` | `null` | Explicit list of destinations (e.g. `["Clipboard", "FileNoDialog"]`). If omitted (`null`), dynamically evaluates `CoreConfig.OutputDestinations` |

### Step: `Notification`
Displays tray balloon or toast notifications upon completion.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `ShowNotification` | `boolean` | `null` | Pre-defines notification. If omitted (`null`), dynamically evaluates `CoreConfig.ShowTrayNotification` |

---

## 6. Concrete Configuration Examples

### Example 1: Capture with Border Flow
Here is the exact JSON recipe that acquires a region, interactively lets the user select the area, automatically adds a 4-pixel blue border around the capture, plays feedback, and exports to the user's preferred destinations:

```json
{
  "$schema": "./recipe.schema.json",
  "id": "recipe_region_blue_border",
  "name": "Region with Blue Border",
  "description": "Captures a selected region, adds a 4px blue border, and exports to destinations.",
  "enabled": true,
  "steps": [
    {
      "stepType": "Source",
      "name": "Acquire Screen",
      "parameters": {
        "SourceType": "Region"
      }
    },
    {
      "stepType": "InteractiveSelection",
      "name": "Select Area",
      "parameters": {
        "SelectionMode": "Region",
        "AllowWindowSnapping": true
      }
    },
    {
      "stepType": "Border",
      "name": "Add 4px Blue Border",
      "parameters": {
        "Width": 4,
        "Color": "#0078D7"
      }
    },
    {
      "stepType": "ImmediateFeedback",
      "name": "Shutter Sound"
    },
    {
      "stepType": "Processors",
      "name": "Run Processors"
    },
    {
      "stepType": "Destinations",
      "name": "Export"
    },
    {
      "stepType": "Notification",
      "name": "Notify User"
    }
  ]
}
```

### Example 2: Overriding the Built-In Region Capture
To override Greenshot's default region capture so that *every* region capture (including the `PrintScreen` hotkey) automatically applies a 2-pixel black border:

```json
{
  "$schema": "./recipe.schema.json",
  "id": "recipe_region",
  "name": "Capture region (with Border)",
  "description": "Default region capture overridden with an automatic border.",
  "enabled": true,
  "steps": [
    {
      "stepType": "Source",
      "parameters": {
        "SourceType": "Region"
      }
    },
    {
      "stepType": "InteractiveSelection",
      "parameters": {
        "SelectionMode": "Region"
      }
    },
    {
      "stepType": "Border",
      "parameters": {
        "Width": 2,
        "Color": "#000000"
      }
    },
    {
      "stepType": "ImmediateFeedback"
    },
    {
      "stepType": "Processors"
    },
    {
      "stepType": "Destinations"
    },
    {
      "stepType": "Notification"
    }
  ]
}
```

### Example 3: Silent Automated Window Capture directly to File
A recipe that waits 500ms, captures the active window without the cursor, plays no sound, shows no tray notification, and directly saves to a file without opening dialogs:

```json
{
  "$schema": "./recipe.schema.json",
  "id": "recipe_silent_window_to_file",
  "name": "Silent Window to File",
  "description": "Automated capture of active window directly saved to disk.",
  "enabled": true,
  "steps": [
    {
      "stepType": "Source",
      "parameters": {
        "SourceType": "ActiveWindow",
        "CaptureMouseCursor": false,
        "DelayMs": 500
      }
    },
    {
      "stepType": "Destinations",
      "parameters": {
        "DestinationDesignations": [
          "FileNoDialog"
        ]
      }
    }
  ]
}
```

### Example 4: Multiple Recipes in a Single File
A single `.json` file can also contain a JSON array of recipes:

```json
[
  {
    "id": "recipe_red_border",
    "name": "Red Border Flow",
    "steps": [
      { "stepType": "Source", "parameters": { "SourceType": "Region" } },
      { "stepType": "InteractiveSelection", "parameters": { "SelectionMode": "Region" } },
      { "stepType": "Border", "parameters": { "Width": 3, "Color": "#FF0000" } },
      { "stepType": "ImmediateFeedback" },
      { "stepType": "Destinations" }
    ]
  },
  {
    "id": "recipe_black_border",
    "name": "Black Border Flow",
    "steps": [
      { "stepType": "Source", "parameters": { "SourceType": "Region" } },
      { "stepType": "InteractiveSelection", "parameters": { "SelectionMode": "Region" } },
      { "stepType": "Border", "parameters": { "Width": 2, "Color": "#000000" } },
      { "stepType": "ImmediateFeedback" },
      { "stepType": "Destinations" }
    ]
  }
]
```

### Example 5: Targeted Window Capture with DropShadow and Regex DLP Redaction
A recipe that targets a specific window matching regex pattern `.*(Greenshot|Notepad|Browser).*`, restores/brings it to front, captures it, applies a 10px drop shadow, scans OCR text for sensitive data (credit cards, emails, API keys), applies blackout redaction, and opens the editor:

```json
{
  "$schema": "./recipe.schema.json",
  "version": "1.0",
  "id": "recipe_window_regex_redact",
  "name": "Target Window with DLP Redaction",
  "description": "Captures a targeted window, adds drop shadow, runs OCR to find sensitive patterns, redacts them, and opens the editor",
  "triggers": [
    {
      "triggerType": "ContextMenu",
      "parameters": {
        "menuItemText": "Target Window with DLP Redact",
        "group": "Recipes"
      }
    },
    {
      "triggerType": "Hotkey",
      "name": "Target Window with DLP Redaction Hotkey",
      "parameters": {
        "hotkey": "Ctrl + Shift + D"
      }
    }
  ],
  "steps": [
    {
      "stepType": "Source",
      "name": "Capture Target Window",
      "parameters": {
        "sourceType": "ActiveWindow",
        "windowTitlePattern": ".*(Notepad|Editor|Greenshot|Chrome|Edge).*",
        "matchCase": false
      }
    },
    {
      "stepType": "Effect",
      "name": "Apply Drop Shadow",
      "parameters": {
        "effect": "DropShadow",
        "shadowSize": 10,
        "darkness": 0.65
      }
    },
    {
      "stepType": "TextEffect",
      "name": "Redact Sensitive Data",
      "parameters": {
        "effect": "Redact",
        "fillColor": "#000000",
        "scope": "Auto",
        "patterns": [
          "\\b\\d{4}[ -]?\\d{4}[ -]?\\d{4}[ -]?\\d{4}\\b",
          "[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}",
          "\\b(AKIA|AIza|ghp_|glpat-)[A-Za-z0-9_\\-]{16,}\\b"
        ],
        "paddingHorizontal": 12,
        "paddingVertical": 25
      }
    },
    {
      "stepType": "ImmediateFeedback",
      "parameters": {
        "playSound": true
      }
    },
    {
      "stepType": "Destinations",
      "parameters": {
        "destinationDesignations": [
          "Editor"
        ]
      }
    }
  ]
}
```

