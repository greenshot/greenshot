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
using System.Windows.Media;
using log4net;
using Windows.UI.ViewManagement;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// The accent color the user picked in the Windows settings (Personalization, Colors), and a notification when it changes.
    /// </summary>
    internal sealed class WindowsAccent
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsAccent));

        // Kept in a field: the ColorValuesChanged event only fires as long as the object lives
        private readonly UISettings _uiSettings;

        /// <summary>
        /// The accent or the light/dark mode changed, raised on a background thread
        /// </summary>
        public event Action Changed;

        public WindowsAccent()
        {
            _uiSettings = new UISettings();
            _uiSettings.ColorValuesChanged += (sender, args) => Changed?.Invoke();
        }

        /// <summary>
        /// The accent shade Windows itself uses for the theme: darker on a light background, lighter on a dark one, so text and
        /// borders in the accent color stay readable. Null when Windows doesn't tell.
        /// </summary>
        public Color? GetAccent(bool isDarkTheme)
        {
            try
            {
                var color = _uiSettings.GetColorValue(isDarkTheme ? UIColorType.AccentLight2 : UIColorType.AccentDark1);
                return Color.FromRgb(color.R, color.G, color.B);
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't read the Windows accent color", ex);
                return null;
            }
        }
    }
}
