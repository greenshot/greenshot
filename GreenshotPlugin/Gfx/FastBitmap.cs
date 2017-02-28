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
using System.Drawing.Imaging;

#endregion

namespace GreenshotPlugin.Gfx
{
	/// <summary>
	///     The base class for the fast bitmap implementation
	/// </summary>
	public abstract unsafe class FastBitmap : IFastBitmapWithClip, IFastBitmapWithOffset
	{
		protected const int PixelformatIndexA = 3;
		protected const int PixelformatIndexR = 2;
		protected const int PixelformatIndexG = 1;
		protected const int PixelformatIndexB = 0;

		public const int ColorIndexR = 0;
		public const int ColorIndexG = 1;
		public const int ColorIndexB = 2;
		public const int ColorIndexA = 3;

		private int _left;

		private int _top;

		protected Rectangle Area;

		/// <summary>
		///     The bitmap for which the FastBitmap is creating access
		/// </summary>
		protected Bitmap Bitmap;

		protected bool BitsLocked;

		protected BitmapData BmData;
		protected byte* Pointer;
		protected int Stride; /* bytes per pixel row */

		/// <summary>
		///     Constructor which stores the image and locks it when called
		/// </summary>
		/// <param name="bitmap">Bitmap</param>
		/// <param name="area">Rectangle</param>
		protected FastBitmap(Bitmap bitmap, Rectangle? area = null)
		{
			Bitmap = bitmap;
			var bitmapArea = new Rectangle(Point.Empty, bitmap.Size);
			if (area.HasValue)
			{
				area.Value.Intersect(bitmapArea);
				Area = area.Value;
			}
			else
			{
				Area = bitmapArea;
			}
			// As the lock takes care that only the specified area is made available we need to calculate the offset
			Left = Area.Left;
			Top = Area.Top;
			// Default cliping is done to the area without invert
			Clip = Area;
			InvertClip = false;
			// Always lock, so we don't need to do this ourselves
			Lock();
		}

		/// <summary>
		///     If this is set to true, the bitmap will be disposed when disposing the IFastBitmap
		/// </summary>
		public bool NeedsDispose { get; set; }

		public Rectangle Clip { get; set; }

		public bool InvertClip { get; set; }

		public void SetResolution(float horizontal, float vertical)
		{
			Bitmap.SetResolution(horizontal, vertical);
		}

		/// <summary>
		///     Return the size of the image
		/// </summary>
		public Size Size
		{
			get
			{
				if (Area == Rectangle.Empty)
				{
					return Bitmap.Size;
				}
				return Area.Size;
			}
		}

		/// <summary>
		///     Return the width of the image
		/// </summary>
		public int Width
		{
			get
			{
				if (Area == Rectangle.Empty)
				{
					return Bitmap.Width;
				}
				return Area.Width;
			}
		}

		/// <summary>
		///     Return the height of the image
		/// </summary>
		public int Height
		{
			get
			{
				if (Area == Rectangle.Empty)
				{
					return Bitmap.Height;
				}
				return Area.Height;
			}
		}

		/// <summary>
		///     Return the left of the fastbitmap, this is also used as an offset
		/// </summary>
		public int Left
		{
			get { return 0; }
			set { _left = value; }
		}

		/// <summary>
		///     Return the top of the fastbitmap, this is also used as an offset
		/// </summary>
		public int Top
		{
			get { return 0; }
			set { _top = value; }
		}

		/// <summary>
		///     Return the right of the fastbitmap
		/// </summary>
		public int Right => Left + Width;

		/// <summary>
		///     Return the bottom of the fastbitmap
		/// </summary>
		public int Bottom => Top + Height;

		/// <summary>
		///     Returns the underlying bitmap, unlocks it and prevents that it will be disposed
		/// </summary>
		public Bitmap UnlockAndReturnBitmap()
		{
			if (BitsLocked)
			{
				Unlock();
			}
			NeedsDispose = false;
			return Bitmap;
		}

		public virtual bool HasAlphaChannel => false;

		/// <summary>
		///     The public accessible Dispose
		///     Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///     Lock the bitmap so we have direct access to the memory
		/// </summary>
		public void Lock()
		{
			if (Width <= 0 || Height <= 0 || BitsLocked)
			{
				return;
			}
			BmData = Bitmap.LockBits(Area, ImageLockMode.ReadWrite, Bitmap.PixelFormat);
			BitsLocked = true;

			var scan0 = BmData.Scan0;
			Pointer = (byte*) (void*) scan0;
			Stride = BmData.Stride;
		}

		/// <summary>
		///     Unlock the System Memory
		/// </summary>
		public void Unlock()
		{
			if (BitsLocked)
			{
				Bitmap.UnlockBits(BmData);
				BitsLocked = false;
			}
		}

