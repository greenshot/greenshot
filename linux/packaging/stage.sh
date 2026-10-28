#!/usr/bin/env bash
# Builds Greenshot for Linux and puts its files where an installation or a package needs them:
#   <prefix>/<libdir>/greenshot          the program (greenshot, greenshot-mcp and their libraries)
#   <prefix>/bin/greenshot               the command
#   <prefix>/share/applications          the desktop files (the app, the greenshot: links)
#   <prefix>/share/metainfo              the AppStream data (software centers, Flathub)
#   <prefix>/share/icons/hicolor         the icon
# Usage: stage.sh --prefix /usr [--destdir DIR] [--libdir lib64] [--rid linux-x64] [--self-contained] [--nuget-source DIR]
# --nuget-source restores from a folder only, for builds without network (Flatpak)
# --destdir is the staging root of a package build (RPM's buildroot, makepkg's pkgdir), the program is linked relatively.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/.." && pwd)"
prefix=/usr/local
destdir=
libdir=lib
rid=
self_contained=false
restore=()

while [[ $# -gt 0 ]]; do
    case "$1" in
        --prefix) prefix="$2"; shift 2 ;;
        --destdir) destdir="$2"; shift 2 ;;
        --libdir) libdir="$2"; shift 2 ;;
        --rid) rid="$2"; shift 2 ;;
        --self-contained) self_contained=true; shift ;;
        --nuget-source) restore=(--source "$2"); shift 2 ;;
        *) echo "Unknown option $1" >&2; exit 2 ;;
    esac
done

if [[ -z "$rid" ]]; then
    case "$(uname -m)" in
        aarch64|arm64) rid=linux-arm64 ;;
        *) rid=linux-x64 ;;
    esac
fi

app="$destdir$prefix/$libdir/greenshot"
share="$destdir$prefix/share"
id=org.getgreenshot.Greenshot

dotnet publish "$root/src/Greenshot.Linux/Greenshot.Linux.csproj" -c Release -r "$rid" --self-contained "$self_contained" "${restore[@]}" \
    -p:DebugType=none -p:DebugSymbols=false -o "$app"

install -d "$destdir$prefix/bin" "$share/applications" "$share/metainfo" "$share/icons/hicolor/scalable/apps"
ln -sfn "../$libdir/greenshot/greenshot" "$destdir$prefix/bin/greenshot"
install -m 644 "$here/$id.desktop" "$here/$id.Url.desktop" "$share/applications/"
install -m 644 "$here/$id.metainfo.xml" "$share/metainfo/"
install -m 644 "$root/src/Greenshot.Linux/Assets/greenshot.svg" "$share/icons/hicolor/scalable/apps/$id.svg"
for size in 16 32 48 64 256; do
    install -d "$share/icons/hicolor/${size}x${size}/apps"
    install -m 644 "$root/src/Greenshot.Linux/Assets/greenshot-$size.png" "$share/icons/hicolor/${size}x${size}/apps/$id.png"
done
