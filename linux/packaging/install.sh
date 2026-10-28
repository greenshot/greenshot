#!/usr/bin/env bash
# Installs Greenshot for the current user: the program to ~/.local/lib/greenshot, the command to ~/.local/bin,
# the menu entry, the greenshot: links and the icon. With --autostart Greenshot also starts with the session.
# Run it from anywhere: linux/packaging/install.sh [--autostart]
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
data="${XDG_DATA_HOME:-$HOME/.local/share}"
config="${XDG_CONFIG_HOME:-$HOME/.config}"
id=org.getgreenshot.Greenshot

for tool in grim wl-copy; do
    command -v "$tool" >/dev/null || echo "Note: $tool is not installed (packages: grim, wl-clipboard), see linux/README.md"
done

"$here/stage.sh" --prefix "$HOME/.local"

# The names of earlier versions
rm -f "$data/applications/greenshot.desktop" "$data/applications/greenshot-url.desktop"
find "$data/icons/hicolor" -name greenshot.png -o -name greenshot.svg 2>/dev/null | xargs -r rm -f

command -v xdg-mime >/dev/null && xdg-mime default "$id.Url.desktop" x-scheme-handler/greenshot || true

if [[ "${1:-}" == "--autostart" ]]; then
    mkdir -p "$config/autostart"
    install -m 644 "$here/$id.desktop" "$config/autostart/greenshot.desktop"
    echo "Greenshot starts with your session"
fi

command -v update-desktop-database >/dev/null && update-desktop-database "$data/applications" || true
command -v gtk-update-icon-cache >/dev/null && gtk-update-icon-cache -q -t "$data/icons/hicolor" || true
echo "Installed. Start it with: greenshot"
