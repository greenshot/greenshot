# Greenshot Capture Recipes Guide

Greenshot features a modular, recipe-driven capture pipeline powered by a **Directed Acyclic Graph (DAG)** workflow engine. Instead of linear, hardcoded sequences, screenshot workflows are defined as graphs of configurable execution nodes with support for parallel branch splitting (fork), path merging (join), variable assignment, scoped user/computer environment expressions, and rich surface drawable placement.

Recipes can be written in code or provided as external `.json` (`.gsrecipe.json`) files. External JSON recipes can create new custom capture workflows or securely override Greenshot's built-in recipes.

---

## 1. Core Architecture: Decoupling Triggers from Recipes

In Greenshot:
- **A Trigger** defines *when and how* a capture is initiated (e.g. pressing a hotkey like `PrintScreen`, clicking a systray menu item, or receiving a clipboard image).
- **A Recipe** defines *what DAG workflow of nodes* executes once triggered (e.g. acquiring pixels, showing an interactive selection rectangle, evaluating environment variables, branching to parallel feedback and watermark steps, running OCR, stamping drawables, and exporting to destinations).

Triggers and recipes are decoupled. Any trigger can run any recipe, and a single recipe can be executed by multiple triggers or invoked manually.

---

## 2. Directed Acyclic Graph (DAG) Workflow Engine

Every recipe defines:
1. **`nodes`**: A list of execution nodes, each having a unique flow-local `id`, a `stepType`, an optional `name`, and step-specific `parameters`.
2. **`flow`**: A graph configuration defining entry point(s) (`startNode` or `startNodes`) and transitions between nodes (`transitions` or `edges`).

```
                    ┌─────────────────┐
                    │  capture_node   │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │   select_node   │
                    └────────┬────────┘
                             │ (Fork / Split)
                ┌────────────┴────────────┐
                │                         │
       ┌────────▼────────┐       ┌────────▼────────┐
       │ feedback_branch │       │  set_vars_node  │
       └────────┬────────┘       └────────┬────────┘
                │                         │
                │                ┌────────▼────────┐
                │                │  watermark_node │
                │                └────────┬────────┘
                │                         │
                └────────────┬────────────┘
                             │ (Join / Merge)
                    ┌────────▼────────┐
                    │   export_join   │
                    └─────────────────┘
```

### Fork & Join Execution Semantics
- **Parallel Branching (Fork)**: When a transition maps one node to multiple targets (e.g. `"select_node": ["feedback_branch", "set_vars_node"]`), both downstream branches execute concurrently as async tasks.
- **Barrier Synchronization (Join)**: When multiple branches converge into a single downstream node (e.g. `export_join`), the engine pauses execution of that node until **all** parent dependency branches have fully completed.
- **Acyclic Enforcement**: The workflow engine performs depth-first cycle detection during recipe load. If any cycle/loop is detected, the recipe is rejected with a validation error.

### Flow Definition Syntax
Transitions can be specified using either `transitions` (adjacency map) or `edges` (edge list):

```json
"flow": {
  "startNode": "source",
  "transitions": {
    "source": "select",
    "select": ["feedback", "watermark"],
    "watermark": "export",
    "feedback": "export"
  }
}
```

Or using edge list syntax:
```json
"flow": {
  "startNode": "source",
  "edges": [
    { "from": "source", "to": "select" },
    { "from": "select", "to": "feedback" },
    { "from": "select", "to": "watermark" },
    { "from": "watermark", "to": "export" },
    { "from": "feedback", "to": "export" }
  ]
}
```

---

## 3. Dynamic Expressions & Scoped Environment Variables

Recipe step parameters support embedded mathematical expressions, boolean conditions, string interpolation, and system environment information using `${...}` syntax exclusively.

### Scopes & Variable Sources
Greenshot expression evaluation separates system information into distinct scopes:

| Scope Prefix | Source | Description | Example |
|---|---|---|---|
| `user.*` | Windows User Environment | User-specific environment variables (from `EnvironmentVariableTarget.User`) | `${user.username}`, `${user.temp}`, `${user.appdata}` |
| `machine.*` | Windows Machine Environment | System-wide / machine-scoped environment variables (from `EnvironmentVariableTarget.Machine`) | `${machine.computername}`, `${machine.os}`, `${machine.programfiles}` |
| `config.*` | Greenshot INI Configuration | Live configuration settings from `greenshot.ini` | `${config.language}`, `${config.capturemousepointer}` |
| `context.*` | Pipeline Flow Context | Variables stored in `context.Properties` or set by preceding steps | `${context.watermark_text}`, `${context.WindowTitle}` |
| `payload.*` | Capture Surface Metrics | Dynamic dimensions and details of the current capture | `${payload.width}`, `${payload.height}`, `${payload.extractedtext}` |
| `now:format` | Date / Time | Current timestamp formatted with .NET DateTime format strings | `${now:yyyy-MM-dd HH:mm:ss}` |

### Mathematical & Logical Expressions
Parameters such as coordinates, widths, heights, and variables can be calculated dynamically inside `${...}`:
- `"${payload.width - 200}"`
- `"${payload.height * 0.5}"`
- `"${(payload.width / 2) - 50}"`

