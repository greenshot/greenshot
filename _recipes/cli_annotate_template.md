---
{
 "title": "CLI Apply Template & Annotation",
 "permalink": "/recipes/cli_annotate_template/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Stamps a \"REVIEWED\" badge with the date on an image file and saves a copy next to it.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "cli_annotate_template",
  "version": "1.1.0",
  "name": "CLI Apply Template & Annotation",
  "summary": "Stamps a \"REVIEWED\" badge with the date on an image file and saves a copy next to it.",
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
   "annotation"
  ],
  "sha256": "172e3741b156735f639ecc6dc7f747e3e4734eee11b2df4a308e77106e3936fa",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/f6dacc4cbbdf2e8792befd147ccba60e4d3db45c/recipes/cli_annotate_template/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/f6dacc4cbbdf2e8792befd147ccba60e4d3db45c/recipes/cli_annotate_template/README.md"
 }
}
---
{% raw %}
Adds a dark badge with **REVIEWED: <today's date>** to the bottom right corner of an image file and saves the result
next to it as `<name>-with_annotation.png`. The new file name is printed, so scripts can pick it up.

## Use it

```
greenshot-cli -r annotate --file=C:\Pictures\example1.png
```

| Argument | Meaning |
|---|---|
| `file` | The image to stamp (required) |

## Allows

Reads the image you name and writes the new file in the same folder.
{% endraw %}
