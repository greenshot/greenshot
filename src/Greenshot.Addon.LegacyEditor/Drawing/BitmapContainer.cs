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
using System.IO;
using System.Runtime.Serialization;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;
using Greenshot.Gfx.Effects;

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     Description of BitmapContainer.
	/// </summary>
	[Serializable]
	public class BitmapContainer : DrawableContainer, IBitmapContainer
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     This is the shadow version of the bitmap, rendered once to save performance
		///     Do not serialize, as the shadow is recreated from the original bitmap if it's not available
		/// </summary>
		[NonSerialized]
        private IBitmapWithNativeSupport _shadowBitmap;

		/// <summary>
		///     This is the offset for the shadow version of the bitmap
		///     Do not serialize, as the offset is recreated
		/// </summary>
		[NonSerialized]
        private NativePoint _shadowOffset = new NativePoint(-1, -1);

		private IBitmapWithNativeSupport _bitmap;

		public BitmapContainer(Surface parent, string filename, IEditorConfiguration editorConfiguration) : this(parent, editorConfiguration)
		{
			Load(filename);
		}

		public BitmapContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			FieldChanged += BitmapContainer_OnFieldChanged;
			Init();
		}

		public override bool HasDefaultSize => true;

		public override Size DefaultSize => _bitmap.Size;

		public IBitmapWithNativeSupport Bitmap
		{
			set
			{
				// Remove all current bitmaps
				DisposeImage();
				DisposeShadow();
				_bitmap = value.CloneBitmap();
				var shadow = GetFieldValueAsBool(FieldTypes.SHADOW);
				CheckShadow(shadow);
				if (!shadow)
				{
					Width = _bitmap.Width;
					Height = _bitmap.Height;
				}
				else
				{
					Width = _shadowBitmap.Width;
					Height = _shadowBitmap.Height;
					Left = Left - _shadowOffset.X;
					Top = Top - _shadowOffset.Y;
				}
			}
			get { return _bitmap; }
		}


		/// <summary>
		///     Make sure the content is also transformed.
		/// </summary>
		/// <param name="matrix"></param>
		public override void Transform(Matrix matrix)
		{
			var rotateAngle = CalculateAngle(matrix);
			// we currently assume only one transformation has been made.
			if (rotateAngle != 0)
			{
				Log.Debug().WriteLine("Rotating element with {0} degrees.", rotateAngle);
				DisposeShadow();
				using (var tmpMatrix = new Matrix())
				{
					using (_bitmap)
					{
						_bitmap = _bitmap.ApplyEffect(new RotateEffect(rotateAngle), tmpMatrix);
					}
				}
			}
			base.Transform(matrix);
		}

		/// <summary>
		/// </summary>
		/// <param name="filename"></param>
		public void Load(string filename)
		{
			if (!File.Exists(filename))
			{
				return;
			}
			// Always make sure BitmapHelper.LoadBitmap results are disposed some time,
			// as we close the bitmap internally, we need to do it afterwards
			using (var tmpImage = BitmapHelper.LoadBitmap(filename))
			{
				Bitmap = tmpImage;
			}
			Log.Debug().WriteLine("Loaded file: " + filename + " with resolution: " + Height + "," + Width);
		}

		protected override void OnDeserialized(StreamingContext streamingContext)
		{
			base.OnDeserialized(streamingContext);
			Init();
		}

		private void Init()
		{
			CreateDefaultAdorners();
		}

		protected override void InitializeFields()
		{
			AddField(GetType(), FieldTypes.SHADOW, false);
		}

		protected void BitmapContainer_OnFieldChanged(object sender, FieldChangedEventArgs e)
		{
		    if (!sender.Equals(this))
		    {
		        return;
		    }

		    if (FieldTypes.SHADOW.Equals(e.Field.FieldType))
		    {
		        ChangeShadowField();
		    }
		}

		public void ChangeShadowField()
		{
			var shadow = GetFieldValueAsBool(FieldTypes.SHADOW);
			if (shadow)
			{
				CheckShadow(true);
				Width = _shadowBitmap.Width;
				Height = _shadowBitmap.Height;
				Left = Left - _shadowOffset.X;
				Top = Top - _shadowOffset.Y;
			}
			else
			{
				Width = _bitmap.Width;
				Height = _bitmap.Height;
				if (_shadowBitmap != null)
				{
					Left = Left + _shadowOffset.X;
					Top = Top + _shadowOffset.Y;
				}
			}
		}

		/// <summary>
		///     The bulk of the clean-up code is implemented in Dispose(bool)
		///     This Dispose is called from the Dispose and the Destructor.
		///     When disposing==true all non-managed resources should be freed too!
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
			_bitmap?.Dispose();
			_bitmap = null;
		}

		private void DisposeShadow()
		{
			_shadowBitmap?.Dispose();
			_shadowBitmap = null;
		}

		/// <summary>
		///     This checks if a shadow is already generated
		/// </summary>
		/// <param name="shadow"></param>
		private void CheckShadow(bool shadow)
		{
			if (shadow && _shadowBitmap == null)
			{
				using (var matrix = new Matrix())
				{
					_shadowBitmap = _bitmap.ApplyEffect(new DropShadowEffect(), matrix);
				}
			}
		}

		/// <summary>
		///     Draw the actual container to the graphics object
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="rm"></param>
		public override void Draw(Graphics graphics, RenderMode rm)
		{
			if (_bitmap != null)
			{
				var shadow = GetFieldValueAsBool(FieldTypes.SHADOW);
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				if (shadow)
				{
					CheckShadow(true);
					graphics.DrawImage(_shadowBitmap.NativeBitmap, Bounds);
				}
				else
				{
					graphics.DrawImage(_bitmap.NativeBitmap, Bounds);
				}
			}
		}
	}
}