### Setting Variables (`SetVariable` Step)
Create or transform variables in the pipeline context for downstream nodes to consume:

```json
{
  "id": "set_metadata",
  "stepType": "SetVariable",
  "parameters": {
    "variables": {
      "captured_by": "Captured by ${user.username} on ${machine.computername}",
      "watermark_box_width": "${payload.width * 0.4}",
      "timestamp_header": "[${now:yyyy-MM-dd HH:mm:ss}]"
    }
  }
}
```

---

## 4. Surface Drawables (`Drawable` Step)

The `Drawable` step allows adding any Greenshot drawable container to the captured surface.

### Supported Drawable Types
- **Shapes & Lines**: `Rectangle`, `Ellipse`, `Line`, `Arrow`, `Freehand`
- **Text & Annotations**: `Text`, `Speechbubble`, `StepLabel`
- **Images & Icons**: `Image`, `Icon`, `Cursor`, `Emoji`, `Svg`
- **Filters & Effects**: `Obfuscate`, `Blur`, `Pixelize`, `Highlight`, `Magnify`, `Crop`

### Flexible Positioning: Absolute, Calculated & Anchored
Drawables can be positioned using:
1. **Absolute Coordinates**: Fixed integers (`left: 50, top: 100, width: 200, height: 40`).
2. **Calculated Expressions**: Dynamic formulas using `${payload.width}` and `${payload.height}` (e.g. `top: "${payload.height - 60}"`, `width: "${payload.width / 2}"`).
3. **Anchor Alignments**:
   - `horizontalAnchor`: `"Left"`, `"Center"` (or `"Middle"`), `"Right"`
   - `verticalAnchor`: `"Top"`, `"Center"` (or `"Middle"`), `"Bottom"`
   - Optional `offsetX` and `offsetY` pixel adjustments.

#### Example Drawable Node Configuration
```json
{
  "id": "stamp_watermark",
  "stepType": "Drawable",
  "parameters": {
    "drawables": [
      {
        "type": "Rectangle",
        "horizontalAnchor": "Right",
        "verticalAnchor": "Bottom",
        "width": 380,
        "height": 40,
        "offsetX": -15,
        "offsetY": -15,
        "fillColor": "rgba(0, 0, 0, 180)",
        "lineColor": "#0078D7",
        "lineThickness": 2,
        "shadow": true
      },
      {
        "type": "Text",
        "horizontalAnchor": "Right",
        "verticalAnchor": "Bottom",
        "width": 370,
        "height": 30,
        "offsetX": -20,
        "offsetY": -20,
        "text": "User: ${user.username} | Host: ${machine.computername} | ${now:yyyy-MM-dd}",
        "textColor": "#FFFFFF",
        "fontSize": 9.5,
        "bold": true
      },
      {
        "type": "Emoji",
        "horizontalAnchor": "Left",
        "verticalAnchor": "Bottom",
        "left": 20,
        "top": "${payload.height - 45}",
        "emoji": "🛡️",
        "size": 32
      }
    ]
  }
}
```

---

## 5. Configuration Precedence Explained

Greenshot resolves every parameter using a strict **three-tier precedence model**:

```
┌─────────────────────────────────────────────────────────────┐
│ Priority 1: Runtime Context Override                        │
│   (Forced for this single run by CLI, Trigger, or API)      │
├─────────────────────────────────────────────────────────────┤
│ Priority 2: Node Parameter Pre-definition                   │
│   (Explicitly defined in the recipe JSON or C# code)        │
├─────────────────────────────────────────────────────────────┤
│ Priority 3: Dynamic User Configuration Evaluation           │
│   (Omitted/null in JSON → evaluated live from greenshot.ini)│
└─────────────────────────────────────────────────────────────┘
```

1. **Priority 1 (Runtime Context Override)**: Explicit parameters supplied for this specific run in `context.Properties` (e.g. from CLI switch `--no-mouse`).
2. **Priority 2 (Node Parameter Pre-definition)**: Hardcoded parameter or expression defined on the node in the recipe JSON.
3. **Priority 3 (Dynamic Evaluation)**: Parameter omitted or `null` in JSON → dynamically evaluates live setting from `greenshot.ini` (`CoreConfig`).

---

## 6. Complete Recipe Examples

### Example 1: Region Capture with Blue Border
Interactive region capture that adds a 2px blue border and opens the editor:

```json
{
  "$schema": "./recipe.schema.json",
  "id": "recipe_region_blue_border",
  "name": "Region with Blue Border",
  "description": "Interactive region capture that adds a 2px blue border and opens the editor",
  "triggers": [
    {
      "triggerType": "ContextMenu",
      "name": "Region with Border Menu Item",
      "parameters": {
        "menuItemText": "Region with Blue Border",
        "group": "Recipes"
      }
    },
    {
      "triggerType": "Hotkey",
      "parameters": {
        "hotkey": "Ctrl + Shift + B"
      }
    }
  ],
  "nodes": [
    {
      "id": "source",
      "stepType": "Source",
      "parameters": { "sourceType": "Region" }
    },
    {
      "id": "select",
      "stepType": "InteractiveSelection",
      "parameters": { "selectionMode": "Region" }
    },
    {
      "id": "border",
      "stepType": "Effect",
      "parameters": { "effect": "Border", "width": 2, "color": "#0000FF" }
    },
    {
      "id": "feedback",
      "stepType": "ImmediateFeedback",
      "parameters": { "playSound": true }
    },
    {
      "id": "destination",
      "stepType": "Destinations",
      "parameters": { "destinationDesignations": [ "Editor" ] }
    }
  ],
  "flow": {
    "startNode": "source",
    "transitions": {
      "source": "select",
      "select": "border",
      "border": "feedback",
      "feedback": "destination"
    }
  }
}
```

