// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.ComponentModel;
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;

namespace Greenshot.Addon.LegacyEditor.Drawing.Filters
{
    /// <summary>
    /// This defines the interface for all filters
    /// </summary>
	public interface IFilter : INotifyPropertyChanged, IFieldHolder
	{
        /// <summary>
        /// Filters are childen of DrawableContainers, this is the parent
        /// </summary>
		DrawableContainer Parent { get; set; }

        /// <summary>
        /// is the filtered area inverted?
        /// </summary>
		bool Invert { get; set; }

        /// <summary>
        /// Apply this filter
        /// </summary>
        /// <param name="graphics">Graphics to use</param>
        /// <param name="bmp">IBitmapWithNativeSupport to apply to</param>
        /// <param name="rect">NativeRect with the area</param>
        /// <param name="renderMode">RenderMode to use</param>
        void Apply(Graphics graphics, IBitmapWithNativeSupport bmp, NativeRect rect, RenderMode renderMode);
    }
}