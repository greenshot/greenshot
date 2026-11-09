---
{
 "title": "CLI Image Format Converter",
 "permalink": "/recipes/cli_convert_format/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Converts an image file to another format (png, jpg, webp, bmp, gif, tiff) from the command line.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "cli_convert_format",
  "version": "1.1.1",
  "name": "CLI Image Format Converter",
  "summary": "Converts an image file to another format (png, jpg, webp, bmp, gif, tiff) from the command line.",
  "kind": "recipe",
  "authors": [
   {
    "name": "Robin Krom",
    "github": "Lakritzator"
   }
  ],
  "license": "MIT",
  "minGreenshot": "1.4.465",
  "requires": [],
  "tags": [
   "command-line",
   "files"
  ],
  "sha256": "8e50a0082ed6e8fc6a83def329e4867ec2b027f842f1463e67406e0cd93e5968",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/5cbb67e3753556761ba67631168cca04913b6164/recipes/cli_convert_format/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/5cbb67e3753556761ba67631168cca04913b6164/recipes/cli_convert_format/README.md"
 }
}
---
{% raw %}
Loads an image file, converts it to the format you ask for and saves it next to the original with the new extension.
The new file name is printed.

## Use it

```
greenshot-cli -r convert --file=C:\Pictures\photo.png --format=webp
```

| Argument | Meaning |
|---|---|
| `file` | The image to convert (required) |
| `format` | `png`, `jpg`, `webp`, `bmp`, `gif` or `tiff` (default `jpg`) |

## Allows

Reads the image you name and writes the converted file in the same folder.
{% endraw %}
