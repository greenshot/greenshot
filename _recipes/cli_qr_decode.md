---
{
 "title": "CLI QR Code & Barcode Extractor",
 "permalink": "/recipes/cli_qr_decode/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Reads QR codes and barcodes in an image file and prints their text.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "cli_qr_decode",
  "version": "1.1.0",
  "name": "CLI QR Code & Barcode Extractor",
  "summary": "Reads QR codes and barcodes in an image file and prints their text.",
  "kind": "recipe",
  "authors": [
   {
    "name": "Robin Krom",
    "github": "Lakritzator"
   }
  ],
  "license": "MIT",
  "minGreenshot": "1.4.465",
  "requires": [
   {
    "id": "Greenshot.Plugin.Zxing",
    "name": "ZXing Barcode Plugin",
    "minVersion": "1.3.0"
   }
  ],
  "tags": [
   "command-line",
   "qr-code"
  ],
  "sha256": "a2972b6445bb1316a535f01369d18a64dec0eb6a46bd1cde77fbec86419cf21b",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/d7a6386242af4cde3a261ceb182ceeeed43f3ee4/recipes/cli_qr_decode/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/d7a6386242af4cde3a261ceb182ceeeed43f3ee4/recipes/cli_qr_decode/README.md"
 }
}
---
{% raw %}
Loads an image file, finds QR codes and barcodes in it and prints what they say, so a script can use it.

## Needs

The ZXing plugin (QR codes and barcodes), which comes with Greenshot.

## Use it

```
greenshot-cli -r qr --file=C:\Pictures\invoice.png
```

| Argument | Meaning |
|---|---|
| `file` | The image with the code (required) |

## Allows

Reads the image you name.
{% endraw %}
