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
using System.Linq;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Capturing
{
    /// <summary>
    /// The numbers of the numbered hotspots of a capture window: in reading order (top to bottom, then left to right),
    /// and a hotspot which comes again (the features changed, e.g. more text was found) keeps its number, so a number the user is
    /// about to press doesn't move. New hotspots get the next free numbers of their tool.
    /// </summary>
    public sealed class HotspotNumbering
    {
        // A hotspot is the same when its tool, area, label and value are
        private readonly Dictionary<(string ToolId, int X, int Y, int Width, int Height, string Text, string Value), int> _numbers = new();
        private readonly Dictionary<string, int> _lastNumber = new();

        /// <summary>
        /// The numbered hotspots of a tool with their numbers, ordered by number
        /// </summary>
        public IList<(CaptureFormHotspot Hotspot, int Number)> Number(IEnumerable<CaptureFormHotspot> hotspots, string toolId)
        {
            var numbered = new List<(CaptureFormHotspot Hotspot, int Number)>();
            if (toolId == null)
            {
                return numbered;
            }
            var ofTool = (hotspots ?? Enumerable.Empty<CaptureFormHotspot>())
                .Where(hotspot => hotspot != null && hotspot.IsNumbered && hotspot.ToolId == toolId)
                .OrderBy(hotspot => hotspot.Bounds.Top).ThenBy(hotspot => hotspot.Bounds.Left);
            var seen = new HashSet<int>();
            foreach (var hotspot in ofTool)
            {
                var key = KeyOf(hotspot);
                if (!_numbers.TryGetValue(key, out int number))
                {
                    _lastNumber.TryGetValue(toolId, out int last);
                    number = last + 1;
                    _lastNumber[toolId] = number;
                    _numbers[key] = number;
                }
                // The same item twice (e.g. two features with the same text and area) gets one number
                if (seen.Add(number))
                {
                    numbered.Add((hotspot, number));
                }
            }
            return numbered.OrderBy(entry => entry.Number).ToList();
        }

        private static (string, int, int, int, int, string, string) KeyOf(CaptureFormHotspot hotspot) =>
            (hotspot.ToolId, hotspot.Bounds.X, hotspot.Bounds.Y, hotspot.Bounds.Width, hotspot.Bounds.Height, hotspot.Text, hotspot.ToolTipText);
    }
}
