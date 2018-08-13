#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

using System.Collections.Generic;
using System.Windows.Media;
using Dapplo.Windows.Common.Structs;
using Greenshot.Core.Enums;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core
{
    /// <summary>
    /// The CaptureElement contains the information of an element in a capture, e.g the window and mouse
    /// </summary>
    public class CaptureElement : ICaptureElement
    {
        public CaptureElement(NativePoint location, ImageSource content)
        {
            Bounds = new NativeRect(location, new NativeSize((int)content.Width, (int)content.Height));
            Content = content;
        }

        /// <inheritdoc />
        public NativeRect Bounds { get; set; }

        /// <inheritdoc />
        public ImageSource Content { get; set; }

        /// <inheritdoc />
        public CaptureElementType ElementType { get; set; } = CaptureElementType.Unknown;

        /// <inheritdoc />
        public IDictionary<string, string> MetaData { get; } = new Dictionary<string, string>();
    }
}
