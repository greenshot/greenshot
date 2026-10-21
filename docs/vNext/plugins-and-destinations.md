# Plugins and destinations: switching them on or off

Users can switch plugins and destinations off in the settings. An administrator can restrict them with `greenshot-fixed.ini`,
which users can't change, and can also stop users from switching anything off themselves. The lines for the file are built in
the settings, nobody has to type them by hand.

## In the settings

- **Plugins tab:** each plugin has a check box. The list shows the loaded plugins, the installed plugins which are switched off,
  and the names the settings mention. A change applies at the next start of Greenshot. Each loaded plugin shows who signed it,
  in green with a check mark when it is the certificate of Greenshot.exe; see [plugin-loading.md](plugin-loading.md) for the
  signature check.
- **Destinations tab:** the *Available* column switches a destination on or off, a destination which is off isn't offered anywhere
  (tray menu, picker, editor, recipes). The change applies when the settings are saved.

When the settings name an output destination which isn't there (e.g. its plugin is switched off), it stays in the settings and a
capture skips it; when none of them is there, the capture opens in the editor.

## For administrators: greenshot-fixed.ini

1. Check what you want to allow in the settings.
2. Click **Block plugins…** on the Plugins tab or **Block destinations…** on the Destinations tab (both open the same window) and choose one of:
   - *Block what isn't checked*: writes the blocked plugins and destinations, everything else can be used.
   - *Allow only what is checked*: writes the allowed plugins and destinations, everything else is blocked, also plugins and
     destinations nobody knows yet.
   *Users can't switch off plugins or destinations themselves* adds empty `ExcludePluginNames=` and `ExcludeDestinations=` lines.

   A plugin or destination which isn't installed yet can be added there by its name (see [the known names](#known-names)) with
   **Add plugin** or **Add destination**: it is blocked, or allowed with *Allow only what is checked*.
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
| `Atlassian` | `Jira`, `Confluence` |
| `Box` | `Box` |
| `CustomUploader` | `CustomUploader`, and `CustomUploader:<name>` per uploader file |
| `Dropbox` | `Dropbox` |
| `ExternalCommand` | `External <name>` per command, e.g. `External MS Paint` |
| `Office` | `Outlook`, `Word`, `Excel`, `Powerpoint`, `OneNote` |
| `Pdf` | `Pdf` |
| `RecipeEditor` | none |
| `Zxing` | `ZxingQrDestination` |

Jira and Confluence were two plugins until 1.4 and are one now, `Atlassian`. In the plugin lists their old names still work and
stand for their destination: `Jira` in `BlockedPluginNames` or `ExcludePluginNames` blocks only the Jira destination,
`Confluence` in `AllowedPluginNames` loads the plugin with only the Confluence destination. Both old names block the whole plugin.

**A file name that is already used.** A Confluence page has one attachment per file name, with versions. When the page already
has a file with the name, the Confluence destination asks: add the capture as a new version of that attachment, or upload it
with another name (`name (2).png` is suggested). The Confluence recipe step can't ask, it always adds a new version, so a recipe
with a fixed file name keeps one attachment on the page with every capture as a version. Jira has no attachment versions: an issue
can have several attachments with the same name, so a capture with a used name is added as another attachment.
