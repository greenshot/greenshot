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
"""Adds the recipe catalog to a checkout of the website (gh-pages) before Jekyll builds it.

  build_pages.py --index index.json --site <gh-pages checkout>

Writes:
  _data/recipes.json            the index, for the overview
  _recipes/index.html           the overview (/recipes/)
  _recipes/<id>.md              one page per recipe (/recipes/<id>/), from its README.md
  _layouts/recipe.html          the layout of a recipe page
  _config.recipes.yml           the recipes collection; build with --config _config.yml,_config.recipes.yml
  recipes/index.json            the index, byte for byte (Greenshot downloads it from here later)
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
    shutil.copyfile(os.path.join(HERE, "_config.recipes.yml"), os.path.join(site, "_config.recipes.yml"))

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
