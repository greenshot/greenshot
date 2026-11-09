---
{
 "title": "Region Snip with Your Own Tool",
 "permalink": "/recipes/external_command_run_tool/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Template: region capture that runs a program of your choice on the file, then opens the editor.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "external_command_run_tool",
  "version": "1.0.0",
  "name": "Region Snip with Your Own Tool",
  "summary": "Template: region capture that runs a program of your choice on the file, then opens the editor.",
  "kind": "recipe",
  "authors": [
   {
    "name": "Robin Krom",
    "github": "Lakritzator"
   }
  ],
  "license": "MIT",
  "minGreenshot": "1.4.466",
  "requires": [],
  "tags": [
   "after-capture",
   "external-command",
   "editor"
  ],
  "sha256": "d0d9ab2e6a89c51e862ae84637de916b488ae9b982a4b35952da44aa9ab9f376",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/5cbb67e3753556761ba67631168cca04913b6164/recipes/external_command_run_tool/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/5cbb67e3753556761ba67631168cca04913b6164/recipes/external_command_run_tool/README.md"
 }
}
---
{% raw %}
A starting point for running a program of your own on a capture: it captures a region you select, runs a command with the
saved file, plays a sound and opens the result in the editor.

As shipped, the command only prints a line (`cmd.exe /c echo ...`), so it changes nothing; the line is in
`${var.tool.output}`. Replace `Program` and `Arguments` with the tool you use, e.g. ImageMagick to add a border
(`magick.exe`, `mogrify -border 20 "${export.file}"`), and set `Output` to `File`: the image is then read again from the
file the tool changed. Greenshot already writes optimized PNG files, an optimizer isn't needed.

## Use it

- Tray menu: **Snip & Run My Tool**
- Hotkey: Ctrl + Shift + O, change it in Settings > General

## Options

| Option | Default |
|---|---|
| Hotkey | Ctrl + Shift + O |

## Allows

Runs `cmd.exe`. Greenshot asks before the first run.
{% endraw %}
