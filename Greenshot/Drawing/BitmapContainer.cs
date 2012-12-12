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
using Greenshot.Core;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of BitmapContainer.
	/// </summary>
	[Serializable()] 
	public class BitmapContainer : DrawableContainer, IBitmapContainer {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BitmapContainer));

		private Bitmap bitmap;

		/// <summary>
		/// This is the shadow version of the bitmap, rendered once to save performance
		/// Do not serialize, as the shadow is recreated from the original bitmap if it's not available
		/// </summary>
		[NonSerialized]
		private Bitmap shadowBitmap = null;

		/// <summary>
		/// This is the offset for the shadow version of the bitmap
		/// Do not serialize, as the offset is recreated
		/// </summary>
		[NonSerialized]
		private Point shadowOffset = new Point(-1, -1);

		public BitmapContainer(Surface parent, string filename) : this(parent) {
			Load(filename);
		}

		public BitmapContainer(Surface parent) : base(parent) {
			AddField(GetType(), FieldType.SHADOW, false);
			FieldChanged += BitmapContainer_OnFieldChanged;
		}

		protected void BitmapContainer_OnFieldChanged(object sender, FieldChangedEventArgs e) {
			if (sender.Equals(this)) {
				if (e.Field.FieldType == FieldType.SHADOW) {
					ChangeShadowField();
				}
			}
		}

		public void ChangeShadowField() {
			bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
			if (shadow) {
				CheckShadow(shadow);
				this.Width = shadowBitmap.Width;
				this.Height = shadowBitmap.Height;
				this.Left = this.Left - this.shadowOffset.X;
				this.Top = this.Top - this.shadowOffset.Y;
			} else {
				this.Width = bitmap.Width;
				this.Height = bitmap.Height;
				if (shadowBitmap != null) {
					this.Left = this.Left + this.shadowOffset.X;
					this.Top = this.Top + this.shadowOffset.Y;
				}
			}
		}

		public Bitmap Bitmap {
			set {
				// Remove all current bitmaps
				Dispose(true);
				bitmap = ImageHelper.Clone(value);
				bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
				CheckShadow(shadow);
				if (!shadow) {
					Width = bitmap.Width;
					Height = bitmap.Height;
				} else {
					Width = shadowBitmap.Width;
					Height = shadowBitmap.Height;
					this.Left = this.Left - this.shadowOffset.X;
					this.Top = this.Top - this.shadowOffset.Y;
				}
			}
			get { return bitmap; }
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~BitmapContainer() {
			Dispose(false);
		}

		/// <summary>
		/// The public accessible Dispose
		/// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public new void Dispose() {
			Dispose(true);
			base.Dispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// The bulk of the clean-up code is implemented in Dispose(bool)
		/// This Dispose is called from the Dispose and the Destructor.
		/// When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (bitmap != null) {
					bitmap.Dispose();
				}
				if (shadowBitmap != null) {
					shadowBitmap.Dispose();
				}
			}
			bitmap = null;
			shadowBitmap = null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filename"></param>
		public void Load(string filename) {
			if (File.Exists(filename)) {
				// Always make sure ImageHelper.LoadBitmap results are disposed some time,
				// as we close the bitmap internally, we need to do it afterwards
				using (Bitmap tmpImage = ImageHelper.LoadBitmap(filename)) {
					Bitmap = tmpImage;
				}
				LOG.Debug("Loaded file: " + filename + " with resolution: " + Height + "," + Width);
			}
		}

		/// <summary>
		/// Rotate the bitmap
		/// </summary>
		/// <param name="rotateFlipType"></param>
		public override void Rotate(RotateFlipType rotateFlipType) {
			Bitmap newBitmap = ImageHelper.RotateFlip((Bitmap)bitmap, rotateFlipType);
			if (newBitmap != null) {
				// Remove all current bitmaps, also the shadow (will be recreated)
				Dispose(true);
				bitmap = newBitmap;
			}
			base.Rotate(rotateFlipType);
		}

		/// <summary>
		/// This checks if a shadow is already generated
		/// </summary>
		/// <param name="shadow"></param>
		private void CheckShadow(bool shadow) {
			if (shadow && shadowBitmap == null) {
				shadowBitmap = ImageHelper.ApplyEffect(bitmap, new DropShadowEffect(), out shadowOffset);
			}
		}

		/// <summary>
		/// Draw the actual container to the graphics object
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="rm"></param>
		public override void Draw(Graphics graphics, RenderMode rm) {
			if (bitmap != null) {
				bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				if (shadow) {
					CheckShadow(shadow);
					graphics.DrawImage(shadowBitmap, Bounds);
				} else {
					graphics.DrawImage(bitmap, Bounds);
				}
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
