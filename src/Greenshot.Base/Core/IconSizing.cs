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
using System.Drawing;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The one rule for the size of the icons in menus and tool strips: the configured base size (the size at 100%)
    /// scaled with the DPI of the display the menu or window is on. Change the rule here and every surface follows.
    /// </summary>
    public static class IconSizing
    {
        /// <summary>
        /// The DPI of a display at 100%
        /// </summary>
        public const int DefaultDpi = 96;

        /// <summary>
        /// The base size which is used when none is configured
        /// </summary>
        public const int DefaultBaseSize = 16;

        /// <summary>
        /// The size in pixels of an icon with the base size on a display with the DPI, e.g. 16 at 144 DPI (150%) is 24.
        /// </summary>
        /// <param name="baseSize">int with the size at 100%</param>
        /// <param name="dpi">int with the DPI of the display</param>
        /// <returns>int with the size in pixels</returns>
        public static int PixelSize(int baseSize, int dpi)
        {
            if (baseSize <= 0)
            {
                baseSize = DefaultBaseSize;
            }

            if (dpi <= 0)
            {
                dpi = DefaultDpi;
            }

            return (int)Math.Round(baseSize * dpi / (double)DefaultDpi, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The size in pixels of the configured icon size on a display with the DPI
        /// </summary>
        /// <param name="dpi">int with the DPI of the display</param>
        /// <returns>int with the size in pixels</returns>
        public static int PixelSize(int dpi) => PixelSize(ConfiguredBaseSize, dpi);

        /// <summary>
        /// The configured icon size at 100% (BaseIconSize in greenshot.ini)
        /// </summary>
        public static int ConfiguredBaseSize
        {
            get
            {
                try
                {
                    var coreConfiguration = IniConfigRegistry.GetSection<ICoreConfiguration>();
                    return coreConfiguration?.IconSize.Width ?? DefaultBaseSize;
                }
                catch (Exception)
                {
                    // Not registered, e.g. in tests
                    return DefaultBaseSize;
                }
            }
        }

        /// <summary>
        /// The DPI of the display at the location on the screen, e.g. where a menu is going to be shown
        /// </summary>
        /// <param name="screenLocation">Point in screen coordinates</param>
        /// <returns>int with the DPI</returns>
        public static int DpiAt(Point screenLocation)
        {
            try
            {
                int dpi = NativeDpiMethods.GetDpi(new NativePoint(screenLocation.X, screenLocation.Y));
                return dpi > 0 ? dpi : DefaultDpi;
            }
            catch (Exception)
            {
                return DefaultDpi;
            }
        }
    }
}
