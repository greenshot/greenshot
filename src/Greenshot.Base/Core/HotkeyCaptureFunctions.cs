/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Stable technical identifiers for the capture functions a user can pick for a <see cref="HotkeyShortcutConfig"/> entry.
    /// These ids are persisted in the configuration and must never change, unlike the translated display names.
    /// They are resolved to the actual (built-in) capture recipe id at registration time, see
    /// Greenshot.Helpers.HotkeyShortcutManager.ResolveRecipeId.
    /// </summary>
    public static class HotkeyCaptureFunctions
    {
        /// <summary>Interactively select a region on the screen.</summary>
        public const string Region = "Region";

        /// <summary>Capture a window (interactively or the active window, depending on CaptureWindowsInteractive).</summary>
        public const string Window = "Window";

        /// <summary>Capture the full screen (or all screens, depending on ScreenCaptureMode).</summary>
        public const string Fullscreen = "Fullscreen";

        /// <summary>Capture the last used region again, without interaction.</summary>
        public const string LastRegion = "LastRegion";

        /// <summary>Open/import the image currently on the clipboard.</summary>
        public const string Clipboard = "Clipboard";

        /// <summary>Every known capture function id, in the order they should be offered to the user.</summary>
        public static readonly string[] All =
        {
            Region,
            Window,
            Fullscreen,
            LastRegion,
            Clipboard
        };
    }
}
