# Plugins and destinations: switching them on or off

Which plugins Greenshot loads and which destinations it offers is set in the settings. An administrator can make the choice a
restriction that users can't change, with `greenshot-fixed.ini`, without typing the names by hand.

## In the settings

- **Plugins tab:** each plugin has a check box. The list shows the loaded plugins, the installed plugins which are switched off,
  and the plugins Greenshot ships which aren't installed, so they can be blocked before they are installed. A change applies at
  the next start of Greenshot.
- **Destinations tab:** the *Available* column switches a destination on or off. A destination which is off isn't offered anywhere
  (tray menu, picker, editor, recipes). The change applies when the settings are saved.

A destination of a plugin which isn't loaded can't be listed, its name is only known when the plugin is loaded. Switching off the
plugin blocks its destinations too. When the settings name an output destination which isn't there (e.g. its plugin is switched
off), it stays in the settings and a capture skips it; when none of them is there, the capture opens in the editor.

## For administrators: greenshot-fixed.ini

1. Check the plugins and destinations you want to allow in the settings.
2. Click **Administrator settings…** (on the Plugins or the Destinations tab). It shows the lines for `greenshot-fixed.ini`.
   *Only allow what is checked* also blocks plugins and destinations which are added later.
3. **Copy** the lines into `greenshot-fixed.ini` next to Greenshot.exe, or **Save as…** when there is no such file yet.

Greenshot then shows the check boxes as locked, with the note that the administrator set them.

## The values

All in the `[Core]` section, comma separated, not case sensitive:

| Key | What it does |
|---|---|
| `ExcludePluginNames` | Plugins which aren't loaded. The name is the part of the file name after `Greenshot.Plugin.`: `Box` for `Greenshot.Plugin.Box.dll`. Greenshot checks it before it loads the file. |
| `IncludePluginNames` | When not empty, only these plugins are loaded. |
| `ExcludeDestinations` | Destinations which aren't available, by designation (e.g. `Jira`, `FileDialog`). |
| `IncludeDestinations` | When not empty, only these destinations are available. |

The plugins Greenshot ships are `Box`, `Confluence`, `CustomUploader`, `Dropbox`, `ExternalCommand`, `Jira`, `Office`, `Pdf`,
`RecipeEditor` and `Zxing`.

The older `IncludePlugins` and `ExcludePlugins` named a plugin by its identifier, e.g. `Box Plugin`. In greenshot-fixed.ini they
still apply; in greenshot.ini Greenshot moves them to `IncludePluginNames` and `ExcludePluginNames` once.
