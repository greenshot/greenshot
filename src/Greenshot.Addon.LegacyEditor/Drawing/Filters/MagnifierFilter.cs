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
using System.Drawing;
using System.Drawing.Drawing2D;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;

namespace Greenshot.Addon.LegacyEditor.Drawing.Filters
{
	[Serializable]
	public class MagnifierFilter : AbstractFilter
	{
		public MagnifierFilter(DrawableContainer parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			AddField(GetType(), FieldTypes.MAGNIFICATION_FACTOR, 2);
		}

		public override void Apply(Graphics graphics, IBitmapWithNativeSupport applyBitmap, NativeRect rect, RenderMode renderMode)
		{
			var applyRect = BitmapHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);

			if (applyRect.Width == 0 || applyRect.Height == 0)
			{
				// nothing to do
				return;
			}
			var magnificationFactor = GetFieldValueAsInt(FieldTypes.MAGNIFICATION_FACTOR);
			var state = graphics.Save();
			if (Invert)
			{
				graphics.SetClip(applyRect);
				graphics.ExcludeClip(rect);
			}
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			var halfWidth = rect.Width / 2;
			var halfHeight = rect.Height / 2;
			var newWidth = rect.Width / magnificationFactor;
			var newHeight = rect.Height / magnificationFactor;
			var source = new NativeRect(rect.X + halfWidth - newWidth / 2, rect.Y + halfHeight - newHeight / 2, newWidth, newHeight);
			graphics.DrawImage(applyBitmap.NativeBitmap, rect, source, GraphicsUnit.Pixel);
			graphics.Restore(state);
		}
	}
}