		/// <summary>
		///     Draw the stored bitmap to the destionation bitmap at the supplied point
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="destination"></param>
		public void DrawTo(Graphics graphics, Point destination)
		{
			DrawTo(graphics, new Rectangle(destination, Area.Size));
		}

		/// <summary>
		///     Draw the stored Bitmap on the Destination bitmap with the specified rectangle
		///     Be aware that the stored bitmap will be resized to the specified rectangle!!
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="destinationRect"></param>
		public void DrawTo(Graphics graphics, Rectangle destinationRect)
		{
			// Make sure this.bitmap is unlocked, if it was locked
			var isLocked = BitsLocked;
			if (isLocked)
			{
				Unlock();
			}

			graphics.DrawImage(Bitmap, destinationRect, Area, GraphicsUnit.Pixel);
		}

		/// <summary>
		///     returns true if x & y are inside the FastBitmap
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>true if x & y are inside the FastBitmap</returns>
		public bool Contains(int x, int y)
		{
			return Area.Contains(x - Left, y - Top);
		}

		/// <summary>
		///     Retrieve the color at x,y
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <returns>Color</returns>
		public abstract Color GetColorAt(int x, int y);

		/// <summary>
		///     Set the color at x,y
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">Color</param>
		public abstract void SetColorAt(int x, int y, Color color);

		/// <summary>
		///     Retrieve the color at x,y as byte[]
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">byte[] for the rgb values color</param>
		public abstract void GetColorAt(int x, int y, byte[] color);

		/// <summary>
		///     Sets the color at x,y as byte[]
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">byte[] for the rgb values color</param>
		public abstract void SetColorAt(int x, int y, byte[] color);

		/// <summary>
		///     Return the left of the fastbitmap, this is also used as an offset
		/// </summary>
		int IFastBitmapWithOffset.Left
		{
			get { return _left; }
			set { _left = value; }
		}

		/// <summary>
		///     Return the top of the fastbitmap, this is also used as an offset
		/// </summary>
		int IFastBitmapWithOffset.Top
		{
			get { return _top; }
			set { _top = value; }
		}

		/// <summary>
		///     Factory for creating a FastBitmap depending on the pixelformat of the source
		///     The supplied rectangle specifies the area for which the FastBitmap does its thing
		/// </summary>
		/// <param name="source">Bitmap to access</param>
		/// <param name="area">Rectangle which specifies the area to have access to, can be Rectangle.Empty for the whole image</param>
		/// <returns>IFastBitmap</returns>
		public static IFastBitmap Create(Image source, Rectangle? area = null)
		{
			switch (source.PixelFormat)
			{
				case PixelFormat.Format8bppIndexed:
					return new FastChunkyBitmap((Bitmap) source, area);
				case PixelFormat.Format24bppRgb:
					return new Fast24RgbBitmap((Bitmap) source, area);
				case PixelFormat.Format32bppRgb:
					return new Fast32RgbBitmap((Bitmap) source, area);
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppPArgb:
					return new Fast32ArgbBitmap((Bitmap) source, area);
				default:
					throw new NotSupportedException($"Not supported Pixelformat {source.PixelFormat}");
			}
		}

		/// <summary>
		///     Factory for creating a FastBitmap as a destination for the source
		/// </summary>
		/// <param name="source">Bitmap to clone</param>
		/// <param name="pixelFormat">Pixelformat of the cloned bitmap</param>
		/// <param name="area">Area of the bitmap to access, can be Rectangle.Empty for the whole</param>
		/// <returns>IFastBitmap</returns>
		public static IFastBitmap CreateCloneOf(Image source, PixelFormat pixelFormat = PixelFormat.DontCare, Rectangle? area = null)
		{
			var destination = source.CloneImage(pixelFormat, area) as Bitmap;
			var fastBitmap = Create(destination) as FastBitmap;
			if (fastBitmap == null)
			{
				return null;
			}
			fastBitmap.NeedsDispose = true;
			if (!area.HasValue)
			{
				return fastBitmap;
			}
			fastBitmap.Left = area.Value.Left;
			fastBitmap.Top = area.Value.Top;
			return fastBitmap;
		}

		/// <summary>
		///     Factory for creating a FastBitmap as a destination
		/// </summary>
		/// <param name="newSize">Size</param>
		/// <param name="pixelFormat">PixelFormat</param>
		/// <param name="backgroundColor">Color or null</param>
		/// <param name="horizontalResolution">float for horizontal DPI</param>
		/// <param name="verticalResolution">float for horizontal DPI</param>
		/// <returns>IFastBitmap</returns>
		public static IFastBitmap CreateEmpty(Size newSize, PixelFormat pixelFormat = PixelFormat.DontCare, Color? backgroundColor = null, float horizontalResolution = 96f,
			float verticalResolution = 96f)
		{
			var destination = ImageHelper.CreateEmpty(newSize.Width, newSize.Height, pixelFormat, backgroundColor, horizontalResolution, verticalResolution);
			var fastBitmap = Create(destination);
			fastBitmap.NeedsDispose = true;
			return fastBitmap;
		}

