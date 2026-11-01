---
{
 "title": "DAG Workflow with environment watermark and parallel feedback",
 "permalink": "/recipes/dag_watermark_branching/",
 "excerpt_separator": "",
 "layout": "recipe",
 "sitemap": false,
 "metarobots": "noindex,nofollow",
 "metadescription": "Region capture with a watermark of user, machine and time, plus a sound while it works.",
 "repository": "greenshot/greenshot-recipes",
 "recipe": {
  "id": "dag_watermark_branching",
  "version": "1.0.0",
  "name": "DAG Workflow with environment watermark and parallel feedback",
  "summary": "Region capture with a watermark of user, machine and time, plus a sound while it works.",
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
   "after-capture",
   "annotation"
  ],
  "sha256": "898ea7ebc163c2f2c832e6844ce0ead8ab27cc8676e8a886c07fe94ba636b5fb",
  "url": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/dag_watermark_branching/recipe.gsrecipe.json",
  "readme": "https://raw.githubusercontent.com/greenshot/greenshot-recipes/a78999f7f7fc36635f63139cdf35dde6bea40bd2/recipes/dag_watermark_branching/README.md"
 }
}
---
{% raw %}
Captures a region you select, plays a short sound and at the same time stamps a watermark with your user name,
the computer name and the time on the capture, then opens it in the editor and copies it to the clipboard.

It is also a small example of branches in a recipe: the sound and the watermark run side by side, and a variable is set
on the way.

## Use it

- Tray menu: **DAG Environment Watermark Capture**
- Hotkey: Ctrl + Shift + W, change it in Settings > General

## Options

| Option | Default |
|---|---|
| Hotkey | Ctrl + Shift + W |
{% endraw %}
