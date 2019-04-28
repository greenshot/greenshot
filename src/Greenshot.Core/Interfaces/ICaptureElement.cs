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
using Dapplo.Windows.Common.Structs;
using Greenshot.Core.Enums;

namespace Greenshot.Core.Interfaces
{
    /// <summary>
    /// This specifies a single element of a capture, making it possible to have a capture contain mouse, window, popup etc information
    /// </summary>
    public interface ICaptureElement<TContent> : IDisposable
    {
        /// <summary>
        /// The location or bounds of the content
        /// </summary>
        NativeRect Bounds
        {
            get;
            set;
        }

        /// <summary>
        /// The actual content
        /// </summary>
        TContent Content
        {
            get;
            set;
        }

        /// <summary>
        /// Used to specify what type of capture element this is
        /// </summary>
        CaptureElementType ElementType { get; set; }

        /// <summary>
        /// Meta-data for this capture element, e.g. the text of a window
        /// </summary>
        IDictionary<string, string> MetaData { get; }
    }
}
