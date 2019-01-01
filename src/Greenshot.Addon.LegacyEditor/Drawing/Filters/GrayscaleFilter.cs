﻿#region Greenshot GNU General Public License

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

#region Usings

using System;
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addon.LegacyEditor.Drawing.Filters
{
	/// <summary>
	///     Description of GrayscaleFilter.
	/// </summary>
	[Serializable]
	public class GrayscaleFilter : AbstractFilter
	{
		public GrayscaleFilter(DrawableContainer parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
		}

		public override void Apply(Graphics graphics, Bitmap applyBitmap, NativeRect rect, RenderMode renderMode)
		{
			var applyRect = BitmapHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);

			if (applyRect.Width == 0 || applyRect.Height == 0)
			{
				// nothing to do
				return;
			}
			var state = graphics.Save();
			if (Invert)
			{
				graphics.SetClip(applyRect);
				graphics.ExcludeClip(rect);
			}
			var grayscaleMatrix = new ColorMatrix(new[]
			{
				new[] {.3f, .3f, .3f, 0, 0},
				new[] {.59f, .59f, .59f, 0, 0},
				new[] {.11f, .11f, .11f, 0, 0},
				new float[] {0, 0, 0, 1, 0},
				new float[] {0, 0, 0, 0, 1}
			});
			using (var ia = new ImageAttributes())
			{
				ia.SetColorMatrix(grayscaleMatrix);
				graphics.DrawImage(applyBitmap, applyRect, applyRect.X, applyRect.Y, applyRect.Width, applyRect.Height, GraphicsUnit.Pixel, ia);
			}
			graphics.Restore(state);
		}
	}
}