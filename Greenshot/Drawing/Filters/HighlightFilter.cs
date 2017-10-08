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

#region Usings

using System;
using System.Drawing;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Drawing.Fields;
using Greenshot.Gfx;
using Greenshot.Gfx.FastBitmap;
using GreenshotPlugin.Interfaces.Drawing;

#endregion

namespace Greenshot.Drawing.Filters
{
	[Serializable]
	public class HighlightFilter : AbstractFilter
	{
	    private static readonly ParallelOptions DefaultParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };

        public HighlightFilter(DrawableContainer parent) : base(parent)
		{
			AddField(GetType(), FieldType.FILL_COLOR, Color.Yellow);
		}

		/// <summary>
		///     Implements the Apply code for the Brightness Filet
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="applyBitmap"></param>
		/// <param name="rect"></param>
		/// <param name="renderMode"></param>
		public override void Apply(Graphics graphics, Bitmap applyBitmap, NativeRect rect, RenderMode renderMode)
		{
			var applyRect = BitmapHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);

			if (applyRect.Width == 0 || applyRect.Height == 0)
			{
				// nothing to do
				return;
			}
			var graphicsState = graphics.Save();
			if (Invert)
			{
				graphics.SetClip(applyRect);
				graphics.ExcludeClip(rect);
			}
			using (var fastBitmap = FastBitmapFactory.CreateCloneOf(applyBitmap, area: applyRect))
			{
				var highlightColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
			    Parallel.For(fastBitmap.Top, fastBitmap.Bottom, DefaultParallelOptions, y =>
			    {
			        for (var x = fastBitmap.Left; x < fastBitmap.Right; x++)
			        {
			            var color = fastBitmap.GetColorAt(x, y);
			            color = Color.FromArgb(color.A, Math.Min(highlightColor.R, color.R), Math.Min(highlightColor.G, color.G), Math.Min(highlightColor.B, color.B));
			            fastBitmap.SetColorAt(x, y, ref color);
			        }
			    });
				fastBitmap.DrawTo(graphics, applyRect.Location);
			}
			graphics.Restore(graphicsState);
		}
	}
}