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
  "version": "1.0.1",
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
  "minGreenshot": "1.4.0",
  "requires": [],
  "tags": [
   "command-line",
   "files"
  ],
  "sha256": "ba0106864b24f9e0cf81d923db8d59a79f1fe751c4e026955037c9949c9cc1cb",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/196923352da5e3a61f9a2ddaad7468bb2f5a93bb/recipes/cli_convert_format/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/196923352da5e3a61f9a2ddaad7468bb2f5a93bb/recipes/cli_convert_format/README.md"
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
