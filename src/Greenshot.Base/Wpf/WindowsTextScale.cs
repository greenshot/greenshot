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
using log4net;
using Windows.UI.ViewManagement;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// The "Text size" of Windows (Settings, Accessibility): 1.0 to 2.25. WPF on .NET Framework only follows it where
    /// the system font size is used, Greenshot's windows multiply their text sizes with it, see <see cref="FontSizeExtension"/>
    /// </summary>
    internal sealed class WindowsTextScale
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsTextScale));

        // Kept in a field: the TextScaleFactorChanged event only fires as long as the object lives
        private readonly UISettings _uiSettings;

        public event Action Changed;

        public WindowsTextScale()
        {
            _uiSettings = new UISettings();
            _uiSettings.TextScaleFactorChanged += (sender, args) => Changed?.Invoke();
        }

        public double Factor
        {
            get
            {
                try
                {
                    double factor = _uiSettings.TextScaleFactor;
                    return factor >= 1 ? factor : 1;
                }
                catch (Exception ex)
                {
                    Log.Debug("Couldn't read the Windows text size", ex);
                    return 1;
                }
            }
        }
    }
}
