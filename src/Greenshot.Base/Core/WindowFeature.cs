/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Represents a window boundary metadata feature on the capture
    /// </summary>
    public class WindowFeature : IDetectedFeature
    {
        /// <inheritdoc />
        public NativeRect Bounds { get; private set; }

        /// <inheritdoc />
        public string FeatureType => "Window";

        /// <inheritdoc />
        public string Text => Window?.Text;

        /// <inheritdoc />
        public string ToolTipText => Window?.Text;

        /// <summary>
        /// The underlying WindowDetails object
        /// </summary>
        public WindowDetails Window { get; }

        /// <summary>
        /// Z-index of the window at the time of enumeration
        /// </summary>
        public int ZIndex { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="window">The WindowDetails to wrap</param>
        /// <param name="zIndex">The Z-index</param>
        public WindowFeature(WindowDetails window, int zIndex)
        {
            Window = window;
            Bounds = window.WindowRectangle;
            ZIndex = zIndex;
        }

        /// <inheritdoc />
        public void Offset(int x, int y)
        {
            Bounds = Bounds.Offset(x, y);
        }
    }
}
