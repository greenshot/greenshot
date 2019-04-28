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
using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Gfx.Stitching
{
    /// <summary>
    /// Stores all the information needed to stitch a bitmap
    /// </summary>
    public class StitchInfo : IDisposable
    {
        private readonly IBitmapWithNativeSupport _bitmap;
        private readonly IList<uint> _hashes;

        private NativeRect _sourceRect;

        /// <summary>
        /// Create stitching information for this bitmap
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        public StitchInfo(IBitmapWithNativeSupport bitmap)
        {
            _bitmap = bitmap;
            using (var fastBitmap = FastBitmapFactory.Create(bitmap))
            {
                _hashes = Enumerable.Range(0, fastBitmap.Height).Select(i => fastBitmap.HorizontalHash(i)).ToList();
            }
            _sourceRect = new NativeRect(NativePoint.Empty, bitmap.Size);
        }

        /// <summary>
        /// The rectangle defining the area which needs to be copied
        /// </summary>
        public NativeRect SourceRect => _sourceRect;

        /// <summary>
        /// Scans the header, and changes the SourceRect if something is found
        /// </summary>
        /// <param name="primaryImage">StitchInfo for the first image</param>
        public StitchInfo RemoveHeader(StitchInfo primaryImage)
        {
            int headerLines = 0;

            var primaryImageHashes = primaryImage._hashes;

            // Find header
            while (headerLines < primaryImageHashes.Count && primaryImageHashes[headerLines] == _hashes[headerLines])
            {
                headerLines++;
            }
            // Not interested in 1 line
            if (headerLines > 1)
            {
                // Remove the lines, by moving the top down (and reducing the height)
                _sourceRect = new NativeRect(_sourceRect.X, headerLines, _sourceRect.Width, _sourceRect.Height - headerLines);
            }
            return this;
        }

        /// <summary>
        /// Scans the footer, and changes the SourceRect if something is found
        /// </summary>
        /// <param name="primaryImage">StitchInfo for the first image</param>
        public StitchInfo RemoveFooter(StitchInfo primaryImage)
        {
            var primaryHashes = primaryImage._hashes;

            int footerLines = -1;
            int primaryLocation, footerLocation;

            // Find footer
            do
            {
                footerLines++;
                primaryLocation = primaryImage.SourceRect.Bottom - footerLines;
                footerLocation = _sourceRect.Bottom - footerLines;
            } while (footerLocation > _sourceRect.Top && primaryLocation > primaryImage.SourceRect.Top && primaryHashes[primaryLocation] == _hashes[footerLocation]);

            // Not interested in 1 line
            if (footerLines > 1)
            {
                // Remove the lines by removing the height
                _sourceRect = new NativeRect(_sourceRect.X, _sourceRect.Y, _sourceRect.Width, _sourceRect.Height - footerLines);
            }
            return this;
        }

        /// <summary>
        /// Find the target location of the image
        /// </summary>
        /// <param name="previousImage"></param>
        /// <returns>StitchInfo for fluent usage</returns>
        public StitchInfo FindTargetLocation(StitchInfo previousImage)
        {
            int currentImageHeight = _sourceRect.Height;
            // Find location in the other image
            for (int location = 0; location < previousImage.SourceRect.Height; location++)
            {
                // Do not try to match until the size makes sense
                if (_sourceRect.Height - location > currentImageHeight)
                {
                    continue;
                }

                bool isMatch = true;
                int y = 0;
                while (location + y < previousImage.SourceRect.Height && _sourceRect.Top + y < previousImage._sourceRect.Bottom)
                {
                    if (previousImage._hashes[previousImage._sourceRect.Top + location + y] != _hashes[_sourceRect.Top + y])
                    {
                        isMatch = false;
                        break;
                    }
                    y++;
                }

                if (!isMatch)
                {
                    continue;
                }
                _sourceRect = new NativeRect(_sourceRect.X, _sourceRect.Y + y, _sourceRect.Width, _sourceRect.Height - y);
                break;
            }
            return this;
        }

        /// <summary>
        /// Remove all the trailing repeating lines
        /// </summary>
        /// <returns>StitchInfo for fluent usage</returns>
        public StitchInfo Trim()
        {
            var lastHash = _hashes[_sourceRect.Bottom-1];
            var linesToRemove = 0;
            for (int y = _sourceRect.Bottom-1; y >= _sourceRect.Top; y--)
            {
                if (_hashes[y] != lastHash)
                {
                    break;
                }
                linesToRemove++;
            }
            // TODO: Check if we need to keep at least one line
            if (linesToRemove > 1)
            {
                _sourceRect = _sourceRect.ChangeHeight(_sourceRect.Height - linesToRemove);
            }
            return this;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _bitmap?.Dispose();
        }

        /// <summary>
        /// Draw the bitmap which needed stitching to the destination image
        /// </summary>
        /// <param name="graphics">Grapgics</param>
        /// <param name="y">int </param>
        public void DrawTo(Graphics graphics, int y)
        {
            graphics.DrawImage(_bitmap.NativeBitmap, new Rectangle(0, y, _sourceRect.Width, _sourceRect.Height), _sourceRect, GraphicsUnit.Pixel);
        }
    }
}
