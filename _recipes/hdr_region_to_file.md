---
{
 "title": "HDR Region to File",
 "permalink": "/recipes/hdr_region_to_file/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Select a region on the live screen of an HDR display and save it as a lossless HDR file (JPEG XR), highlights included.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "hdr_region_to_file",
  "version": "1.1.0",
  "name": "HDR Region to File",
  "summary": "Select a region on the live screen of an HDR display and save it as a lossless HDR file (JPEG XR), highlights included.",
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
  "sha256": "3c955a4e0559b4798731df168ade368fff760a236ba05397184bbda0e1dd2769",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/f6dacc4cbbdf2e8792befd147ccba60e4d3db45c/recipes/hdr_region_to_file/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/f6dacc4cbbdf2e8792befd147ccba60e4d3db45c/recipes/hdr_region_to_file/README.md"
 }
}
---
{% raw %}
Select a region on the live screen of an HDR display and save it as an HDR file: JPEG XR, lossless, with the
highlights as they were on the screen. JPEG XR is built into Windows and is the format the Xbox Game Bar uses for HDR
screenshots.

The region is selected on the live screen, so an HDR video keeps playing while you select.

Without HDR content a normal PNG is saved, and the notification says so. That happens when the region is on an SDR
display, or when nothing in it is brighter than SDR white or has colors outside sRGB (for example a normal window).

## Use it

- Hotkey: Ctrl + Shift + PrintScreen, change it in the recipe's settings
- Right-click context menu

## Options

| Option | Default |
|---|---|
| Hotkey | Ctrl + Shift + PrintScreen |
| Ask where to save the HDR file | off: saves to your output folder |
| Open in the editor instead | off: draw on the capture, then Save as JPEG XR keeps the highlights with your drawing on top |
| Ask how to save an HDR capture | off: with HDR content, asks whether to save it as HDR file (JPEG XR) or PNG |

## Allows

Writes the file to your output folder, or where you choose.
{% endraw %}
