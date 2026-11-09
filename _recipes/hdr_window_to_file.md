---
{
 "title": "HDR Window to File",
 "permalink": "/recipes/hdr_window_to_file/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Capture the active window on an HDR display and save it as a lossless HDR file (JPEG XR), highlights included.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "hdr_window_to_file",
  "version": "1.1.0",
  "name": "HDR Window to File",
  "summary": "Capture the active window on an HDR display and save it as a lossless HDR file (JPEG XR), highlights included.",
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
   "hdr",
   "file"
  ],
  "sha256": "c30ea534fb7eb086a6ca1c3892d9164f31e7a9c159d3cab228c4185f714b50f7",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/5cbb67e3753556761ba67631168cca04913b6164/recipes/hdr_window_to_file/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/5cbb67e3753556761ba67631168cca04913b6164/recipes/hdr_window_to_file/README.md"
 }
}
---
{% raw %}
Capture the active window on an HDR display and save it as an HDR file: JPEG XR, lossless, with the highlights as they
were on the screen. JPEG XR is built into Windows and is the format the Xbox Game Bar uses for HDR screenshots.

Without HDR content a normal PNG is saved, and the notification says so. That happens when the window is on an SDR
display, or when nothing in it is brighter than SDR white or has colors outside sRGB.

## Use it

- Hotkey: Ctrl + Alt + PrintScreen, change it in the recipe's settings
- Right-click context menu

## Options

| Option | Default |
|---|---|
| Hotkey | Ctrl + Alt + PrintScreen |
| Ask where to save the HDR file | off: saves to your output folder |

## Allows

Writes the file to your output folder, or where you choose.
{% endraw %}
