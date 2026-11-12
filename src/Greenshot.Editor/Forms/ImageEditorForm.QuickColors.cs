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

using Greenshot.Editor.Controls;
using Greenshot.Editor.Drawing.Fields;
namespace Greenshot.Editor.Forms
{
    public partial class ImageEditorForm
    {
        private QuickColorPalette _quickColors;

        private void InitializeQuickColors()
        {
            _quickColors = new QuickColorPalette(propertiesToolStrip, EditorConfiguration);
            _quickColors.ColorSelected += color =>
            {
                if (_surface?.FieldAggregator is not FieldAggregator fields || !fields.HasFieldValue(_quickColors.Target)) return;
                fields.GetField(_quickColors.Target).Value = color;
                _quickColors.Remember(color);
                FocusCanvas(fromKeyboard: false);
            };
            btnLineColor.ColorSelected += (s, e) => _quickColors.Remember(btnLineColor.SelectedColor);
            btnFillColor.ColorSelected += (s, e) => _quickColors.Remember(btnFillColor.SelectedColor);
            Activated += (s, e) => RefreshQuickColors();
            Disposed += (s, e) => _quickColors.Dispose();
        }

        private void RefreshQuickColors()
        {
            var fields = _surface?.FieldAggregator as FieldAggregator;
            _quickColors?.Refresh(fields?.HasFieldValue(FieldType.LINE_COLOR) == true, fields?.HasFieldValue(FieldType.FILL_COLOR) == true);
        }
    }
}