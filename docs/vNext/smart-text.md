# Smart text

Smart text finds data in the text of a capture: web addresses, e-mail addresses, phone numbers, IBANs, dates, issue keys and your
own patterns, e.g. the customer number in a window of a remote desktop. Instead of copying all the OCR text you pick what you need,
in a window or in a recipe. The plugin is `Greenshot.Plugin.SmartText`, a **beta** feature of the full Greenshot (not in Greenshot Light).

- [For users](#for-users)
- [Own patterns](#own-patterns)
- [In a recipe](#in-a-recipe)
- [For developers](#for-developers)

---

## For users

### Turn it on

Smart text is only there for beta testers: in the settings, *Expert*, check *Enable to enable beta-test features*
(`BetaTester=True` in `[Core]` of greenshot.ini) and restart Greenshot. The installer has it as *Smart text plug-in (beta)*.
Smart text reads the text with Windows OCR, so a Windows OCR language must be installed (the same as for the OCR destination).

### Open it

- *Capture smart text* in the tray menu: select a region like the text capture (T), the window shows what was found.
  Its hotkey can be set like the one of every built-in recipe.
- The destination *Smart text* (in the destination picker and the editor), for a capture you already have.
- A recipe with the *Smart text* step and `Pick` `Ask` (see [In a recipe](#in-a-recipe)).
- While selecting, in the text mode (T): every item found so far gets a number badge on the screen and a row in a list in a
  corner, filled in as the text arrives. A click on a badge or a row (or its number key, 1 to 9 and 0) shows *Open* and *Copy*,
  Ctrl+click (or Ctrl+number) opens it right away, or copies it when it can't be opened. Both end the capture. A click on the
  text itself still selects text.

### The window

The capture is on the left, every item found has a numbered box; the list on the right has the same numbers.

| What | How |
|---|---|
| Copy one value | Enter, a double click, or *Copy*: the selected value goes to the clipboard and the window closes |
| Copy several | Space (or the check box) checks items, *Copy selected* copies them, one per line. Ctrl+A checks all items of the filter |
| Copy everything | *Copy all text* copies the whole OCR text, as the OCR destination does |
| Correct a value | F2 goes to *Value*: what you type there is what is copied (OCR misreads happen). Esc goes back to the list |
| Open | *Open* (web address), *Write e-mail*, *Call* (phone number), or Ctrl+O. Only `http`, `https`, `mailto` and `tel` are opened, everything else is copy-only |
| Find an item | 1 to 9 jump to the item with that number, a click on a box selects it; the filter box and the chips (*All*, *Web address*, ...) narrow the list |
| Close | Esc or *Close* |

When a recipe asks, the buttons say *Use* instead of *Copy*: the picked values go to the recipe.

### Which detectors

*Settings*, *Plugins*, *Smart text* lists the built-in detectors with a check box each. On by default: web address, e-mail
address, phone number, IBAN (with its check digits), date and issue key (`PROJ-123`). Off by default: IP address, MAC
address, GUID, hash, error code (`0x80070005`), file path, amount of money, VAT ID, BIC, ISBN, EAN, card number (Luhn check digit)
and coordinates. The setting is kept in `[SmartText]` `Detectors` of greenshot.ini, only what differs from the default.

Some details of the built-in detectors:

- A web address broken over two lines by OCR is joined when the next line starts where it starts. A trailing `.` or `)` is not
  part of the address.
- Phone numbers: `+49 30 1234567`, `0049 (0)30 123 45-67` and `(030) 1234567` are found; the value has only the digits and a
  leading `+`. Numbers with dots (versions, IP addresses) and ISO dates are not phone numbers.
- Dates become `yyyy-MM-dd`; the order of `9/11/2026` follows the date format of Windows, month names are those of the
  language of Windows.
- A web address starts with `http://`, `https://` or `www.`; a bare domain (`getgreenshot.org`) or a file name (`readme.txt`) is
  not a web address.
- Windows OCR misreads underlined text (an underlined `r` becomes `c`, an `o` becomes `Q`, underlined links can become
  garbage). Greenshot gives Windows OCR the capture as it is.
- When two detectors find the same text, an own pattern wins, then a checked value (IBAN, date, ...), then the longer one.

---

## Own patterns

An own pattern finds a value with a regular expression, optionally next to a label. *New...* on the settings page opens the
editor; the *Test text* shows at once what the pattern finds.

| Field | Meaning |
|---|---|
| Name | Shown in the window and used by recipes (`Detectors`) |
| Pattern | A .NET regular expression, e.g. `\b\d{4}-?\d{4}\b`. A pattern which takes longer than 250 ms on a text is stopped. With a label the pattern only needs the value itself. To take only a part of the match, name that part `value`: `Order #(?<value>\d+)` finds `1234` in `Order #1234` |
| Label | A regular expression for the label, e.g. `Kunden(nummer\|-?nr\.?)`; empty: no label |
| Where the value is | Right of the label (same line), below it (the next lines, under the label), right or else below, or anywhere when the label is in the capture |
| Where to look in the capture | Everywhere, a half (top, bottom, left, right) or a quarter of the capture. A pattern file can have any other area (`area`, fractions of the capture), the editor then shows it as *Custom* |
| In numbers, read O as 0 and l as 1 | OCR reads `0` as `O` and `1` as `l` or `I`: in tokens which are mostly digits these (and Z, S, B, G) are read as digits |
| At least / at most this many digits | The value has at least or at most this many digits; empty: any number. E.g. 8 and 8 for a customer number of exactly 8 digits |
| Change the value | Keep it, remove spaces, dashes and dots, upper or lower case |
| Check the value | IBAN check digits (mod 97), card number check digit (Luhn), ISBN, EAN |
| Open with | A web address with `{value}`, e.g. `https://crm.example.com/customer?id={value}`: the window offers *Open* for the value. The value is encoded, it can't change the server of the address |

The patterns are files in the folder `SmartText` next to greenshot.ini, one `<name>.gssmarttext.json` per pattern:

```json
{
  "kind": "SmartText",
  "version": 1,
  "name": "Customer number",
  "pattern": "\\b\\d{4}-?\\d{4}\\b",
  "label": "Kunden(nummer|-?nr\\.?)",
  "digitLookAlikes": true,
  "normalize": "RemoveSeparators"
}
```

Like the [external commands](external-commands.md), a file Greenshot didn't write is ignored, and one changed outside Greenshot is
only used after you approved it (Greenshot asks once it runs, or *Approve...* on the settings page). *Import...* and *Export...*
pass patterns on; an import always shows the pattern, its label and where it sends values before anything is stored. Only import
patterns from people you trust: a pattern with *Open with* sends the values it finds to that web address.

---

## In a recipe

The step `SmartText` (category *Analysis*) runs OCR when the capture has no text yet and finds the items.

| Parameter | Default | Meaning |
|---|---|---|
| `Detectors` | all which are on | Ids of built-in detectors (`Url`, `Email`, `Phone`, `Iban`, `Date`, `IssueKey`, `IpAddress`, `MacAddress`, `Guid`, `Hash`, `ErrorCode`, `Path`, `Money`, `VatId`, `Bic`, `Isbn`, `Ean`, `CardNumber`, `Coordinates`) and names of own patterns. With `Patterns` and no `Detectors` only the patterns of the recipe are used |
| `Patterns` | none | Patterns of the recipe, the fields of a pattern file (camelCase) without `openUrl` |
| `Pick` | `AskIfMore` | `First`, `All`, `Ask` (the window) or `AskIfMore` (the window only when more than one item was found). Without a user (command line, AI tool) `Ask` and `AskIfMore` take the first item, so the recipe doesn't fail on the choice |
| `Area` | the whole capture | `{"left": 0.5, "top": 0, "width": 0.5, "height": 0.3}`; replaces the area of the patterns |
| `Language` | the OCR language of the settings | The OCR language when OCR runs here, e.g. `de-DE` |
| `ResultName` | `Found` | The name of the results |

The results come from the capture, so they are untrusted values (see [variables.md](variables.md)):

| Variable | Value |
|---|---|
| `${var.Found}` | The value of the (first) picked item |
| `${var.Found.text}` | That item as OCR read it |
| `${var.Found.kind}` | Its detector: a built-in id or the name of the own pattern |
| `${var.Found.count}` | How many items were found |
| `${var.Found.picked}` | How many were picked (0: nothing found, or the window was closed) |
| `${var.Found.all}` | All picked values, one per line |

The values are not logged. The step doesn't change `Payload.ExtractedText`: the following steps say what they use, e.g.
the clipboard step with `ClipboardMode` `TextOnly` and `ClipboardCustomText` `${var.Found}`.

### Example: copy the customer number from a remote desktop window

The window is found by its process and title; `WhenNoWindow` `Stop` ends the recipe when the window isn't there, instead of
capturing the screen. The `Notification` step's `Message` tells the user what happened.

```jsonc
{
  "version": "2.0",
  "id": "crm_customer_number",
  "name": "Copy the customer number from the CRM window",
  "triggers": [ { "triggerType": "Hotkey", "parameters": { "Hotkey": "Ctrl + Alt + K" } } ],
  "nodes": [
    { "id": "source", "stepType": "Source", "name": "Capture the CRM window",
      "parameters": { "SourceType": "Window", "ProcessName": "wfica32", "WindowTitlePattern": "CRM", "WhenNoWindow": "Stop" } },
    { "id": "find", "stepType": "SmartText", "name": "Find the customer number",
      "parameters": {
        "Language": "de-DE",
        "ResultName": "CustomerNumber",
        "Patterns": [ { "name": "Customer number", "pattern": "\\b\\d{4}-?\\d{4}\\b", "label": "Kunden(nummer|-?nr\\.?)",
                        "digitLookAlikes": true, "normalize": "RemoveSeparators" } ] } },
    { "id": "found", "stepType": "Conditional", "name": "Found?",
      "parameters": { "Branches": [ { "Key": "Yes", "Expression": "${var.CustomerNumber.count > 0}" },
                                    { "Key": "No", "Expression": "else" } ] } },
    { "id": "copy", "stepType": "Clipboard", "name": "Copy it",
      "parameters": { "ClipboardMode": "TextOnly", "ClipboardCustomText": "${var.CustomerNumber}" } },
    { "id": "done", "stepType": "Notification", "name": "Tell the user",
      "parameters": { "Message": "Customer number ${var.CustomerNumber} copied" } },
    { "id": "none", "stepType": "Notification", "name": "Nothing found",
      "parameters": { "Message": "No customer number found in the CRM window" } }
  ],
  "flow": {
    "startNodes": [ "source" ],
    "transitions": [
      { "from": "source", "to": "find" }, { "from": "find", "to": "found" },
      { "from": "found", "to": "copy", "branch": "Yes" }, { "from": "found", "to": "none", "branch": "No" },
      { "from": "copy", "to": "done" }
    ]
  }
}
```

---

## For developers

- `Greenshot.Plugin.SmartText` depends only on Greenshot.Base. The OCR lines come from `OcrLines` (`Greenshot.Base/Interfaces/Ocr`),
  shared with the OCR step and the OCR destination: the lines found while selecting, else OCR runs and the lines are kept with the capture.
- `Detection/`: `TextLayout` (the OCR lines and word boxes as text with positions), `SmartTextDetector` (one pattern: look-alikes,
  label, area, normalize, check, action), `BuiltInDetectors`, `SmartTextScanner` (overlaps, duplicates, order), `SmartTextPatternCheck`
  (the checks of a pattern and of `openUrl`). The items are `SmartTextItem`, an `IDetectedFeature` kept with the capture like a barcode.
- `Capturing/`: `SmartTextCaptureOverlay` scans the text lines of the capture window while the text tool is active and keeps the
  `SmartTextItem` features up to date; `SmartTextHotspotTransformer` makes them numbered hotspots of the text tool (see
  docs/vNext/capture-tools.md, Hotspots).
- `SmartTextDefinitions`: the pattern files (`DefinitionStore`, approval like the commands) and the on/off setting per detector.
- Views: `SmartTextWindow` (picker), `SmartTextSettingsView`, `SmartTextPatternWindow`; texts in `Languages/greenshot.smarttext.*.ini`.
- Tests: `Greenshot.Tests/SmartText`.
