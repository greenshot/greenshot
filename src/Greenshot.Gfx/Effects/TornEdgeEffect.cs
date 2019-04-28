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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.Effects
{
	/// <summary>
	///     TornEdgeEffect extends on DropShadowEffect
	/// </summary>
	[TypeConverter(typeof(EffectConverter))]
	public sealed class TornEdgeEffect : DropShadowEffect
	{
        /// <summary>
        /// Height of the teeth
        /// </summary>
	    public int ToothHeight { get; set; } = 12;

        /// <summary>
        /// How many teeth are horizontally displayed
        /// </summary>
	    public int HorizontalToothRange { get; set; } = 20;

        /// <summary>
        /// How many teeth are vertically displayed
        /// </summary>
	    public int VerticalToothRange { get; set; } = 20;

        /// <summary>
        /// Specify which edges should get teeth
        /// </summary>
		public bool[] Edges { get; set; } = { true, true, true, true };

        /// <summary>
        /// Generate a shadow?
        /// </summary>
        public bool GenerateShadow { get; set; } = true;

        /// <inheritdoc/>
		public override IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
			var tmpTornImage = CreateTornEdge(sourceBitmap, ToothHeight, HorizontalToothRange, VerticalToothRange, Edges);
		    if (!GenerateShadow)
		    {
		        return tmpTornImage;
		    }
		    using (tmpTornImage)
		    {
		        return tmpTornImage.CreateShadow(Darkness, ShadowSize, ShadowOffset, matrix, PixelFormat.Format32bppArgb);
		    }
		}

	    /// <summary>
	    ///     Helper method for the tornedge
	    /// </summary>
	    /// <param name="path">Path to draw to</param>
	    /// <param name="points">Points for the lines to draw</param>
	    private static void DrawLines(GraphicsPath path, IList<NativePoint> points)
	    {
	        path.AddLine(points[0], points[1]);
	        for (var i = 0; i < points.Count - 1; i++)
	        {
	            path.AddLine(points[i], points[i + 1]);
	        }
	    }

	    /// <summary>
	    ///     Make the picture look like it's torn
	    /// </summary>
	    /// <param name="sourceBitmap">Bitmap to make torn edge off</param>
	    /// <param name="toothHeight">How large (height) is each tooth</param>
	    /// <param name="horizontalToothRange">How wide is a horizontal tooth</param>
	    /// <param name="verticalToothRange">How wide is a vertical tooth</param>
	    /// <param name="edges">
	    ///     bool[] with information on if the edge needs torn or not. Order is clockwise:
	    ///     0=top,1=right,2=bottom,3=left
	    /// </param>
	    /// <returns>Changed bitmap</returns>
	    public static IBitmapWithNativeSupport CreateTornEdge(IBitmapWithNativeSupport sourceBitmap, int toothHeight, int horizontalToothRange, int verticalToothRange, bool[] edges)
	    {
	        var returnBitmap = BitmapFactory.CreateEmpty(sourceBitmap.Width, sourceBitmap.Height, PixelFormat.Format32bppArgb, Color.Empty, sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);
	        using (var path = new GraphicsPath())
	        {
	            var random = new Random();
	            var horizontalRegions = (int)Math.Round((float)sourceBitmap.Width / horizontalToothRange);
	            var verticalRegions = (int)Math.Round((float)sourceBitmap.Height / verticalToothRange);

	            var topLeft = new NativePoint(0, 0);
	            var topRight = new NativePoint(sourceBitmap.Width, 0);
	            var bottomLeft = new NativePoint(0, sourceBitmap.Height);
	            var bottomRight = new NativePoint(sourceBitmap.Width, sourceBitmap.Height);

	            var points = new List<NativePoint>();

	            if (edges[0])
	            {
	                // calculate starting point only if the left edge is torn
	                if (!edges[3])
	                {
	                    points.Add(topLeft);
	                }
	                else
	                {
	                    points.Add(new NativePoint(random.Next(1, toothHeight), random.Next(1, toothHeight)));
	                }
	                for (var i = 1; i < horizontalRegions - 1; i++)
	                {
	                    points.Add(new NativePoint(i * horizontalToothRange, random.Next(1, toothHeight)));
	                }
	                points.Add(new NativePoint(sourceBitmap.Width - random.Next(1, toothHeight), random.Next(1, toothHeight)));
	            }
	            else
	            {
	                // set start & endpoint to be the default "whole-line"
	                points.Add(topLeft);
	                points.Add(topRight);
	            }
	            // Right
	            if (edges[1])
	            {
	                for (var i = 1; i < verticalRegions - 1; i++)
	                {
	                    points.Add(new NativePoint(sourceBitmap.Width - random.Next(1, toothHeight), i * verticalToothRange));
	                }
	                points.Add(new NativePoint(sourceBitmap.Width - random.Next(1, toothHeight), sourceBitmap.Height - random.Next(1, toothHeight)));
	            }
	            else
	            {
	                // correct previous ending point
	                points[points.Count - 1] = topRight;
	                // set endpoint to be the default "whole-line"
	                points.Add(bottomRight);
	            }
	            // Bottom
	            if (edges[2])
	            {
	                for (var i = 1; i < horizontalRegions - 1; i++)
	                {
	                    points.Add(new NativePoint(sourceBitmap.Width - i * horizontalToothRange, sourceBitmap.Height - random.Next(1, toothHeight)));
	                }
	                points.Add(new NativePoint(random.Next(1, toothHeight), sourceBitmap.Height - random.Next(1, toothHeight)));
	            }
	            else
	            {
	                // correct previous ending point
	                points[points.Count - 1] = bottomRight;
	                // set endpoint to be the default "whole-line"
	                points.Add(bottomLeft);
	            }
	            // Left
	            if (edges[3])
	            {
	                // One fewer as the end point is the starting point
	                for (var i = 1; i < verticalRegions - 1; i++)
	                {
	                    points.Add(new NativePoint(random.Next(1, toothHeight), points[points.Count - 1].Y - verticalToothRange));
	                }
	            }
	            else
	            {
	                // correct previous ending point
	                points[points.Count - 1] = bottomLeft;
	                // set endpoint to be the default "whole-line"
	                points.Add(topLeft);
	            }
	            // End point always is the starting point
	            points[points.Count - 1] = points[0];

	            DrawLines(path, points);

	            path.CloseFigure();

	            // Draw the created figure with the original image by using a TextureBrush so we have anti-aliasing
	            using (var graphics = Graphics.FromImage(returnBitmap.NativeBitmap))
	            {
	                graphics.SmoothingMode = SmoothingMode.HighQuality;
	                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
	                graphics.CompositingQuality = CompositingQuality.HighQuality;
	                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
	                using (Brush brush = new TextureBrush(sourceBitmap.NativeBitmap))
	                {
	                    // Important note: If the target wouldn't be at 0,0 we need to translate-transform!!
	                    graphics.FillPath(brush, path);
	                }
	            }
	        }
	        return returnBitmap;
	    }

    }
}