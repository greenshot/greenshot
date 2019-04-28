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

namespace Greenshot.Core.Interfaces
{
    /// <summary>
    /// This contains all the capture information
    /// </summary>
    public interface ICapture<TContent> : IDisposable
    {
        /// <summary>
        /// The calculated bounds for this capture
        /// </summary>
        NativeRect Bounds { get; }

        /// <summary>
        /// The crop rectangle for this capture
        /// </summary>
        NativeRect CropRect { get; }

        /// <summary>
        /// Meta-data for this capture
        /// </summary>
        IDictionary<string, string> MetaData { get; }

        /// <summary>
        /// Time the capture was made
        /// </summary>
        DateTimeOffset Taken { get; }

        /// <summary>
        /// the actual capture elements, making up the capture
        /// </summary>
        IList<ICaptureElement<TContent>> CaptureElements { get; }
    }
}