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
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.FastBitmap
{
	/// <summary>
	///     The base class for the fast bitmap implementation
	/// </summary>
	public abstract unsafe class FastBitmapBase : IFastBitmapWithClip, IFastBitmapWithOffset
	{
		/// <summary>
		/// Index of the alpha data
		/// </summary>
		protected const int PixelformatIndexA = 3;
        /// <summary>
        /// Index of the red data
        /// </summary>
		protected const int PixelformatIndexR = 2;
        /// <summary>
        /// Index of the green data
        /// </summary>
		protected const int PixelformatIndexG = 1;
        /// <summary>
        /// Index of the blue data
        /// </summary>
		protected const int PixelformatIndexB = 0;

        /// <summary>
        /// Index of the red color data
        /// </summary>
		public const int ColorIndexR = 0;
        /// <summary>
        /// Index of the green color data
        /// </summary>
		public const int ColorIndexG = 1;
        /// <summary>
        /// Index of the blue color data
        /// </summary>
		public const int ColorIndexB = 2;
        /// <summary>
        /// Index of the alpha color data
        /// </summary>
		public const int ColorIndexA = 3;

	    private const uint Seed = 0x9747b28c;

        /// <summary>
        /// Area which is covered by this fast bitmap
        /// </summary>
        protected NativeRect Area;

		/// <summary>
		///     The bitmap for which the FastBitmap is creating access
		/// </summary>
		protected IBitmapWithNativeSupport Bitmap;

		/// <summary>
		/// Are the bits already locked?
		/// </summary>
		protected bool BitsLocked;

		/// <summary>
		/// This contains the locked BitmapData
		/// </summary>
		protected BitmapData BmData;
		/// <summary>
		/// Pointer to image data
		/// </summary>
		protected byte* Pointer;
        /// <summary>
        /// bytes per pixel row
        /// </summary>
		protected int Stride;

		/// <summary>
		///     Constructor which stores the image and locks it when called
		/// </summary>
		/// <param name="bitmap">Bitmap</param>
		/// <param name="area">NativeRect</param>
		protected FastBitmapBase(IBitmapWithNativeSupport bitmap, NativeRect? area = null)
		{
			Bitmap = bitmap;
			var bitmapArea = new NativeRect(NativePoint.Empty, bitmap.Size);
			Area = area?.Intersect(bitmapArea) ?? bitmapArea;
			// As the lock takes care that only the specified area is made available we need to calculate the offset
			Left = Area.Left;
			Top = Area.Top;
			// Default clipping is done to the area without invert
			Clip = Area;
			InvertClip = false;
			// Always lock, so we don't need to do this ourselves
			Lock();
		}

		/// <inheritdoc />
		public bool NeedsDispose { get; set; }

        /// <inheritdoc />
        public NativeRect Clip { get; set; }

        /// <inheritdoc />
        public bool InvertClip { get; set; }

        /// <inheritdoc />
        public void SetResolution(float horizontal, float vertical)
		{
			Bitmap.NativeBitmap.SetResolution(horizontal, vertical);
		}

		/// <inheritdoc />
		public Size Size
		{
			get
			{
				if (Area.IsEmpty)
				{
					return Bitmap.Size;
				}
				return Area.Size;
			}
		}

		/// <inheritdoc />
		public int Width
		{
			get
			{
			    return Area.IsEmpty ? Bitmap.Width : Area.Width;
			}
		}

        /// <inheritdoc />
		public int Height
		{
			get
			{
			    return Area.IsEmpty ? Bitmap.Height : Area.Height;
			}
		}

        /// <inheritdoc />
        public int Left
		{
			get { return 0; }
			set { ((IFastBitmapWithOffset) this).Left = value; }
		}

        /// <inheritdoc />
        public int Top
		{
			get { return 0; }
			set { ((IFastBitmapWithOffset) this).Top = value; }
		}

        /// <inheritdoc />
        public int Right => Left + Width;

        /// <inheritdoc />
        public int Bottom => Top + Height;

        /// <inheritdoc />
        public IBitmapWithNativeSupport UnlockAndReturnBitmap()
		{
			if (BitsLocked)
			{
				Unlock();
			}
			NeedsDispose = false;
			return Bitmap;
		}

        /// <inheritdoc />
        public virtual bool HasAlphaChannel => false;

		/// <inheritdoc />
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
			BmData = Bitmap.NativeBitmap.LockBits(Area, ImageLockMode.ReadWrite, Bitmap.PixelFormat);
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
		    if (!BitsLocked)
		    {
		        return;
		    }
		    Bitmap.NativeBitmap.UnlockBits(BmData);
		    BitsLocked = false;
		}

        /// <summary>
        ///     Draw the stored bitmap to the destionation bitmap at the supplied point
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <param name="destination">destinationRect</param>
        public void DrawTo(Graphics graphics, NativePoint destination)
		{
			DrawTo(graphics, new NativeRect(destination, Area.Size));
		}

        /// <summary>
        ///     Draw the stored Bitmap on the Destination bitmap with the specified rectangle
        ///     Be aware that the stored bitmap will be resized to the specified rectangle!!
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <param name="destinationRect">destinationRect</param>
        public void DrawTo(Graphics graphics, NativeRect destinationRect)
		{
			// Make sure this.bitmap is unlocked, if it was locked
			var isLocked = BitsLocked;
			if (isLocked)
			{
				Unlock();
			}

			graphics.DrawImage(Bitmap.NativeBitmap, destinationRect, Area, GraphicsUnit.Pixel);
		}

	    /// <inheritdoc />
        public bool Contains(int x, int y)
		{
			return Area.Contains(x - Left, y - Top);
		}

	    /// <inheritdoc />
        public abstract Color GetColorAt(int x, int y);

	    /// <inheritdoc />
        public abstract void SetColorAt(int x, int y, ref Color color);

	    /// <inheritdoc />
        public abstract void GetColorAt(int x, int y, byte[] color, int colorIndex = 0);

	    /// <inheritdoc />
        public abstract void GetColorAt(int x, int y, byte* color, int colorIndex = 0);

	    /// <inheritdoc />
		public abstract void SetColorAt(int x, int y, byte[] color, int colorIndex = 0);

	    /// <inheritdoc />
        public abstract void SetColorAt(int x, int y, byte* color, int colorIndex = 0);

	    /// <inheritdoc />
        public abstract int BytesPerPixel { get; }

        /// <summary>
        ///     Return the left of the fastbitmap, this is also used as an offset
        /// </summary>
        int IFastBitmapWithOffset.Left { get; set; }

	    /// <summary>
		///     Return the top of the fastbitmap, this is also used as an offset
		/// </summary>
		int IFastBitmapWithOffset.Top { get; set; }

	    /// <summary>
		///     Destructor
		/// </summary>
		~FastBitmapBase()
		{
			Dispose(false);
		}

		/// <summary>
		///     This Dispose is called from the Dispose and the Destructor.
		///     When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			Unlock();
		    if (disposing && (Bitmap != null && NeedsDispose))
		    {
		        Bitmap.Dispose();
		    }
		    Bitmap = null;
			BmData = null;
			Pointer = null;
		}

        /// <summary>
        /// Calculate the the hash-code for a horizontal line
        /// </summary>
        /// <param name="y">int with y coordinate</param>
        /// <param name="right">optional x starting coordinate of the hash calculation</param>
        /// <param name="left">optional x ending coordinate of the hash calculation</param>
	    /// <returns>uint with the hash</returns>
        public uint HorizontalHash(int y, int? right = null, int? left = null)
	    {
	        var offset = (left ?? Left) * BytesPerPixel + y * Stride;
	        var length = (right ?? Right) - (left ?? Left) * BytesPerPixel;
            var hash = new Murmur3(Seed, (uint) length);

	        while (length >= 4)
	        {
                hash.AddBytes(Pointer[offset++], Pointer[offset++], Pointer[offset++], Pointer[offset++]);
	            length -= 4;
	        }
	        switch (length)
	        {
	            case 3:
	                hash.AddTrailingBytes(Pointer[offset++], Pointer[offset++], Pointer[offset]);
	                break;
	            case 2:
	                hash.AddTrailingBytes(Pointer[offset++], Pointer[offset]);
	                break;
	            case 1:
	                hash.AddTrailingBytes(Pointer[offset]);
	                break;
	        }
            return hash.CalculatedHash;
	    }

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

		/// <inheritdoc />
		void IFastBitmapWithClip.GetColorAt(int x, int y, byte[] color, int colorIndex)
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
			GetColorAt(x, y, color, colorIndex);
		}

	    /// <inheritdoc />
	    void IFastBitmapWithClip.GetColorAt(int x, int y, byte* color, int colorIndex)
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
	                y = Clip.Bottom - 1;
	            }
	            if (x < Clip.Left)
	            {
	                x = Clip.Left;
	            }
	            if (x >= Clip.Right)
	            {
	                x = Clip.Right - 1;
	            }
	        }
	        GetColorAt(x, y, color, colorIndex);
	    }

        /// <inheritdoc />
        Color IFastBitmapWithClip.GetColorAt(int x, int y)
		{
			var contains = Clip.Contains(x, y);
			if (InvertClip && contains)
			{
			    // TODO: Implement nearest
			    return HasAlphaChannel ? Color.Transparent : Color.Black;
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


		/// <inheritdoc />
		void IFastBitmapWithClip.SetColorAt(int x, int y, byte[] color, int colorIndex)
		{
			var contains = Clip.Contains(x, y);
			if (InvertClip && contains || !InvertClip && !contains)
			{
				return;
			}
			SetColorAt(x, y, color, colorIndex);
		}

	    /// <inheritdoc />
        void IFastBitmapWithClip.SetColorAt(int x, int y, ref Color color)
		{
			var contains = Clip.Contains(x, y);
			if (InvertClip && contains || !InvertClip && !contains)
			{
				return;
			}
			SetColorAt(x, y, ref color);
		}

	    /// <inheritdoc />
        void IFastBitmapWithClip.SetColorAt(int x, int y, byte* color, int colorIndex)
	    {
	        var contains = Clip.Contains(x, y);
	        if (InvertClip && contains || !InvertClip && !contains)
	        {
	            return;
	        }
	        SetColorAt(x, y, color, colorIndex);
	    }

        /// <summary>
        ///     returns true if x and y are inside the FastBitmap
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>true if x and y are inside the FastBitmap</returns>
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
			x -= ((IFastBitmapWithOffset) this).Left;
			y -= ((IFastBitmapWithOffset) this).Top;
			return GetColorAt(x, y);
		}

	    /// <inheritdoc />
        void IFastBitmapWithOffset.GetColorAt(int x, int y, byte[] color, int colorIndex)
		{
			x -= ((IFastBitmapWithOffset) this).Left;
			y -= ((IFastBitmapWithOffset) this).Top;
			GetColorAt(x, y, color, colorIndex);
		}


	    /// <inheritdoc />
	    void IFastBitmapWithOffset.GetColorAt(int x, int y, byte* color, int colorIndex)
	    {
	        x -= ((IFastBitmapWithOffset) this).Left;
	        y -= ((IFastBitmapWithOffset) this).Top;
	        GetColorAt(x, y, color, colorIndex);
	    }

        /// <inheritdoc />
        void IFastBitmapWithOffset.SetColorAt(int x, int y, byte* color, int colorIndex)
		{
			x -= ((IFastBitmapWithOffset) this).Left;
			y -= ((IFastBitmapWithOffset) this).Top;
			SetColorAt(x, y, color, colorIndex);
		}


	    /// <inheritdoc />
	    void IFastBitmapWithOffset.SetColorAt(int x, int y, byte[] color, int colorIndex)
	    {
	        x -= ((IFastBitmapWithOffset)this).Left;
	        y -= ((IFastBitmapWithOffset)this).Top;
	        SetColorAt(x, y, color, colorIndex);
	    }

        /// <inheritdoc />
        void IFastBitmapWithOffset.SetColorAt(int x, int y, ref Color color)
		{
			x -= ((IFastBitmapWithOffset) this).Left;
			y -= ((IFastBitmapWithOffset) this).Top;
			SetColorAt(x, y, ref color);
		}
    }
}