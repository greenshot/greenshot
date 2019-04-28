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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Structs;
using Greenshot.Core.Enums;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core
{
    /// <summary>
    /// The CaptureElement contains the information of an element in a capture, e.g the window and mouse
    /// </summary>
    public class CaptureElement<TContent> : ICaptureElement<TContent>
    {
        public CaptureElement(NativePoint location, TContent content)
        {
            NativeSize size;
            if (content is BitmapSource bitmapSource)
            {
                size = new NativeSize((int)bitmapSource.Width, (int)bitmapSource.Height);
            }
            else if (content is Bitmap bitmap)
            {
                size = new NativeSize(bitmap.Width, bitmap.Height);
            }
            else
            {
                throw new NotSupportedException(typeof(TContent).ToString());
            }

            Bounds = new NativeRect(location, size);
            Content = content;
        }

        /// <inheritdoc />
        public NativeRect Bounds { get; set; }

        /// <inheritdoc />
        public TContent Content { get; set; }

        /// <inheritdoc />
        public CaptureElementType ElementType { get; set; } = CaptureElementType.Unknown;

        /// <inheritdoc />
        public IDictionary<string, string> MetaData { get; } = new Dictionary<string, string>();

        public void Dispose()
        {
            if (Content is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
