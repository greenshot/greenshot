# Plugins and destinations: switching them on or off

Users can switch plugins and destinations off in the settings. An administrator can restrict them with `greenshot-fixed.ini`,
which users can't change, and can also stop users from switching anything off themselves. The lines for the file are built in
the settings, nobody has to type them by hand.

## In the settings

- **Plugins tab:** each plugin has a check box. The list shows the loaded plugins, the installed plugins which are switched off,
  and the names the settings mention. A plugin which isn't installed can be added by its name (see [the known names](#known-names)),
  e.g. to block it before it is installed. A change applies at the next start of Greenshot.
- **Destinations tab:** the *Available* column switches a destination on or off, a destination which is off isn't offered anywhere
  (tray menu, picker, editor, recipes). A destination which isn't there, e.g. of a plugin which isn't installed, can be added by its
  name too. The change applies when the settings are saved.

When the settings name an output destination which isn't there (e.g. its plugin is switched off), it stays in the settings and a
capture skips it; when none of them is there, the capture opens in the editor.

## For administrators: greenshot-fixed.ini

1. Check what you want to allow in the settings, add names which aren't installed when needed.
2. Click **Administrator settings…** (on the Plugins or the Destinations tab) and choose one of:
   - *Block what isn't checked*: writes the blocked plugins and destinations, everything else can be used.
   - *Allow only what is checked*: writes the allowed plugins and destinations, everything else is blocked, also plugins and
     destinations nobody knows yet.
   *Users can't switch off plugins or destinations themselves* adds empty `ExcludePluginNames=` and `ExcludeDestinations=` lines.
3. **Copy** the lines into `greenshot-fixed.ini` next to Greenshot.exe, or **Save as…** when there is no such file yet.

Greenshot then shows what the administrator blocked as unchecked and locked. Users can still switch off more, unless the
administrator turned that off.

## The values

All in the `[Core]` section, comma separated, not case sensitive:

| Key | Who | What it does |
|---|---|---|
| `BlockedPluginNames` | Administrator | Plugins which aren't loaded. Greenshot checks the name before it loads the file. |
| `AllowedPluginNames` | Administrator | When set, only these plugins are loaded, every other one is blocked. Set empty in greenshot-fixed.ini, no plugin is loaded. |
| `ExcludePluginNames` | User | Plugins the user switched off, on top of the blocked ones. Set in greenshot-fixed.ini (also empty), the user can't switch plugins off. |
| `BlockedDestinations` | Administrator | Destinations which aren't available. |
| `AllowedDestinations` | Administrator | When set, only these destinations are available, every other one is blocked. |
| `ExcludeDestinations` | User | Destinations the user switched off. Set in greenshot-fixed.ini (also empty), the user can't switch destinations off. |

The older `IncludePlugins` and `ExcludePlugins` named a plugin by its identifier, e.g. `Box Plugin`. In greenshot-fixed.ini they
still apply as an administrator restriction; in greenshot.ini Greenshot moves them to `AllowedPluginNames` and `ExcludePluginNames`
once. An `ExcludeDestinations` list in an existing greenshot-fixed.ini keeps blocking those destinations, and stops users from
switching off others.

## Known names

A plugin's name is the part of its file name after `Greenshot.Plugin.`: `Box` for `Greenshot.Plugin.Box.dll`. A destination of a
plugin is only available while the plugin is loaded, blocking the plugin blocks its destinations.

| Plugin | Destinations |
|---|---|
| (Greenshot itself) | `Editor`, `FileNoDialog` (save directly), `FileDialog` (save as), `Clipboard`, `Printer`, `EMail`, `Picker` (ask every time), `Windows10Share`, `Windows10OCR` |
| `Box` | `Box` |
| `Confluence` | `Confluence` |
| `CustomUploader` | `CustomUploader`, and `CustomUploader:<name>` per uploader file |
| `Dropbox` | `Dropbox` |
| `ExternalCommand` | `External <name>` per command, e.g. `External MS Paint` |
| `Jira` | `Jira` |
| `Office` | `Outlook`, `Word`, `Excel`, `Powerpoint`, `OneNote` |
| `Pdf` | `Pdf` |
| `RecipeEditor` | none |
| `Zxing` | `ZxingQrDestination` |
