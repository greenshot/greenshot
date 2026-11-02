---
{
 "title": "Region with fixed width",
 "permalink": "/recipes/region_fixed_width/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Region capture scaled down to a maximum width, smaller images filled up, with an optional border.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "region_fixed_width",
  "version": "1.0.0",
  "name": "Region with fixed width",
  "summary": "Region capture scaled down to a maximum width, smaller images filled up, with an optional border.",
  "kind": "recipe",
  "authors": [
   {
    "name": "Christian Schulz",
    "github": "Christian-Schulz"
   }
  ],
  "license": "MIT",
  "minGreenshot": "1.4.0",
  "requires": [],
  "tags": [
   "after-capture",
   "resize"
  ],
  "sha256": "1473416ad46a173805dc5346fcf8a7e0445c765960cbf0b3e345d22eade068d2",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/1c5ed6401282c8307c7fd9197bf9f009bfe350d4/recipes/region_fixed_width/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/1c5ed6401282c8307c7fd9197bf9f009bfe350d4/recipes/region_fixed_width/README.md"
 }
}
---
{% raw %}
Captures a region you select and gives every capture the same width: wider images are scaled down to the maximum
width, smaller images are placed on a canvas of that width. A border is added if you like. The result goes to your
export destinations.

Handy for documentation and tickets, where all screenshots should look the same width.

## Use it

- Hotkey: Ctrl + Alt + F, change it in Greenshot's Settings > General (hotkeys) or in the recipe editor
- Right-click context menu

## Options

| Option | Default |
|---|---|
| Hotkey | Ctrl + Alt + F |
| Max width (px) | 800 |
| Add border | on |
| Border thickness (px) | 2 |
| Border color | black |
| Fill up smaller images | on |
| Canvas color | gray (#888888) |

The border is counted into the width: with a 2 px border the image is scaled to the max width minus 4 px, so the final
image is exactly as wide as the max width.

## Allows

Nothing beyond the capture itself and your export destinations.
{% endraw %}
