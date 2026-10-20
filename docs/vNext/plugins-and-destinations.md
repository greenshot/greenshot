# Plugins and destinations: switching them on or off

Users can switch plugins and destinations off in the settings. An administrator can block plugins and destinations with
`greenshot-fixed.ini`, which users can't switch on again, and can also stop users from switching anything off themselves. The
lines for the file are built in the settings, nobody has to type the names.

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
   *Only allow what is checked* also blocks plugins and destinations which are added later. *Users can't switch off plugins or
   destinations themselves* adds empty `ExcludePluginNames=` and `ExcludeDestinations=` lines.
3. **Copy** the lines into `greenshot-fixed.ini` next to Greenshot.exe, or **Save as…** when there is no such file yet.

Greenshot then shows what the administrator blocked as unchecked and locked. Users can still switch off more, unless the
administrator turned that off.

## The values

All in the `[Core]` section, comma separated, not case sensitive:

| Key | What it does |
|---|---|
| `BlockedPluginNames` | Administrator: plugins which aren't loaded. The name is the part of the file name after `Greenshot.Plugin.`: `Box` for `Greenshot.Plugin.Box.dll`. Greenshot checks it before it loads the file. |
| `IncludePluginNames` | Administrator: when not empty, only these plugins are loaded. |
| `ExcludePluginNames` | User: plugins the user switched off, added to the blocked ones. Set in greenshot-fixed.ini (also empty), the user can't switch plugins off. |
| `BlockedDestinations` | Administrator: destinations which aren't available, by designation (e.g. `Jira`, `FileDialog`). |
| `IncludeDestinations` | Administrator: when not empty, only these destinations are available. |
| `ExcludeDestinations` | User: destinations the user switched off. Set in greenshot-fixed.ini (also empty), the user can't switch destinations off. |

The plugins Greenshot ships are `Box`, `Confluence`, `CustomUploader`, `Dropbox`, `ExternalCommand`, `Jira`, `Office`, `Pdf`,
`RecipeEditor` and `Zxing`.

The older `IncludePlugins` and `ExcludePlugins` named a plugin by its identifier, e.g. `Box Plugin`. In greenshot-fixed.ini they
still apply as a block by the administrator; in greenshot.ini Greenshot moves them to `IncludePluginNames` and `ExcludePluginNames` once.
An `ExcludeDestinations` list in an existing greenshot-fixed.ini keeps blocking those destinations, and stops users from switching off others.
