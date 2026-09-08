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

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Standard identifiers for built-in recipe step types.
    /// </summary>
    public static class WellKnownStepTypes
    {
        /// <summary>
        /// Acquires raw pixels from a capture source (Screen, Window, ActiveWindow, Clipboard, File, LastRegion).
        /// </summary>
        public const string Source = "Source";

        /// <summary>
        /// Presents interactive selection UI (region overlay, window snap, or OCR text selection).
        /// </summary>
        public const string InteractiveSelection = "InteractiveSelection";

        /// <summary>
        /// Provides immediate acquisition feedback (camera shutter sound, flash, DPI alignment).
        /// </summary>
        public const string ImmediateFeedback = "ImmediateFeedback";

        /// <summary>
        /// Runs image processors (e.g. Windows 10 OCR, TitleFix, or plugin processors).
        /// </summary>
        public const string Processors = "Processors";

        /// <summary>
        /// Exports the capture surface to one or more destinations (File, Clipboard, Editor, Picker, etc.).
        /// </summary>
        public const string Destinations = "Destinations";

        /// <summary>
        /// Dispatches completion feedback after export (e.g. tray balloon/toast notification).
        /// </summary>
        public const string Notification = "Notification";

        /// <summary>
        /// Applies an image effect (e.g. Border, DropShadow, TornEdge, Invert, Grayscale, Rotate, Resize).
        /// </summary>
        public const string Effect = "Effect";

        /// <summary>
        /// Applies a border to the capture. Maintained as alias to Effect for backward compatibility.
        /// </summary>
        public const string Border = "Border";

        /// <summary>
        /// Evaluates a condition and executes child steps based on the result.
        /// </summary>
        public const string Conditional = "Conditional";

        /// <summary>
        /// Scans text via OCR, locates occurrences matching regex/text pattern, and applies effects (Blur, Pixelize, Highlight, Redact).
        /// </summary>
        public const string TextEffect = "TextEffect";
    }
}
