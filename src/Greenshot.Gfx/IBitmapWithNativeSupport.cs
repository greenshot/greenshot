// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;
using System.Windows.Media.Imaging;

namespace Greenshot.Gfx
{
    /// <summary>
    /// This adds native bitmap support to IBitmap
    /// </summary>
    public interface IBitmapWithNativeSupport : IBitmap
    {
        /// <summary>
        ///     Retrieves a Bitmap which only can be used as long as the underlying implementation is not disposed.
        ///     Do not dispose this.
        /// </summary>
        Bitmap NativeBitmap { get; }
        
        /// <summary>
        ///     Retrieves a BitmapSource which only can be used as long as the underlying implementation is not disposed.
        /// </summary>
        BitmapSource NativeBitmapSource { get; }

        /// <summary>
        /// Return the Size
        /// </summary>
        Size Size { get; }
    }
}