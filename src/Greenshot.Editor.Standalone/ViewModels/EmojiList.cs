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
using System.Collections.Generic;

namespace Greenshot.Editor.Standalone.ViewModels
{
    /// <summary>
    /// The emoji the panel offers at a click. Every other emoji can be typed or pasted (on Windows: Win+. opens the emoji
    /// picker of the system).
    /// </summary>
    public static class EmojiList
    {
        public static IReadOnlyList<string> Common { get; } = new[]
        {
            "\U0001F600", "\U0001F603", "\U0001F604", "\U0001F601", "\U0001F606", "\U0001F605", "\U0001F602", "\U0001F642",
            "\U0001F609", "\U0001F60A", "\U0001F60D", "\U0001F618", "\U0001F60E", "\U0001F914", "\U0001F610", "\U0001F644",
            "\U0001F62E", "\U0001F632", "\U0001F633", "\U0001F622", "\U0001F62D", "\U0001F621", "\U0001F631", "\U0001F634",
            "\U0001F44D", "\U0001F44E", "\U0001F44C", "\U0001F44F", "\U0001F64F", "\U0001F4AA", "\U0001F449", "\U0001F448",
            "\U0001F446", "\U0001F447", "✋", "\U0001F44B", "✅", "❌", "❗", "❓",
            "⚠️", "\U0001F6AB", "⭐", "\U0001F525", "\U0001F4A1", "\U0001F4A5", "\U0001F389", "\U0001F680",
            "❤️", "\U0001F494", "\U0001F4AF", "\U0001F41B", "\U0001F512", "\U0001F513", "\U0001F511", "\U0001F50D",
            "\U0001F4CC", "\U0001F4CE", "\U0001F4DD", "\U0001F4C5", "\U0001F4C8", "\U0001F4C9", "\U0001F4E7", "\U0001F4BE",
            "\U0001F5D1️", "⚙️", "\U0001F527", "\U0001F6E0️", "⏰", "⌛", "\U0001F4BB", "\U0001F4F1",
            "➡️", "⬅️", "⬆️", "⬇️", "\U0001F504", "➕", "➖", "✖️",
            "\U0001F534", "\U0001F7E0", "\U0001F7E1", "\U0001F7E2", "\U0001F535", "\U0001F7E3", "⚫", "⚪"
        };
    }
}
