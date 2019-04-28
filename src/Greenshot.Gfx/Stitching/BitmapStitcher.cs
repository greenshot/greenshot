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
using System.Drawing.Imaging;
using System.Linq;

namespace Greenshot.Gfx.Stitching
{
    /// <summary>
    /// This class helps to stitch bitmaps together
    /// </summary>
    public class BitmapStitcher : IDisposable
    {
        private PixelFormat? _resultPixelFormat;
        private readonly IList<StitchInfo> _stitchInfos = new List<StitchInfo>();
        /// <summary>
        ///  If set to true, the BitmapStitcher will try to detect a header and remove this
        /// </summary>
        public bool RemoveHeader { get; set; } = true;

        /// <summary>
        /// If set to true, the BitmapStitcher will try to detect a footer and remove this.
        /// Default is false, as this doesn't work well with empty locations
        /// </summary>
        public bool RemoveFooter { get; set; } = false;
        
        /// <summary>
        /// Trim trailing lines
        /// </summary>
        public bool Trim { get; set; } = true;

        /// <summary>
        /// Adds a bitmap to be stitched
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <returns>BitmapStitcher for fluent calling</returns>
        public BitmapStitcher AddBitmap(IBitmapWithNativeSupport bitmap)
        {
            if (!_resultPixelFormat.HasValue)
            {
                _resultPixelFormat = bitmap.PixelFormat;
            }
            var stitchInfo = new StitchInfo(bitmap);
            _stitchInfos.Add(stitchInfo);
            return this;
        }

        /// <summary>
        /// Create the resulting bitmap, this needs to be disposed
        /// </summary>
        /// <returns>Bitmap</returns>
        public IBitmapWithNativeSupport Result()
        {
            if (RemoveHeader)
            {
                var first = _stitchInfos[0];
                foreach (var stitchInfo in _stitchInfos.Skip(1))
                {
                    stitchInfo.RemoveHeader(first);
                }
            }

            if (RemoveFooter)
            {
                var first = _stitchInfos[0];
                foreach (var stitchInfo in _stitchInfos.Skip(1))
                {
                    stitchInfo.RemoveFooter(first);
                }
            }

            if (Trim)
            {
                // Remove from the last to first, until something is left
                foreach (var stitchInfo in _stitchInfos.Reverse())
                {
                    if (stitchInfo.Trim().SourceRect.Height > 0)
                    {
                        break;
                    }
                }
            }

            var previous = _stitchInfos[0];
            // Find target locations
            foreach (var stitchInfo in _stitchInfos.Skip(1))
            {
                stitchInfo.FindTargetLocation(previous);
                previous = stitchInfo;
            }

            // Calculate the total height of the result bitmap
            var totalHeight = _stitchInfos.Sum(info => info.SourceRect.Height);
            // Create the resulting bitmap
            var resultBitmap = BitmapFactory.CreateEmpty(_stitchInfos[0].SourceRect.Width, totalHeight, _resultPixelFormat ?? PixelFormat.Format32bppArgb);

            // Now stitch the captures together by copying them onto the result bitmap
            using (var graphics = Graphics.FromImage(resultBitmap.NativeBitmap))
            {
                var currentPosition = 0;
                foreach (var stitchInfo in _stitchInfos)
                {
                    stitchInfo.DrawTo(graphics, currentPosition);
                    currentPosition += stitchInfo.SourceRect.Height;
                }
            }
            return resultBitmap;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var stitchInfo in _stitchInfos)
            {
                stitchInfo.Dispose();
            }
        }
    }
}
