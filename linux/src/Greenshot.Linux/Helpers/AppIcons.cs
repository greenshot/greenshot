/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Avalonia.Controls;
using Avalonia.Platform;

namespace Greenshot.Linux.Helpers;

internal static class AppIcons
{
    private static WindowIcon? _window;
    private static WindowIcon? _tray;

    public static WindowIcon Window => _window ??= Load("greenshot-64.png");

    public static WindowIcon Tray => _tray ??= Load("greenshot-32.png");

    private static WindowIcon Load(string name) => new(AssetLoader.Open(new Uri($"avares://greenshot/Assets/{name}")));
}