### Example 2: Branching DAG Workflow with Environment Watermark
Demonstrating parallel fork/join execution, variable evaluation, user & machine environment scopes, and drawable surface stamping:

```json
{
  "$schema": "./recipe.schema.json",
  "id": "recipe_dag_watermark_branching",
  "name": "DAG Workflow with Environment Watermark and Parallel Feedback",
  "description": "Demonstrates DAG branching, variable assignment, scoped user/machine environment variables, mathematical positioning expressions, and multi-drawable surface stamping.",
  "triggers": [
    {
      "triggerType": "Hotkey",
      "parameters": { "hotkey": "Ctrl + Shift + W" }
    }
  ],
  "nodes": [
    {
      "id": "capture_node",
      "stepType": "Source",
      "parameters": { "sourceType": "Region" }
    },
    {
      "id": "select_node",
      "stepType": "InteractiveSelection",
      "parameters": { "selectionMode": "Region", "allowWindowSnapping": true }
    },
    {
      "id": "feedback_branch",
      "stepType": "ImmediateFeedback",
      "parameters": { "playSound": true }
    },
    {
      "id": "set_vars_node",
      "stepType": "SetVariable",
      "parameters": {
        "variables": {
          "department": "Engineering QA",
          "watermark_text": "Captured by ${user.username} on ${machine.computername} [${now:yyyy-MM-dd HH:mm}]"
        }
      }
    },
    {
      "id": "watermark_node",
      "stepType": "Drawable",
      "parameters": {
        "drawables": [
          {
            "type": "Rectangle",
            "horizontalAnchor": "Right",
            "verticalAnchor": "Bottom",
            "width": 380,
            "height": 40,
            "offsetX": -15,
            "offsetY": -15,
            "fillColor": "rgba(0, 0, 0, 180)",
            "lineColor": "#0078D7",
            "lineThickness": 2,
            "shadow": true
          },
          {
            "type": "Text",
            "horizontalAnchor": "Right",
            "verticalAnchor": "Bottom",
            "width": 370,
            "height": 30,
            "offsetX": -20,
            "offsetY": -20,
            "text": "${context.watermark_text}",
            "textColor": "#FFFFFF",
            "fontSize": 9.5,
            "fontFamily": "Segoe UI",
            "bold": true
          }
        ]
      }
    },
    {
      "id": "export_join",
      "stepType": "Destinations",
      "parameters": {
        "destinationDesignations": [ "Editor", "Clipboard" ]
      }
    }
  ],
  "flow": {
    "startNode": "capture_node",
    "transitions": {
      "capture_node": "select_node",
      "select_node": [ "feedback_branch", "set_vars_node" ],
      "set_vars_node": "watermark_node",
      "feedback_branch": "export_join",
      "watermark_node": "export_join"
    }
  }
}
```


### Example 3: Targeted Window Capture with Regex DLP Redaction
Captures a specific window, applies a drop shadow, scans OCR text for sensitive patterns, redacts them with black bounding boxes, and opens the editor:

```json
{
  "$schema": "./recipe.schema.json",
  "id": "recipe_window_regex_redact",
  "name": "Target Window with DLP Redaction",
  "description": "Captures a targeted window, adds drop shadow, runs OCR to find sensitive patterns, redacts them, and opens the editor",
  "triggers": [
    {
      "triggerType": "Hotkey",
      "parameters": { "hotkey": "Ctrl + Shift + D" }
    }
  ],
  "nodes": [
    {
      "id": "capture_window",
      "stepType": "Source",
      "parameters": {
        "sourceType": "ActiveWindow",
        "windowTitlePattern": ".*(Notepad|Editor|Greenshot|Chrome|Edge).*",
        "matchCase": false
      }
    },
    {
      "id": "shadow",
      "stepType": "Effect",
      "parameters": { "effect": "DropShadow", "shadowSize": 10, "darkness": 0.65 }
    },
    {
      "id": "redact",
      "stepType": "TextEffect",
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
      "id": "feedback",
      "stepType": "ImmediateFeedback",
      "parameters": { "playSound": true }
    },
    {
      "id": "destination",
      "stepType": "Destinations",
      "parameters": { "destinationDesignations": [ "Editor" ] }
    }
  ],
  "flow": {
    "startNode": "capture_window",
    "transitions": {
      "capture_window": "shadow",
      "shadow": "redact",
      "redact": "feedback",
      "feedback": "destination"
    }
  }
}
```
