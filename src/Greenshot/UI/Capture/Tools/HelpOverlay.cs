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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;

namespace Greenshot.UI.Capture.Tools
{
    /// <summary>
    /// F1 shows or hides a panel with the keys which work now: those of the active tool first, then those which are always active.
    /// The list comes from the registered keys (ICaptureToolHost.KeyBindings), so keys of plugins are in it too,
    /// and the descriptions are read when the panel is shown, in the current language.
    /// </summary>
    public class HelpOverlay : CaptureOverlay
    {
        private bool _visible;

        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterKey(this, Key.F1, ModifierKeys.None, () => Texts.Core.CaptureKeyHelp, Toggle);
        }

        /// <summary>
        /// Another tool has other keys
        /// </summary>
        public override void OnToolChanged()
        {
            if (_visible)
            {
                ShowHelp();
            }
        }

        private void Toggle()
        {
            _visible = !_visible;
            if (_visible)
            {
                ShowHelp();
            }
            else
            {
                Host.HidePanel(this);
            }
        }

        /// <summary>
        /// One row per description: keys which do the same (e.g. the arrow keys, or left and right Shift) share a row
        /// </summary>
        public static IList<(IList<string> Keys, string Description)> GetRows(IEnumerable<CaptureKeyBinding> bindings, ICaptureTool activeTool)
        {
            var rows = new List<(IList<string> Keys, string Description)>();
            var active = (bindings ?? Enumerable.Empty<CaptureKeyBinding>()).Where(binding => binding.IsActiveFor(activeTool)).ToList();
            // The keys of the tool first, they change with the tool; OrderBy is stable, so the order of registration stays
            foreach (var binding in active.OrderBy(binding => binding.Tool == null ? 1 : 0))
            {
                string description = binding.Description;
                int index = rows.FindIndex(row => row.Description == description);
                if (index < 0)
                {
                    rows.Add((new List<string> { binding.KeyText }, description));
                }
                else if (!rows[index].Keys.Contains(binding.KeyText))
                {
                    rows[index].Keys.Add(binding.KeyText);
                }
            }
            return rows;
        }

        private void ShowHelp()
        {
            var style = Host.ToolStyle;
            double gap = style.Scale(8);
            double keyGap = style.Scale(3);
            var title = style.CreateText(Texts.Core.CaptureKeysTitle, 10, bold: true);

            // Measure first: the key caps in one column, right aligned, the descriptions next to them
            double keyColumnWidth = 0, textWidth = title.Width, height = title.Height + gap;
            var rows = new List<(IList<string> Keys, double KeysWidth, FormattedText Text, double Height)>();
            foreach (var (keys, description) in GetRows(Host.KeyBindings, Host.ActiveTool))
            {
                double keysWidth = 0, keysHeight = 0;
                foreach (var key in keys)
                {
                    var keyCap = style.MeasureKeyCap(key);
                    keysWidth += keyCap.Width + (keysWidth > 0 ? keyGap : 0);
                    keysHeight = Math.Max(keysHeight, keyCap.Height);
                }
                var text = style.CreateText(description, 9, style.MutedForeground);
                double rowHeight = Math.Max(keysHeight, text.Height) + style.Scale(4);
                keyColumnWidth = Math.Max(keyColumnWidth, keysWidth);
                textWidth = Math.Max(textWidth, text.Width);
                rows.Add((keys, keysWidth, text, rowHeight));
                height += rowHeight;
            }

            Host.ShowPanel(this, new Size(keyColumnWidth + gap + textWidth, height), dc =>
            {
                dc.DrawText(title, new Point(0, 0));
                double y = title.Height + gap;
                foreach (var (keys, keysWidth, text, rowHeight) in rows)
                {
                    double x = keyColumnWidth - keysWidth;
                    foreach (var key in keys)
                    {
                        var keyCap = style.MeasureKeyCap(key);
                        style.DrawKeyCap(dc, key, new Point(x, y + (rowHeight - keyCap.Height) / 2));
                        x += keyCap.Width + keyGap;
                    }
                    dc.DrawText(text, new Point(keyColumnWidth + gap, y + (rowHeight - text.Height) / 2));
                    y += rowHeight;
                }
            });
        }
    }
}
