#!/usr/bin/env python3
# Greenshot - a free and open source screenshot tool
# Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <https://www.gnu.org/licenses/>.
"""Writes the recipe catalog into a checkout of the website (gh-pages), for GitHub's classic Pages build.

  build_pages.py --index index.json --site <gh-pages checkout>

Writes (the workflow commits them to gh-pages):
  _data/recipes.json            the index, for the overview
  _recipes/index.html           the overview (/recipes/)
  _recipes/<id>.md              one page per recipe (/recipes/<id>/), from its README.md; pages of recipes no longer
                                in the index are removed
  _layouts/recipe.html          the layout of a recipe page
  css/recipes.css               the styles of the recipe pages
  recipes/index.json            the index, byte for byte (Greenshot reads the signed index of the catalog release instead)
and adds the "recipes" collection to _config.yml once. A collection, not pages: pages are listed on /sitemap/.
"""
import argparse
import json
import os
import re
import shutil
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]*$")


def fetch(url):
    with urllib.request.urlopen(url, timeout=30) as response:
        return response.read().decode("utf-8")


def readme_body(readme, base_url):
    """The README without its first heading (the page shows the name), relative links pointing to the recipe folder."""
    lines = readme.splitlines()
    if lines and lines[0].startswith("# "):
        lines = lines[1:]
    body = "\n".join(lines).strip() + "\n"
    body = re.sub(r"\]\((?!https?:|#|/)([^)\s]+)\)", lambda match: f"]({base_url}/{match.group(1)})", body)
    # The README is text of the recipe's author, not a template
    return "{% raw %}\n" + body + "{% endraw %}\n"


COLLECTION = """  recipes:
    output: true
"""


def add_collection(config_path):
    """Adds the recipes collection under 'collections:' of the site's _config.yml, if it isn't there yet."""
    with open(config_path, encoding="utf-8", newline="") as file:
        config = file.read()
    if re.search(r"^  recipes:", config, re.MULTILINE):
        return
    match = re.search(r"^collections:[^\n]*\n", config, re.MULTILINE)
    if not match:
        raise SystemExit("_config.yml has no 'collections:' section")
    newline = "\r\n" if "\r\n" in config else "\n"
    config = config[:match.end()] + COLLECTION.replace("\n", newline) + config[match.end():]
    with open(config_path, "w", encoding="utf-8", newline="") as file:
        file.write(config)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--index", required=True, help="index.json of a greenshot-recipes release")
    parser.add_argument("--site", required=True, help="the website checkout")
    arguments = parser.parse_args()

    with open(arguments.index, "rb") as file:
        index_bytes = file.read()
    index = json.loads(index_bytes)
    site = arguments.site

    for folder in ("_data", "_recipes", "_layouts", "recipes"):
        os.makedirs(os.path.join(site, folder), exist_ok=True)
    with open(os.path.join(site, "recipes", "index.json"), "wb") as file:
        file.write(index_bytes)
    with open(os.path.join(site, "_data", "recipes.json"), "w", encoding="utf-8") as file:
        json.dump(index, file, ensure_ascii=False)
    shutil.copyfile(os.path.join(HERE, "index.html"), os.path.join(site, "_recipes", "index.html"))
    shutil.copyfile(os.path.join(HERE, "recipe.html"), os.path.join(site, "_layouts", "recipe.html"))
    shutil.copyfile(os.path.join(HERE, "recipes.css"), os.path.join(site, "css", "recipes.css"))
    add_collection(os.path.join(site, "_config.yml"))
    for name in os.listdir(os.path.join(site, "_recipes")):
        if name.endswith(".md"):
            os.remove(os.path.join(site, "_recipes", name))

    for recipe in index["recipes"]:
        recipe_id = recipe["id"]
        if not ID_PATTERN.match(recipe_id):
            raise SystemExit(f"unexpected recipe id {recipe_id!r}")
        readme = fetch(recipe["readme"])
        base_url = recipe["readme"].rsplit("/", 1)[0]
        front_matter = {
            "title": recipe["name"],
            "permalink": f"/recipes/{recipe_id}/",
            "excerpt_separator": "",
            "layout": "recipe",
            "sitemap": False,
            "metarobots": "noindex,nofollow",
            "metadescription": recipe["summary"],
            "repository": index["repository"],
            "recipe": recipe,
        }
        with open(os.path.join(site, "_recipes", f"{recipe_id}.md"), "w", encoding="utf-8", newline="\n") as file:
            # JSON is valid YAML, so the front matter can't be broken by the texts in it
            file.write("---\n" + json.dumps(front_matter, ensure_ascii=False, indent=1) + "\n---\n")
            file.write(readme_body(readme, base_url))
    print(f"{len(index['recipes'])} recipe page(s) written")


if __name__ == "__main__":
    main()
