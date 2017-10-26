#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Greenshot.Gfx.Stitching
{
    /// <summary>
    /// This class helps to stitch bitmaps together
    /// </summary>
    public class BitmapStitcher
    {
        private PixelFormat _resultPixelFormat;
        private readonly IList<StitchInfo> _stitchInfos = new List<StitchInfo>();
        /// <summary>
        /// Defines if the header is found and removed
        /// </summary>
        public bool RemoveHeader { get; set; } = true;

        /// <summary>
        /// Defines if a footer is found and removed
        /// </summary>
        public bool RemoveFooter { get; set; } = true;
        
        /// <summary>
        /// Remove trailing lines
        /// </summary>
        public bool RemoveEnd { get; set; } = true;

        /// <summary>
        /// Adds a bitmap to be stitched
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <returns>BitmapStitcher for fluent calling</returns>
        public BitmapStitcher AddBitmap(Bitmap bitmap)
        {
            var stitchInfo = new StitchInfo(bitmap);
            if (_stitchInfos.Count >= 1)
            {
                if (RemoveHeader)
                {
                    stitchInfo.ScanForHeader(_stitchInfos[0]);
                }
                stitchInfo.FindTargetLocation(_stitchInfos.Last());
            }
            else
            {
                _resultPixelFormat = bitmap.PixelFormat;
            }
            _stitchInfos.Add(stitchInfo);
            return this;
        }

        /// <summary>
        /// Create the resulting bitmap, this needs to be disposed
        /// </summary>
        /// <returns>Bitmap</returns>
        public Bitmap Result()
        {
            if (RemoveEnd)
            {
                _stitchInfos.Last().RemoveTrailingLines();
            }
            var totalHeight = _stitchInfos.Sum(info => info.SourceRect.Height);

            var resultBitmap = BitmapFactory.CreateEmpty(_stitchInfos[0].SourceRect.Width, totalHeight, _resultPixelFormat);

            using (var graphics = Graphics.FromImage(resultBitmap))
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
    }
}
