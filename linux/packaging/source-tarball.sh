#!/usr/bin/env bash
# The source of Greenshot for Linux as greenshot-<version>.tar.gz: linux/ and what it uses of the Windows version
# (the translations, the emoji font and list, the recipe schema for greenshot-mcp). For the RPM and AUR packages.
# Usage: source-tarball.sh [git ref, default HEAD] [output directory, default .]; --working-tree packs the files as they are
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/../.." && pwd)"
ref=HEAD
working_tree=false
if [[ "${1:-}" == "--working-tree" ]]; then
    working_tree=true
    shift
elif [[ $# -gt 0 ]]; then
    ref="$1"
    shift
fi
out="$(cd "${1:-.}" && pwd)"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$repo/linux/Directory.Build.props")"
name="greenshot-$version"

cd "$repo"
paths=(linux LICENSE nuget.config docs/vNext/recipe.schema.json docs/vNext/examples src/Greenshot/Languages
       src/Greenshot.Editor/Controls/Emoji/emoji-test.txt src/Greenshot.Editor/Drawing/Emoji/Twemoji.Mozilla.ttf)
for languages in src/Greenshot.Plugin.*/Languages; do
    paths+=("$languages")
done

if $working_tree; then
    tar --transform "s,^,$name/," --exclude='bin' --exclude='obj' -czf "$out/$name.tar.gz" "${paths[@]}"
else
    git archive --format=tar.gz --prefix="$name/" -o "$out/$name.tar.gz" "$ref" -- "${paths[@]}"
fi
echo "$out/$name.tar.gz"
