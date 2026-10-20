# How Greenshot loads plugins

What happens with a plugin from the moment Greenshot finds its file until it runs, and which settings decide on the way.
Greenshot Light has no plugins. Which plugins and destinations are switched on or off is described in
[plugins-and-destinations.md](plugins-and-destinations.md).

## Overview

```mermaid
flowchart TD
    S["Greenshot starts, greenshot.ini<br/>(with defaults and greenshot-fixed.ini) is read"] --> F["Find Greenshot.Plugin.*.dll<br/>(application folder, App\Greenshot for the portable version)"]
    F --> R{"Allowed by the settings?<br/>Blocked / Allowed / Exclude"}
    R -- no --> X["Not loaded"]
    R -- yes --> G{"Signature check"}
    G -- load --> L["Load the DLL, create the plugin"]
    G -- skip --> X
    G -- hold back --> H["Not loaded, asked after the start"]
    L --> C["ConfigureServices: settings sections,<br/>destinations, settings views"]
    C --> D["Register what needs the settings<br/>(recipe steps when recipes are on)"]
    D --> T["StartAsync, with a time limit"]
    T --> RUN["Plugin runs"]
    H --> Q["Question after the tray icon is shown"]
```

1. **Find:** every `Greenshot.Plugin.*.dll` in the application folder and its subfolders (the installer puts each plugin in
   `Plugins\<name>\`). The plugin's name is the part of the file name after `Greenshot.Plugin.`: `Box` for `Greenshot.Plugin.Box.dll`.
2. **Settings:** `BlockedPluginNames`, `AllowedPluginNames` and `ExcludePluginNames` decide by that name, before the file is read,
   see [plugins-and-destinations.md](plugins-and-destinations.md).
3. **Signature check:** see below.
4. **Load and register:** the DLL is loaded and the plugin registers its settings sections (filled from the already read
   greenshot.ini), destinations, settings views and recipe steps.
5. **Start:** each plugin starts with a time limit; a plugin which fails is logged.

## The signature check

Release builds of Greenshot and its plugins are signed with the Greenshot code signing certificate. A plugin loads when it is
signed with the same certificate as Greenshot.exe; any other plugin (not signed, a broken signature, another signer) is held back
and the user is asked. Builds from Visual Studio aren't signed: when Greenshot.exe has no valid signature the check is off.

```mermaid
flowchart TD
    A["Greenshot.Plugin.*.dll allowed by the settings"] --> C{"Greenshot.exe signed?<br/>(once per start)"}
    C -- "no (Visual Studio build)" --> LOAD["Load plugin"]
    C -- yes --> D["SHA-256 of the DLL"]
    D --> E{"Hash in PluginVerifiedHashes?"}
    E -- yes --> LOAD
    E -- no --> F{"Hash in PluginApprovedHashes?<br/>(user or greenshot-fixed.ini)"}
    F -- yes --> LOAD
    F -- no --> G{"Hash in PluginRejectedHashes?"}
    G -- yes --> SKIPLOG["Not loaded, log line"]
    G -- no --> H{"Valid signature,<br/>same certificate as Greenshot.exe?"}
    H -- yes --> V["Add hash to PluginVerifiedHashes"] --> LOAD
    H -- no --> I{"AskForUnsignedPlugins?"}
    I -- "False" --> SKIPLOG
    I -- "True" --> Q["Held back, asked after the tray icon is shown"]
    Q -- Load --> AP["Hash added to PluginApprovedHashes<br/>loads at the next start"]
    Q -- "Don't load" --> RJ["Hash added to PluginRejectedHashes"]
    Q -- closed --> AGAIN["Asked again at the next start"]
```

- **Cost:** checking a signature takes about 6 ms per file (revocation isn't checked, that can go online and take 200 ms or more),
  a SHA-256 about 0.1-0.3 ms. A plugin whose hash passed the check once isn't checked again until it changes, so a normal start
  only computes the hashes. `PluginVerifiedHashes` keeps the hashes of the plugins found at the last start.
- **The question** shows the plugin's name, folder, signer and SHA-256. *Load* approves exactly this file: an update has a new hash
  and is checked again. The plugin loads at the next start, loading it later would miss the registration of its settings and
  destinations.
- **Only the plugin DLL** is checked, not the libraries in its folder (some are signed by others, e.g. Microsoft).
- **Limits:** the lists are in greenshot.ini, which every program of the user can change. The check protects against a plugin
  which was placed in a folder the user can write to (portable or per-user installation); an all-users installation keeps the
  plugins under Program Files, where only administrators can write.

## For administrators

| Key | What it does |
|---|---|
| `PluginApprovedHashes` | SHA-256 of plugins which aren't signed like Greenshot.exe but may load. In greenshot-fixed.ini, users can't approve others. |
| `PluginRejectedHashes` | SHA-256 of plugins the user chose not to load. |
| `PluginVerifiedHashes` | Kept by Greenshot: hashes which passed the signature check. |
| `AskForUnsignedPlugins` | `False` in greenshot-fixed.ini: no question, a plugin which isn't signed like Greenshot.exe only loads when `PluginApprovedHashes` lists it. |

**Administrator settings…** in the settings (Plugins or Destinations tab) adds the hashes approved on this PC to the lines for
greenshot-fixed.ini, and *Don't ask about plugins which aren't signed* adds `AskForUnsignedPlugins=False`.
