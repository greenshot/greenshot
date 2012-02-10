/// <summary>
/// Parts of this class were taken from BlurEffect.cs of Paint.NET 3.0.1, 
/// which was released under MIT license.
/// http://www.getpaint.net
/// Some of this code has been adapted for integration with Greenshot.
/// See Paint.NET copyright notice below.
/// </summary>

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using Greenshot.Drawing.Fields;
using Greenshot.Plugin.Drawing;
using GreenshotPlugin.Core;
using System.Drawing.Imaging;

namespace Greenshot.Drawing.Filters {
	[Serializable()] 
	public class BlurFilter : AbstractFilter {
		public double previewQuality;
		public double PreviewQuality {
			get { return previewQuality; }
			set { previewQuality = value; OnPropertyChanged("PreviewQuality"); }
		}
		
		public BlurFilter(DrawableContainer parent) : base(parent) {
			AddField(GetType(), FieldType.BLUR_RADIUS, 3);
			AddField(GetType(), FieldType.PREVIEW_QUALITY, 1.0d);
		}

		public unsafe override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
			int blurRadius = GetFieldValueAsInt(FieldType.BLUR_RADIUS);
			double previewQuality = GetFieldValueAsDouble(FieldType.PREVIEW_QUALITY);
			Rectangle applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);
			using (Bitmap blurImage = ImageHelper.CreateBlur(applyBitmap, applyRect, renderMode == RenderMode.EXPORT, blurRadius, previewQuality, Invert, parent.Bounds)) {
				if (blurImage != null) {
					graphics.DrawImageUnscaled(blurImage, applyRect.Location);
				}
			}
			return;
		}
	}
}