		/// <summary>
		///     Destructor
		/// </summary>
		~FastBitmap()
		{
			Dispose(false);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/// <summary>
		///     This Dispose is called from the Dispose and the Destructor.
		///     When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			Unlock();
			if (disposing)
			{
				if (Bitmap != null && NeedsDispose)
				{
					Bitmap.Dispose();
				}
			}
			Bitmap = null;
			BmData = null;
			Pointer = null;
		}

		#region IFastBitmapWithClip

		/// <summary>
		///     Test if the bitmap containt the specified coordinates
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <returns>true if the specified coordinates are within</returns>
		bool IFastBitmapWithClip.Contains(int x, int y)
		{
			var contains = Clip.Contains(x, y);
			if (InvertClip)
			{
				return !contains;
			}
			return contains;
		}

		/// <summary>
		///     Get the color at the specified location, if it's not clipped
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">byte array with the color information</param>
		void IFastBitmapWithClip.GetColorAt(int x, int y, byte[] color)
		{
			var contains = Clip.Contains(x, y);
			if (InvertClip && contains)
			{
				// TODO: Implement nearest
				return;
			}
			if (!InvertClip && !contains)
			{
				if (y < Clip.Top)
				{
					y = Clip.Top;
				}
				if (y >= Clip.Bottom)
				{
					y = Clip.Bottom-1;
				}
				if (x < Clip.Left)
				{
					x = Clip.Left;
				}
				if (x >= Clip.Right)
				{
					x = Clip.Right-1;
				}
			}
			GetColorAt(x, y, color);
		}

		/// <summary>
		///     Get the color at the specified location, if it's not clipped
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <returns>Color</returns>
		Color IFastBitmapWithClip.GetColorAt(int x, int y)
		{
			var contains = Clip.Contains(x, y);
			if (InvertClip && contains)
			{
				// TODO: Implement nearest
				if (HasAlphaChannel)
				{
					return Color.Transparent;
				}
				return Color.Black;
			}
			if (!InvertClip && !contains)
			{
				if (y < Clip.Top)
				{
					y = Clip.Top;
				}
				if (y >= Clip.Bottom)
				{
					y = Clip.Bottom-1;
				}
				if (x < Clip.Left)
				{
					x = Clip.Left;
				}
				if (x >= Clip.Right)
				{
					x = Clip.Right-1;
				}
			}
			return GetColorAt(x, y);
		}


		/// <summary>
		///     Set the color at the specified location, if it's not clipped
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">byte array with the color information</param>
		void IFastBitmapWithClip.SetColorAt(int x, int y, byte[] color)
		{
			var contains = Clip.Contains(x, y);
			if (InvertClip && contains || !InvertClip && !contains)
			{
				return;
			}
			SetColorAt(x, y, color);
		}

		/// <summary>
		///     Set the color at the specified location, if it's not clipped
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">byte array with the color information</param>
		void IFastBitmapWithClip.SetColorAt(int x, int y, Color color)
		{
			var contains = Clip.Contains(x, y);
			if (InvertClip && contains || !InvertClip && !contains)
			{
				return;
			}
			SetColorAt(x, y, color);
		}

		#endregion

		#region IFastBitmapWithOffset

		/// <summary>
		///     returns true if x & y are inside the FastBitmap
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>true if x & y are inside the FastBitmap</returns>
		bool IFastBitmapWithOffset.Contains(int x, int y)
		{
			return Area.Contains(x - Left, y - Top);
		}

		/// <summary>
		///     Get the color at the specified location
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <returns>Color</returns>
		Color IFastBitmapWithOffset.GetColorAt(int x, int y)
		{
			x -= _left;
			y -= _top;
			return GetColorAt(x, y);
		}

		/// <summary>
		///     Get the color at the specified location
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">byte array with the color information</param>
		void IFastBitmapWithOffset.GetColorAt(int x, int y, byte[] color)
		{
			x -= _left;
			y -= _top;
			GetColorAt(x, y, color);
		}

		/// <summary>
		///     Set the color at the specified location
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">byte array with the color information</param>
		void IFastBitmapWithOffset.SetColorAt(int x, int y, byte[] color)
		{
			x -= _left;
			y -= _top;
			SetColorAt(x, y, color);
		}

		/// <summary>
		///     Set the color at the specified location
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">Color</param>
		void IFastBitmapWithOffset.SetColorAt(int x, int y, Color color)
		{
			x -= _left;
			y -= _top;
			SetColorAt(x, y, color);
		}

		#endregion
	}
}