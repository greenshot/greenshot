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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Greenshot.Document;
using SixLabors.Fonts;

namespace Greenshot.Editor.Standalone.Rendering
{
    /// <summary>
    /// The fonts the renderer draws text with: the first installed family of a style's list, and the Twemoji font which
    /// comes with Greenshot for emoji, so they look the same on every system.
    /// </summary>
    public static class EditorFonts
    {
        /// <summary>
        /// The name of the emoji font file, next to the editor
        /// </summary>
        public const string EmojiFontFileName = "Twemoji.Mozilla.ttf";

        // Generic families and the fonts which stand for them on Windows, macOS and Linux
        private static readonly Dictionary<string, string[]> Generic = new(StringComparer.OrdinalIgnoreCase)
        {
            [TextStyle.SansSerif] = new[] { "Segoe UI", "Microsoft Sans Serif", "Arial", "Helvetica Neue", "Helvetica", "Inter", "DejaVu Sans", "Liberation Sans", "Noto Sans" },
            [TextStyle.Serif] = new[] { "Times New Roman", "Times", "DejaVu Serif", "Liberation Serif", "Noto Serif" },
            [TextStyle.Monospace] = new[] { "Consolas", "Cascadia Mono", "Courier New", "Menlo", "DejaVu Sans Mono", "Liberation Mono" }
        };

        private static readonly ConcurrentDictionary<string, FontFamily> Resolved = new(StringComparer.OrdinalIgnoreCase);
        private static readonly FontCollection Own = new();

        private static readonly Lazy<FontFamily?> EmojiFamily = new(() =>
        {
            var file = System.IO.Path.Combine(AppContext.BaseDirectory, EmojiFontFileName);
            if (!File.Exists(file))
            {
                return null;
            }

            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Own.Add(stream);
        });

        /// <summary>
        /// The Twemoji font, null when its file is missing (a broken installation): emoji are then drawn with what the system has
        /// </summary>
        public static FontFamily? Emoji => EmojiFamily.Value;

        /// <summary>
        /// The names of the installed font families, sorted
        /// </summary>
        public static IReadOnlyList<string> InstalledFamilies { get; } = SystemFonts.Families.Select(family => family.Name).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase).ToList();

        /// <summary>
        /// The first family of the style's list which is installed, else a sans-serif one, else any
        /// </summary>
        public static FontFamily FamilyOf(TextStyle style) => Resolved.GetOrAdd(style?.Family ?? string.Empty, static list =>
        {
            var names = TextStyle.Families(list).SelectMany(name => Generic.TryGetValue(name, out var stand) ? stand : new[] { name }).Concat(Generic[TextStyle.SansSerif]);
            foreach (var name in names)
            {
                if (SystemFonts.TryGet(name, out var found))
                {
                    return found;
                }
            }

            return SystemFonts.Families.First();
        });

        /// <summary>
        /// The font of a style, its size in canvas pixels
        /// </summary>
        public static Font FontOf(TextStyle style)
        {
            bool bold = (style?.Weight ?? 400) >= 600, italic = style?.Italic == true;
            var fontStyle = bold ? italic ? FontStyle.BoldItalic : FontStyle.Bold : italic ? FontStyle.Italic : FontStyle.Regular;
            // Fonts measures in points at 72 dpi, that is one point per pixel
            return FamilyOf(style).CreateFont((float)Math.Max(1, style?.Size ?? 11), fontStyle);
        }
    }
}
