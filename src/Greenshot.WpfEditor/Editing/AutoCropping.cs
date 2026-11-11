/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Greenshot.Document;
using Greenshot.Document.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.WpfEditor.Editing
{
    /// <summary>
    /// Finds the part of the document which isn't a border of one color, like the auto crop of the old editor
    /// </summary>
    public static class AutoCropping
    {
        // Colors which differ this little from the border still count as border (JPEG artifacts)
        private const int Tolerance = 8;

        /// <summary>
        /// The area inside the border in canvas pixels, null when there is no border or only border
        /// </summary>
        public static RectD? ContentArea(GreenshotDocument document)
        {
            var canvas = document.CanvasArea();
            using var image = DocumentRenderer.Render(document);
            int width = image.Width, height = image.Height;
            if (width < 2 || height < 2)
            {
                return null;
            }

            var border = image[0, 0];
            int left = width, top = height, right = -1, bottom = -1;
            image.ProcessPixelRows(pixels =>
            {
                for (int y = 0; y < height; y++)
                {
                    var row = pixels.GetRowSpan(y);
                    for (int x = 0; x < width; x++)
                    {
                        if (Differs(row[x], border))
                        {
                            if (x < left) left = x;
                            if (x > right) right = x;
                            if (y < top) top = y;
                            if (y > bottom) bottom = y;
                        }
                    }
                }
            });

            if (right < 0 || (left == 0 && top == 0 && right == width - 1 && bottom == height - 1))
            {
                return null;
            }

            return new RectD(canvas.X + left, canvas.Y + top, right - left + 1, bottom - top + 1);
        }

        private static bool Differs(Rgba32 a, Rgba32 b) =>
            System.Math.Abs(a.R - b.R) > Tolerance || System.Math.Abs(a.G - b.G) > Tolerance || System.Math.Abs(a.B - b.B) > Tolerance || System.Math.Abs(a.A - b.A) > Tolerance;
    }
}
