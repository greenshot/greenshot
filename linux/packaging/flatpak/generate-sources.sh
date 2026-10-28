#!/usr/bin/env bash
# Lists the NuGet packages Greenshot needs in nuget-sources.json next to the manifest: flatpak-builder downloads them before
# the build, which has no network. Uses flatpak-dotnet-generator.py of flatpak-builder-tools (downloaded when missing).
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/../.." && pwd)"
generator="${FLATPAK_DOTNET_GENERATOR:-$here/flatpak-dotnet-generator.py}"
if [[ ! -f "$generator" ]]; then
    curl -sSLo "$generator" https://raw.githubusercontent.com/flatpak/flatpak-builder-tools/master/dotnet/flatpak-dotnet-generator.py
fi

python3 "$generator" --dotnet 10 --freedesktop 26.08 --runtime linux-x64 --runtime linux-arm64 \
    "$here/nuget-sources.json" "$root/src/Greenshot.Linux/Greenshot.Linux.csproj"
