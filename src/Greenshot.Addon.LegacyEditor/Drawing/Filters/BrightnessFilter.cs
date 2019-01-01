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
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;
using Greenshot.Gfx.Effects;

#endregion

namespace Greenshot.Addon.LegacyEditor.Drawing.Filters
{
	[Serializable]
	public class BrightnessFilter : AbstractFilter
	{
		public BrightnessFilter(DrawableContainer parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			AddField(GetType(), FieldTypes.BRIGHTNESS, 0.9d);
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

			var state = graphics.Save();
			if (Invert)
			{
				graphics.SetClip(applyRect);
				graphics.ExcludeClip(rect);
			}
			var brightness = GetFieldValueAsFloat(FieldTypes.BRIGHTNESS);
			using (var ia = AdjustEffect.CreateAdjustAttributes(brightness, 1f, 1f))
			{
				graphics.DrawImage(applyBitmap, applyRect, applyRect.X, applyRect.Y, applyRect.Width, applyRect.Height, GraphicsUnit.Pixel, ia);
			}
			graphics.Restore(state);
		}
	}
}