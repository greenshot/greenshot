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
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Addon.LegacyEditor.Drawing.Filters
{
	[Serializable]
	public class BlurFilter : AbstractFilter
	{
		private double _previewQuality;

		public BlurFilter(DrawableContainer parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			AddField(GetType(), FieldTypes.BLUR_RADIUS, 3);
		}

		public double PreviewQuality
		{
			get => _previewQuality;
            set
			{
				_previewQuality = value;
				OnPropertyChanged("PreviewQuality");
			}
		}

		public override void Apply(Graphics graphics, IBitmapWithNativeSupport applyBitmap, NativeRect rect, RenderMode renderMode)
		{
			var blurRadius = GetFieldValueAsInt(FieldTypes.BLUR_RADIUS);
			var applyRect = BitmapHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);
			if (applyRect.Width == 0 || applyRect.Height == 0)
			{
				return;
			}
			var state = graphics.Save();
			if (Invert)
			{
				graphics.SetClip(applyRect);
				graphics.ExcludeClip(rect);
			}
			using (var fastBitmap = FastBitmapFactory.CreateCloneOf(applyBitmap, area: applyRect))
			{
				fastBitmap.ApplyBoxBlur(blurRadius);
				fastBitmap.DrawTo(graphics, applyRect);
			}
			graphics.Restore(state);
		}
	}
}