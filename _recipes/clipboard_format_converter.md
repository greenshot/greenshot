---
{
 "title": "Clipboard Format Converter & Watermark",
 "permalink": "/recipes/clipboard_format_converter/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Watches the clipboard: a copied image gets a small time stamp and opens in the editor.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "clipboard_format_converter",
  "version": "1.0.0",
  "name": "Clipboard Format Converter & Watermark",
  "summary": "Watches the clipboard: a copied image gets a small time stamp and opens in the editor.",
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
   "clipboard",
   "annotation",
   "editor"
  ],
  "sha256": "08cc23a2a479ae987dd348960124f7b7ee94121c513f3416a9adb20209d97e2c",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/clipboard_format_converter/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/clipboard_format_converter/README.md"
 }
}
---
{% raw %}
When another program copies an image to the clipboard, this recipe adds a small dark label with
**Greenshot: <time>** in the bottom right corner and opens the image in Greenshot's editor.

## Use it

Switch the recipe on in Settings > Recipes. It runs every time an image lands on the clipboard, until you switch it off.
{% endraw %}
