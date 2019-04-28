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
using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core
{
    /// <inheritdoc />
    public class Capture<TContent> : ICapture<TContent>
    {
        public void Dispose()
        {
            foreach (var captureElement in CaptureElements)
            {
                captureElement?.Dispose();
            }
            CaptureElements.Clear();
        }

        /// <inheritdoc />
        public DateTimeOffset Taken { get; } = DateTimeOffset.Now;

        /// <inheritdoc />
        public IList<ICaptureElement<TContent>> CaptureElements { get; } = new List<ICaptureElement<TContent>>();

        /// <inheritdoc />
        public NativeRect Bounds => CaptureElements.Select(element => element.Bounds).Aggregate((b1, b2) => b1.Union(b2));

        /// <inheritdoc />
        public NativeRect CropRect { get; set; }

        /// <inheritdoc />
        public IDictionary<string, string> MetaData { get; } = new Dictionary<string, string>();
    }
}
