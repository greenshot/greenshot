---
{
 "title": "QR Codes Showcase (Payments, Business Cards & Links)",
 "permalink": "/recipes/qr_codes_showcase/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Region capture with three example QR codes: a SEPA payment, a business card and a link.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "qr_codes_showcase",
  "version": "1.0.0",
  "name": "QR Codes Showcase (Payments, Business Cards & Links)",
  "summary": "Region capture with three example QR codes: a SEPA payment, a business card and a link.",
  "kind": "recipe",
  "authors": [
   {
    "name": "Robin Krom",
    "github": "Lakritzator"
   }
  ],
  "license": "MIT",
  "minGreenshot": "1.4.0",
  "requires": [
   {
    "id": "Greenshot.Plugin.Zxing",
    "name": "ZXing Barcode & QR Code Extension",
    "minVersion": "1.3.0",
    "url": "https://getgreenshot.org/plugins/zxing"
   }
  ],
  "tags": [
   "after-capture",
   "qr-code",
   "annotation"
  ],
  "sha256": "d99e0f67634b0c90e6d4971b44f31e5f4a4c8861a0dc6abf349c87c7e495ebce",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/qr_codes_showcase/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/qr_codes_showcase/README.md"
 }
}
---
{% raw %}
Captures a region you select and puts three QR codes on it: an EPC (SEPA) bank transfer, a vCard business card and a
link with rounded dots, then opens it in the editor. Change the texts in the recipe to your own data.

## Needs

The ZXing plugin (QR codes and barcodes), which comes with Greenshot.

## Use it

- Tray menu: **QR Codes Showcase (Payments & Business Cards)**
- Hotkey: Ctrl + Shift + Q, change it in Settings > General

## Options

| Option | Default |
|---|---|
| Hotkey | Ctrl + Shift + Q |
{% endraw %}
