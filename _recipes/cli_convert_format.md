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
  "version": "1.1.0",
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
  "sha256": "323deeb5c8b71ed90a5c5dc859da179d3e20f6580506917eb2d0b40da26ac0ce",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/d7a6386242af4cde3a261ceb182ceeeed43f3ee4/recipes/cli_convert_format/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/d7a6386242af4cde3a261ceb182ceeeed43f3ee4/recipes/cli_convert_format/README.md"
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
