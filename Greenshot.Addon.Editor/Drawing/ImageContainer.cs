/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.Drawing.Drawing2D;
using System.IO;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces.Drawing;
using Dapplo.LogFacade;

namespace Greenshot.Addon.Editor.Drawing
{
	/// <summary>
	/// Description of BitmapContainer.
	/// </summary>
	[Serializable]
	public class ImageContainer : DrawableContainer, IImageContainer
	{
		private static readonly LogSource Log = new LogSource();

		protected bool _shadow = true;

		[Field(FieldTypes.SHADOW)]
		public bool Shadow
		{
			get
			{
				return _shadow;
			}
			set
			{
				_shadow = value;
				OnFieldPropertyChanged(FieldTypes.SHADOW);
				ChangeShadowField();
			}
		}

		private Image _image;

		/// <summary>
		/// This is the _shadow version of the bitmap, rendered once to save performance
		/// Do not serialize, as the _shadow is recreated from the original bitmap if it's not available
		/// </summary>
		[NonSerialized]
		private Image _shadowBitmap;

		/// <summary>
		/// This is the offset for the _shadow version of the bitmap
		/// Do not serialize, as the offset is recreated
		/// </summary>
		[NonSerialized]
		private Point _shadowOffset = new Point(-1, -1);

		public ImageContainer(Surface parent, string filename) : this(parent)
		{
			Load(filename);
		}

		public ImageContainer(Surface parent) : base(parent)
		{
		}

		public void ChangeShadowField()
		{
			if (_shadow)
			{
				CheckShadow(_shadow);
				if (_shadowBitmap != null)
				{
					Width = _shadowBitmap.Width;
					Height = _shadowBitmap.Height;
				}
				Left = Left - _shadowOffset.X;
				Top = Top - _shadowOffset.Y;
			}
			else if (_image != null)
			{
				Width = _image.Width;
				Height = _image.Height;
				if (_shadowBitmap != null)
				{
					Left = Left + _shadowOffset.X;
					Top = Top + _shadowOffset.Y;
				}
			}
		}

		public Image Image
		{
			set
			{
				// Remove all current bitmaps
				DisposeImage();
				DisposeShadow();
				_image = ImageHelper.Clone(value);
				CheckShadow(_shadow);
				if (!_shadow)
				{
					Width = _image.Width;
					Height = _image.Height;
				}
				else if (_shadowBitmap != null)
				{
					Width = _shadowBitmap.Width;
					Height = _shadowBitmap.Height;
					Left = Left - _shadowOffset.X;
					Top = Top - _shadowOffset.Y;
				}
			}
			get
			{
				return _image;
			}
		}

		/// <summary>
		/// The bulk of the clean-up code is implemented in Dispose(bool)
		/// This Dispose is called from the Dispose and the Destructor.
		/// When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				DisposeImage();
				DisposeShadow();
			}
			base.Dispose(disposing);
		}

		private void DisposeImage()
		{
			if (_image != null)
			{
				_image.Dispose();
			}
			_image = null;
		}

		private void DisposeShadow()
		{
			if (_shadowBitmap != null)
			{
				_shadowBitmap.Dispose();
			}
			_shadowBitmap = null;
		}

		/// <summary>
		/// Make sure the content is also transformed.
		/// </summary>
		/// <param name="matrix"></param>
		public override void Transform(Matrix matrix)
		{
			int rotateAngle = CalculateAngle(matrix);
			// we currently assume only one transformation has been made.
			if (rotateAngle != 0)
			{
				Log.Debug().WriteLine("Rotating element with {0} degrees.", rotateAngle);
				DisposeShadow();
				using (var tmpMatrix = new Matrix())
				{
					using (Image tmpImage = _image)
					{
						_image = ImageHelper.ApplyEffect(_image, new RotateEffect(rotateAngle), tmpMatrix);
					}
				}
			}
			base.Transform(matrix);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filename"></param>
		public void Load(string filename)
		{
			if (File.Exists(filename))
			{
				// Always make sure ImageHelper.LoadBitmap results are disposed some time,
				// as we close the bitmap internally, we need to do it afterwards
				using (Image tmpImage = ImageHelper.LoadImage(filename))
				{
					Image = tmpImage;
				}
				Log.Debug().WriteLine("Loaded file: {0} with resolution: {1},{2}", filename, Height, Width);
			}
		}


		/// <summary>
		/// This checks if a _shadow is already generated
		/// </summary>
		/// <param name="_shadow"></param>
		private void CheckShadow(bool shadow)
		{
			if (shadow && _shadowBitmap == null && _image != null)
			{
				using (var matrix = new Matrix())
				{
					_shadowBitmap = ImageHelper.ApplyEffect(_image, new DropShadowEffect(), matrix);
				}
			}
		}

		/// <summary>
		/// Draw the actual container to the graphics object
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="rm"></param>
		public override void Draw(Graphics graphics, RenderMode rm)
		{
			if (_image != null)
			{
				GraphicsState state = graphics.Save();
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				if (_shadow)
				{
					CheckShadow(_shadow);
					graphics.DrawImage(_shadowBitmap, Bounds);
				}
				else
				{
					graphics.DrawImage(_image, Bounds);
				}
				graphics.Restore(state);
			}
		}

		public override bool HasDefaultSize
		{
			get
			{
				return true;
			}
		}

		public override Size DefaultSize
		{
			get
			{
				return _image.Size;
			}
		}
	}
}