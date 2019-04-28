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

using System;
using System.Drawing;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Addons.Core
{
    /// <summary>
    ///     This allows us to repeatedly capture the screen via DotNet
    /// </summary>
    public class DotNetScreenCapture : IDisposable
    {


        /// <summary>
        /// Return the source rectangle
        /// </summary>
        public NativeRect SourceRect { get; }

        /// <summary>
        /// Return the source rectangle
        /// </summary>
        public NativeSize DestinationSize { get; }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="sourceCaptureBounds">NativeRect, optional, with the source area from the screen</param>
        /// <param name="requestedSize">NativeSize, optional, specifying the resulting size</param>
        public DotNetScreenCapture(NativeRect? sourceCaptureBounds = null, NativeSize? requestedSize = null)
        {
 
        }

        /// <summary>
        /// Capture a frame from the screen
        /// </summary>
        public void CaptureFrame()
        {
 
        }


        /// <summary>
        /// Get the frame, captured with the previous CaptureFrame call, as 
        /// </summary>
        /// <returns>Bitmap</returns>
        public Bitmap CurrentFrameAsBitmap()
        {
            return null;
        }

        /// <summary>
        /// Dispose all DC, DIB, handles etc
        /// </summary>
        public void Dispose()
        {

        }
    }
}