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
  "version": "1.0.0",
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
  "sha256": "021e31785b382a7e84fa813382b43625e39d5224b1a39efc3c380e5216092b3c",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/cli_annotate_template/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/cli_annotate_template/README.md"
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
