#region Greenshot GNU General Public License

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
using System.Drawing.Imaging;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addons.Core
{
	/// <summary>
	///     This class is used to pass an instance of the "Capture" around
	///     Having the Bitmap, eventually the Windows Title and cursor all together.
	/// </summary>
	public class Capture : ICapture
	{
		private static readonly LogSource Log = new LogSource();

		private CaptureDetails _captureDetails;

		private Icon _cursor;

		private NativePoint _cursorLocation = NativePoint.Empty;

		private Bitmap _bitmap;

		private NativeRect _screenBounds;

		/// <summary>
		///     Default Constructor
		/// </summary>
		public Capture()
		{
			_screenBounds = WindowCapture.GetScreenBounds();
			_captureDetails = new CaptureDetails();
		}

        /// <summary>
        ///     Constructor with Bitmap
        ///     Note: the supplied bitmap can be disposed immediately or when constructor is called.
        /// </summary>
        /// <param name="newBitmap">Bitmap</param>
        public Capture(Bitmap newBitmap) : this()
		{
			Bitmap = newBitmap;
		}

		/// <summary>
		///     Get/Set the Screenbounds
		/// </summary>
		public NativeRect ScreenBounds
		{
			get
			{
				if (_screenBounds == NativeRect.Empty)
				{
					_screenBounds = WindowCapture.GetScreenBounds();
				}
				return _screenBounds;
			}
			set { _screenBounds = value; }
		}

		/// <summary>
		///     Get/Set the Bitmap
		/// </summary>
		public Bitmap Bitmap
		{
			get => _bitmap;
		    set
			{
				_bitmap?.Dispose();
				_bitmap = value;
				if (value != null)
				{
					if (value.PixelFormat.Equals(PixelFormat.Format8bppIndexed) || value.PixelFormat.Equals(PixelFormat.Format1bppIndexed) ||
					    value.PixelFormat.Equals(PixelFormat.Format4bppIndexed))
					{
						Log.Debug().WriteLine("Converting Bitmap to PixelFormat.Format32bppArgb as we don't support: " + value.PixelFormat);
						try
						{
							// Default Bitmap PixelFormat is Format32bppArgb
							_bitmap = new Bitmap(value);
						}
						finally
						{
							// Always dispose, even when a exception occured
							value.Dispose();
						}
					}
					Log.Debug().WriteLine("Bitmap is set with the following specifications: {0} - {1}", _bitmap.Size, _bitmap.PixelFormat);
				}
				else
				{
					Log.Debug().WriteLine("Bitmap is removed.");
				}
			}
		}

		public void NullBitmap()
		{
			_bitmap = null;
		}

		/// <summary>
		///     Get/Set the image for the Cursor
		/// </summary>
		public Icon Cursor
		{
			get { return _cursor; }
			set
			{
				_cursor?.Dispose();
				_cursor = (Icon) value.Clone();
			}
		}

		/// <summary>
		///     Set if the cursor is visible
		/// </summary>
		public bool CursorVisible { get; set; }

		/// <summary>
		///     Get/Set the CursorLocation
		/// </summary>
		public NativePoint CursorLocation
		{
			get { return _cursorLocation; }
			set { _cursorLocation = value; }
		}

		/// <summary>
		///     Get/set the Location
		/// </summary>
		public NativePoint Location { get; set; } = NativePoint.Empty;

		/// <summary>
		///     Get/set the CaptureDetails
		/// </summary>
		public ICaptureDetails CaptureDetails
		{
			get { return _captureDetails; }
			set { _captureDetails = (CaptureDetails) value; }
		}

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
		///     Crops the capture to the specified rectangle (with Bitmap coordinates!)
		/// </summary>
		/// <param name="cropRectangle">NativeRect with bitmap coordinates</param>
		public bool Crop(NativeRect cropRectangle)
		{
			Log.Debug().WriteLine("Cropping to: " + cropRectangle);
			if (!BitmapHelper.Crop(ref _bitmap, ref cropRectangle))
			{
				return false;
			}
			Location = cropRectangle.Location;
			// Change mouse location according to the cropRegtangle (including screenbounds) offset
			MoveMouseLocation(-cropRectangle.Location.X, -cropRectangle.Location.Y);

			return true;
		}

		/// <summary>
		///     Apply a translate to the mouse location.
		///     e.g. needed for crop
		/// </summary>
		/// <param name="x">x coordinates to move the mouse</param>
		/// <param name="y">y coordinates to move the mouse</param>
		public void MoveMouseLocation(int x, int y)
		{
		    _cursorLocation = _cursorLocation.Offset(x, y);
		}

		/// <summary>
		///     Destructor
		/// </summary>
		~Capture()
		{
			Dispose(false);
		}

		/// <summary>
		///     This Dispose is called from the Dispose and the Destructor.
		///     When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				_bitmap?.Dispose();
				_cursor?.Dispose();
			}
			_bitmap = null;
			_cursor = null;
		}
	}
}