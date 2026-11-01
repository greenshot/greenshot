---
{
 "title": "Region Snip with External Tool Optimization",
 "permalink": "/recipes/external_command_optimize/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Template: region capture that runs an external program on the file, then opens the editor.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "external_command_optimize",
  "version": "1.0.0",
  "name": "Region Snip with External Tool Optimization",
  "summary": "Template: region capture that runs an external program on the file, then opens the editor.",
  "kind": "recipe",
  "authors": [
   {
    "name": "Robin Krom",
    "github": "Lakritzator"
   }
  ],
  "license": "MIT",
  "minGreenshot": "1.4.0",
  "requires": [],
  "tags": [
   "after-capture",
   "external-command",
   "editor"
  ],
  "sha256": "d9a581779686064d825a1b67589199a52eba130d34ca8301d72d2fd2d9767dc5",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/external_command_optimize/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/external_command_optimize/README.md"
 }
}
---
{% raw %}
A starting point for running your own tool on a capture: it captures a region you select, runs a command with the
saved file, plays a sound and opens the result in the editor.

As shipped, the command only prints a line (`cmd.exe /c echo ...`), so it changes nothing. Replace `CommandLine` and
`Arguments` with the optimizer you use, e.g. `pngquant.exe` or `oxipng.exe`.

## Use it

- Tray menu: **Snip & Optimize Image**
- Hotkey: Ctrl + Shift + O, change it in Settings > General

## Options

| Option | Default |
|---|---|
| Hotkey | Ctrl + Shift + O |

## Allows

Runs `cmd.exe`. Greenshot asks before the first run.
{% endraw %}
