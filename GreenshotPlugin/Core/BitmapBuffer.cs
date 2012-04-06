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
using System.Runtime.Serialization;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// The BitmapBuffer is exactly what it says, it buffers a Bitmap.
	/// And it is possible to Draw on the Bitmap with direct memory access for better performance
	/// </summary>
	[Serializable()] 
	public unsafe class BitmapBuffer : IDisposable {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BitmapBuffer));
		private bool clone;
		private Bitmap bitmap;

		public static Color BackgroundBlendColor {
			get;
			set;
		}

		static BitmapBuffer() {
			BackgroundBlendColor = Color.White;
		}

		/// <summary>
		/// Get the bitmap, you will always need to dispose the returned bitmap!!
		/// Only works, and makes sense, if cloned and not locked!
		/// </summary>
		public Bitmap Bitmap {
			get {
				if (bitsLocked) {
					throw new NotSupportedException("Can't get a locked bitmap!");
				}
				if (!clone) {
					throw new NotSupportedException("Can't return a not cloned bitmap!");
				} else {
					// Make sure the bitmap isn't disposed when this object is closed
					clone = false;
					return bitmap;
				}
			}
		}

		[NonSerialized]
		private BitmapData bmData;
		[NonSerialized]
		private Rectangle rect;
		[NonSerialized]
		private byte* pointer;
		[NonSerialized]
		private int stride; /* bytes per pixel row */
		[NonSerialized]
		private int aIndex = -1;
		[NonSerialized]
		private int rIndex = -1;
		[NonSerialized]
		private int gIndex = -1;
		[NonSerialized]
		private int bIndex = -1;
		[NonSerialized]
		private int bytesPerPixel;
		[NonSerialized]
		private bool bitsLocked = false;
		
		public Size Size {
			get {return rect.Size;}
		}
		public int Length {
			get {return rect.Width*rect.Height;}
		}
		public int Width {
			get {return rect.Width;}
		}
		public int Height {
			get {return rect.Height;}
		}
		
		/// <summary>
		/// Create a BitmapBuffer from a Bitmap
		/// </summary>
		/// <param name="sourceBmp">Bitmap</param>
		public BitmapBuffer(Bitmap bmp) : this(bmp, Rectangle.Empty) {
		}

		/// <summary>
		/// Create a BitmapBuffer from a Bitmap a flag if we need a clone
		/// </summary>
		/// <param name="sourceBmp">Bitmap</param>
		/// <param name="clone">bool specifying if the bitmap needs to be cloned</param>
		public BitmapBuffer(Bitmap sourceBmp, bool clone) : this(sourceBmp, Rectangle.Empty, clone) {
		}

		/// <summary>
		/// Create a BitmapBuffer from a Bitmap and a Rectangle specifying what part from the Bitmap to take.
		/// </summary>
		/// <param name="sourceBmp">Bitmap</param>
		/// <param name="applyRect">Rectangle</param>
		public BitmapBuffer(Bitmap sourceBmp, Rectangle applyRect) : this(sourceBmp, applyRect, true) {
		}

		/// <summary>
		/// Create a BitmapBuffer from a Bitmap, a Rectangle specifying what part from the Bitmap to take and a flag if we need a clone
		/// </summary>
		/// <param name="sourceBmp">Bitmap</param>
		/// <param name="applyRect">Rectangle</param>
		/// <param name="clone">bool specifying if the bitmap needs to be cloned</param>
		public BitmapBuffer(Bitmap sourceBmp, Rectangle applyRect, bool clone) {
			this.clone = clone;
			Rectangle sourceRect = new Rectangle(applyRect.X, applyRect.Y, applyRect.Width, applyRect.Height);
			Rectangle bitmapRect = new Rectangle(0,0, sourceBmp.Width, sourceBmp.Height);
			
			if(sourceRect.IsEmpty) {
				sourceRect = bitmapRect;
			} else {
				sourceRect.Intersect(bitmapRect);
			}
			// Does the rect have any pixels?
			if (sourceRect.Height <= 0 || sourceRect.Width <= 0) {
				return;
			}

			if (clone) {
				this.bitmap = ImageHelper.CloneArea(sourceBmp, sourceRect, PixelFormat.DontCare);
				// Set "this" rect to location 0,0
				// as the Cloned Bitmap is only the part we want to work with
				this.rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			} else if (!ImageHelper.SupportsPixelFormat(sourceBmp) && PixelFormat.Format8bppIndexed != sourceBmp.PixelFormat) {
				throw new ArgumentException("Unsupported pixel format: " + sourceBmp.PixelFormat + " set clone to true!");
			} else {
				this.bitmap = sourceBmp;
				this.rect = sourceRect;
			}
		}

		/**
		 * Destructor
		 */
		~BitmapBuffer() {
			Dispose(false);
		}

		/**
		 * The public accessible Dispose
		 * Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		 */
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/**
		 * This Dispose is called from the Dispose and the Destructor.
		 * When disposing==true all non-managed resources should be freed too!
		 */
		protected virtual void Dispose(bool disposing) {
			Unlock();
			if (disposing) {
				if (bitmap != null && clone) {
					bitmap.Dispose();
				}
			}
			bitmap = null;
			bmData = null;
			pointer = null;
		}
		
		/**
		 * This is called when deserializing the object
		 */
		public BitmapBuffer(SerializationInfo info, StreamingContext ctxt) {
			this.bitmap = (Bitmap)info.GetValue("bitmap", typeof(Bitmap));
			this.rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			// The rest will be set when Lock is called
		}

		/**
		 * This is called when serializing the object
		 */
		public void GetObjectData(SerializationInfo info, StreamingContext ctxt) {
			bool isLocked = bitsLocked;
			if (isLocked) {
				Unlock();
			}
			info.AddValue("bitmap", this.bitmap);
			if (isLocked) {
				Lock();
			}
		}

		/**
		 * Lock the bitmap so we have direct access to the memory
		 */
		public void Lock() {
			if(rect.Width > 0 && rect.Height > 0 && !bitsLocked) {
				bmData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat); 
				bitsLocked = true;
	
				IntPtr Scan0 = bmData.Scan0; 
				pointer = (byte*)(void*)Scan0; 

				PrepareForPixelFormat();
				stride = bmData.Stride;
			} 
		}

		/**
		 * Unlock the System Memory
		 */
		public void Unlock() {
			if (bitsLocked) {
				bitmap.UnlockBits(bmData); 
				bitsLocked = false;
			}
		}
		
		/**
		 * Draw the stored bitmap to the destionation bitmap at the supplied point
		 */
		public void DrawTo(Graphics graphics, Point destination) {
			DrawTo(graphics, null, destination);
		}
		
		/**
		 * Draw the stored Bitmap on the Destination bitmap with the specified rectangle
		 * Be aware that the stored bitmap will be resized to the specified rectangle!!
		 */
		public void DrawTo(Graphics graphics, Rectangle destinationRect) {
			DrawTo(graphics, destinationRect, null);
		}

		/**
		 * private helper to draw the bitmap
		 */
		private void DrawTo(Graphics graphics, Rectangle? destinationRect, Point? destination) {
			if (destinationRect.HasValue) {
				// Does the rect have any pixels?
				if (destinationRect.Value.Height <= 0 || destinationRect.Value.Width <= 0) {
					return;
				}
			}
			// Make sure this.bitmap is unlocked, if it was locked
			bool isLocked = bitsLocked;
			if (isLocked) {
				Unlock();
			}

			if (destinationRect.HasValue) {
				graphics.DrawImage(this.bitmap, destinationRect.Value);
			} else if (destination.HasValue) {
				graphics.DrawImageUnscaled(this.bitmap, destination.Value);
			}
			// If it was locked, lock it again
			if (isLocked) {
				Lock();
			}
		}

		/// <summary>
		/// Retrieve the color index, for 8BPP, at location x,y
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y Coordinate</param>
		/// <returns>index</returns>
		public byte GetColorIndexAt(int x, int y) {
			int offset = x*bytesPerPixel+y*stride;
			return pointer[offset];
		}

		/// <summary>
		/// Set the color index, for 8BPP, at location x,y
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y Coordinate</param>
		/// <param name="color">Color index to set</param>
		public void SetColorIndexAt(int x, int y, byte color) {
			int offset = x * bytesPerPixel + y * stride;
			pointer[offset] = color;
		}

		/// <summary>
		/// Retrieve the color at location x,y
		/// Before the first time this is called the Lock() should be called once!
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y Coordinate</param>
		/// <returns>Color</returns>
		public Color GetColorAt(int x, int y) {
			if(x>=0 && y>=0 && x<rect.Width && y<rect.Height) {
				int offset = x*bytesPerPixel+y*stride;
				int a = (aIndex==-1) ? 255 : (int)pointer[aIndex+offset];
				return Color.FromArgb(a, pointer[rIndex+offset], pointer[gIndex+offset], pointer[bIndex+offset]);
			} else {
				return Color.Empty;
			}
		}

		/// <summary>
		/// Retrieve the color, without alpha (is blended), at location x,y
		/// Before the first time this is called the Lock() should be called once!
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y Coordinate</param>
		/// <returns>Color</returns>
		public Color GetColorAtWithoutAlpha(int x, int y) {
			if (x >= 0 && y >= 0 && x < rect.Width && y < rect.Height) {
				int offset = x * bytesPerPixel + y * stride;
				int a = (aIndex == -1) ? (byte)255 : pointer[aIndex + offset];
				int red = pointer[rIndex + offset];
				int green = pointer[gIndex + offset];
				int blue = pointer[bIndex + offset];

				if (a < 255) {
					int rem = 255 - a;
					red = (red * a + BackgroundBlendColor.R * rem) / 255;
					green = (green * a + BackgroundBlendColor.G * rem) / 255;
					blue = (blue * a + BackgroundBlendColor.B * rem) / 255;
				}
				return Color.FromArgb(255, red, green, blue);
			} else {
				return Color.Empty;
			}
		}

		/**
		 * Set the color at location x,y
		 * Before the first time this is called the Lock() should be called once!
		 */				
		public void SetColorAt(int x, int y, Color color) {
			if(x>=0 && y>=0 && x<rect.Width && y<rect.Height) {
				int offset = x*bytesPerPixel+y*stride;
				if(aIndex!=-1) pointer[aIndex+offset] = color.A;
				pointer[rIndex+offset] = color.R;
				pointer[gIndex+offset] = color.G;
				pointer[bIndex+offset] = color.B;
			}
		}

		/**
		 * Retrieve the color at location x,y as an array
		 * Before the first time this is called the Lock() should be called once!
		 */
		public byte[] GetColorArrayAt(int x, int y) {
			if(x>=0 && y>=0 && x<rect.Width && y<rect.Height) {
				int offset = x*bytesPerPixel+y*stride;
				byte a = (aIndex==-1) ? (byte)255 : (byte)pointer[aIndex+offset];
				return new byte[] { a, pointer[rIndex + offset], pointer[gIndex + offset], pointer[bIndex + offset] };
			} else {
				return new byte[] { 0, 0, 0, 0 };
			}
		}

		/**
		 * Retrieve the color at location x,y to a byte[]
		 * Before the first time this is called the Lock() should be called once!
		 */
		public void GetColorIn(int x, int y, byte[] color) {
			if (x >= 0 && y >= 0 && x < rect.Width && y < rect.Height) {
				int offset = x * bytesPerPixel + y * stride;
				color[0] = (aIndex == -1) ? (byte)255 : (byte)pointer[aIndex + offset];
				color[1] = pointer[rIndex + offset];
				color[2] = pointer[gIndex + offset];
				color[3] = pointer[bIndex + offset];
			} else {
				color[0] = 0;
				color[1] = 0;
				color[2] = 0;
				color[3] = 0;
			}
		}

		/**
		 * Set the color at location x,y as an array
		 * Before the first time this is called the Lock() should be called once!
		 */
		public void SetColorArrayAt(int x, int y, byte[] colors) {
			if(x>=0 && y>=0 && x<rect.Width && y<rect.Height) {
				int offset = x*bytesPerPixel+y*stride;
				if(aIndex!=-1) pointer[aIndex+offset] = (byte)colors[0];
				pointer[rIndex+offset] = (byte)colors[1];
				pointer[gIndex+offset] = (byte)colors[2];
				pointer[bIndex+offset] = (byte)colors[3];
			}
		}
		/**
		 * Set the color at location x,y as an array
		 * Before the first time this is called the Lock() should be called once!
		 */
		public void SetUncheckedColorArrayAt(int x, int y, byte[] colors) {
			int offset = x * bytesPerPixel + y * stride;
			if (aIndex != -1) pointer[aIndex + offset] = (byte)colors[0];
			pointer[rIndex + offset] = (byte)colors[1];
			pointer[gIndex + offset] = (byte)colors[2];
			pointer[bIndex + offset] = (byte)colors[3];
		}
		/**
		 * Set some internal values for accessing the bitmap according to the PixelFormat
		 */
		private void PrepareForPixelFormat() {
			// aIndex is only set if the pixel format supports "A".
			aIndex = -1;
			switch(bitmap.PixelFormat) {
				case PixelFormat.Format32bppArgb: 
					bIndex = 0;
					gIndex = 1;
					rIndex = 2;
					aIndex = 3;
					bytesPerPixel = 4;
					break;
				case PixelFormat.Format32bppRgb: 
					bIndex = 0;
					gIndex = 1;
					rIndex = 2;
					bytesPerPixel = 4;
					break;
				case PixelFormat.Format24bppRgb: 
					bIndex = 0;
					gIndex = 1;
					rIndex = 2;
					bytesPerPixel = 3;
					break;
				case PixelFormat.Format8bppIndexed:
					bytesPerPixel = 1;
					break;
				default: 
					throw new FormatException("Bitmap.Pixelformat."+bitmap.PixelFormat+" is currently not supported. Supported: Format32bpp(A)Rgb, Format24bppRgb");
			}
		}
	}
}
