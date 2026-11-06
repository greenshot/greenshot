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

using System.Windows;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// The colors the shared styles (Wpf/Styles) use for an element and everything in it. Without a value set anywhere above it's
    /// <see cref="ThemeManager.Instance"/>, Greenshot's theme. A part with a theme setting of its own (the panel area of the editor
    /// follows the editor theme) sets a <see cref="ThemePalette"/> on its root, so views of plugins in it look the same as the editor.
    /// Both have the same brush properties, the styles bind to them by name: <c>{Binding Path=(wpf:ThemeScope.Theme).ForegroundBrush, RelativeSource={RelativeSource Self}}</c>.
    /// Context menus and tool tips are windows of their own and keep Greenshot's theme.
    /// </summary>
    public static class ThemeScope
    {
        public static readonly DependencyProperty ThemeProperty = DependencyProperty.RegisterAttached(
            "Theme", typeof(object), typeof(ThemeScope),
            new FrameworkPropertyMetadata(ThemeManager.Instance, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The <see cref="ThemeManager"/> or a <see cref="ThemePalette"/> for the element
        /// </summary>
        public static object GetTheme(DependencyObject element) => element.GetValue(ThemeProperty);

        /// <summary>
        /// Use the palette for the element and everything in it, null for Greenshot's theme again
        /// </summary>
        public static void SetTheme(DependencyObject element, object value)
        {
            if (value == null)
            {
                element.ClearValue(ThemeProperty);
                return;
            }

            element.SetValue(ThemeProperty, value);
        }
    }
}
