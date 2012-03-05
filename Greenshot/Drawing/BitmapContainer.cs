/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

using Greenshot.Drawing.Fields;
using Greenshot.Helpers;
using Greenshot.Plugin.Drawing;
using GreenshotPlugin.Core;
using System.Drawing.Drawing2D;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of BitmapContainer.
	/// </summary>
	[Serializable()] 
	public class BitmapContainer : DrawableContainer, IBitmapContainer {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BitmapContainer));

		protected Bitmap bitmap;

		public BitmapContainer(Surface parent, string filename) : this(parent) {
			AddField(GetType(), FieldType.SHADOW, false);
			Load(filename);
		}

		public BitmapContainer(Surface parent) : base(parent) {
			AddField(GetType(), FieldType.SHADOW, false);
		}


		public Bitmap Bitmap {
			set {
				if (bitmap != null) {
					bitmap.Dispose();
				}
				bitmap = ImageHelper.Clone(value);
				Width = value.Width;
				Height = value.Height;
			}
			get { return bitmap; }
		}

		/**
		 * Destructor
		 */
		~BitmapContainer() {
			Dispose(false);
		}

		/**
		 * The public accessible Dispose
		 * Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		 */
		public new void Dispose() {
			Dispose(true);
			base.Dispose();
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/**
		 * This Dispose is called from the Dispose and the Destructor.
		 * When disposing==true all non-managed resources should be freed too!
		 */
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (bitmap != null) {
					bitmap.Dispose();
				}
			}
			bitmap = null;
		}

		public void Load(string filename) {
			if (File.Exists(filename)) {
				Bitmap = ImageHelper.LoadBitmap(filename);
				LOG.Debug("Loaded file: " + filename + " with resolution: " + Height + "," + Width);
			}
		}

		public override void Rotate(RotateFlipType rotateFlipType) {
			Bitmap newBitmap = ImageHelper.RotateFlip((Bitmap)bitmap, rotateFlipType);
			if (bitmap != null) {
				bitmap.Dispose();
			}
			bitmap = newBitmap;
			base.Rotate(rotateFlipType);
		}

		public override void Draw(Graphics graphics, RenderMode rm) {
			if (bitmap != null) {
				bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				if (shadow) {
					ImageAttributes ia = new ImageAttributes();
					ColorMatrix cm = new ColorMatrix();
					cm.Matrix00 = 0;
					cm.Matrix11 = 0;
					cm.Matrix22 = 0;
					cm.Matrix33 = 0.25f;
					ia.SetColorMatrix(cm);
					graphics.DrawImage(bitmap, new Rectangle(Bounds.Left + 2, Bounds.Top + 2, Bounds.Width, Bounds.Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, ia); 
				}
				graphics.DrawImage(bitmap, Bounds); 
			}
		}

		public override bool hasDefaultSize {
			get {
				return true;
			}
		}

		public override Size DefaultSize {
			get {
				return bitmap.Size;
			}
		}
	}
}
