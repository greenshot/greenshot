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
Acquires raw pixels and aligns display DPI.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `SourceType` | `string` | `"Region"` | One of: `"Region"`, `"Window"`, `"ActiveWindow"`, `"FullScreen"`, `"LastRegion"`, `"Clipboard"`, `"File"`, `"TextOcr"` |
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

### Step: `Border`
Adds a border around the captured image without opening any modal dialogs.

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Width` | `integer` | `2` | Border thickness in pixels (must be >= 1) |
| `Color` | `string` | `"#000000"` | HTML hex code (`"#FF0000"`, `"#336699"`) or standard named color (`"Red"`, `"Black"`, `"Navy"`) |

### Step: `Effect`
Applies an image effect (border, shadow, torn edge, color inversion, grayscale).

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Effect` | `string` | `"Border"` | One of: `"Border"`, `"DropShadow"`, `"TornEdge"`, `"Invert"`, `"Grayscale"` |
| `Width` | `integer` | `2` | Used when `Effect` is `"Border"`: border thickness |
| `Color` | `string` | `"#000000"` | Used when `Effect` is `"Border"`: border color |

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
