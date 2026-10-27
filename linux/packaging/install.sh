#!/usr/bin/env bash
# Installs Greenshot for the current user: the program to ~/.local/lib/greenshot, the command to ~/.local/bin,
# the menu entry and the icon. With --autostart Greenshot also starts with the session.
# Run it from anywhere: linux/packaging/install.sh [--autostart]
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/.." && pwd)"
data="${XDG_DATA_HOME:-$HOME/.local/share}"
config="${XDG_CONFIG_HOME:-$HOME/.config}"
lib="$HOME/.local/lib/greenshot"
bin="$HOME/.local/bin"

for tool in grim wl-copy; do
    command -v "$tool" >/dev/null || echo "Note: $tool is not installed (packages: grim, wl-clipboard), see linux/README.md"
done

dotnet publish "$root/src/Greenshot.Linux/Greenshot.Linux.csproj" -c Release -r linux-x64 --self-contained false -o "$lib"

mkdir -p "$bin" "$data/applications" "$data/icons/hicolor/scalable/apps"
ln -sf "$lib/greenshot" "$bin/greenshot"
install -m 644 "$here/greenshot.desktop" "$data/applications/greenshot.desktop"
install -m 644 "$here/greenshot-url.desktop" "$data/applications/greenshot-url.desktop"
command -v xdg-mime >/dev/null && xdg-mime default greenshot-url.desktop x-scheme-handler/greenshot || true
install -m 644 "$root/src/Greenshot.Linux/Assets/greenshot.svg" "$data/icons/hicolor/scalable/apps/greenshot.svg"
for size in 16 32 48 64 256; do
    mkdir -p "$data/icons/hicolor/${size}x${size}/apps"
    install -m 644 "$root/src/Greenshot.Linux/Assets/greenshot-$size.png" "$data/icons/hicolor/${size}x${size}/apps/greenshot.png"
done

if [[ "${1:-}" == "--autostart" ]]; then
    mkdir -p "$config/autostart"
    install -m 644 "$here/greenshot.desktop" "$config/autostart/greenshot.desktop"
    echo "Greenshot starts with your session"
fi

command -v update-desktop-database >/dev/null && update-desktop-database "$data/applications" || true
echo "Installed. Start it with: greenshot"
