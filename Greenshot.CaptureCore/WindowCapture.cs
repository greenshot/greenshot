//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Structs;
using Greenshot.Core.Configuration;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.CaptureCore
{
	/// <summary>
	///     The Window Capture code
	/// </summary>
	public static class WindowCapture
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ICaptureConfiguration CaptureConfiguration = IniConfig.Current.GetSubSection<ICaptureConfiguration>();

		/// <summary>
		///     This method will capture the current Cursor by using User32 Code
		/// </summary>
		/// <returns>A Capture Object with the Mouse Cursor information in it.</returns>
		public static ICapture CaptureCursor(ICapture capture)
		{
			Log.Debug().WriteLine("Capturing the mouse cursor.");
			if (capture == null)
			{
				capture = new Capture();
			}
			int x, y;
			CURSORINFO cursorInfo = new CURSORINFO();
			cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
			if (User32.GetCursorInfo(out cursorInfo))
			{
				if ((cursorInfo.flags & CursorInfoFlags.CURSOR_SHOWING) == CursorInfoFlags.CURSOR_SHOWING)
				{
					using (var cursor = new Cursor(cursorInfo.hCursor))
					{
						capture.Cursor = cursor;
						var cursorLocation = User32.GetCursorLocation();
						// Allign cursor location to Bitmap coordinates (instead of Screen coordinates)
						x = (int) cursorLocation.X - cursor.HotSpot.X - capture.ScreenBounds.X;
						y = (int) cursorLocation.Y - cursor.HotSpot.Y - capture.ScreenBounds.Y;
						// Set the location
						capture.CursorLocation = new Point(x, y);
					}
				}
			}
			return capture;
		}

		/// <summary>
		///     This method will use User32 code to capture the specified captureBounds from the screen
		/// </summary>
		/// <param name="capture">ICapture where the captured Bitmap will be stored</param>
		/// <param name="captureBounds">Rectangle with the bounds to capture</param>
		/// <returns>A Capture Object with a part of the Screen as an Image</returns>
		public static ICapture CaptureRectangle(ICapture capture, Rectangle captureBounds)
		{
			if (capture == null)
			{
				capture = new Capture();
			}
			Image capturedImage = null;
			// If the CaptureHandler has a handle use this, otherwise use the CaptureRectangle here
			if (CaptureHandler.CaptureScreenRectangle != null)
			{
				try
				{
					capturedImage = CaptureHandler.CaptureScreenRectangle(captureBounds);
				}
				catch
				{
					// ignored
				}
			}
			// If no capture, use the normal screen capture
			if (capturedImage == null)
			{
				capturedImage = CaptureRectangle(captureBounds);
			}
			capture.Image = capturedImage;
			capture.Location = captureBounds.Location;
			return capture.Image == null ? null : capture;
		}

		/// <summary>
		///     This method will use User32 code to capture the specified captureBounds from the screen
		/// </summary>
		/// <param name="captureBounds">Rectangle with the bounds to capture</param>
		/// <returns>Bitmap which is captured from the screen at the location specified by the captureBounds</returns>
		public static Bitmap CaptureRectangle(Rectangle captureBounds)
		{
			Bitmap returnBitmap = null;
			if ((captureBounds.Height <= 0) || (captureBounds.Width <= 0))
			{
				Log.Warn().WriteLine("Nothing to capture, ignoring!");
				return null;
			}
			Log.Debug().WriteLine("CaptureRectangle Called!");

			// .NET GDI+ Solution, according to some post this has a GDI+ leak...
			// See http://connect.microsoft.com/VisualStudio/feedback/details/344752/gdi-object-leak-when-calling-graphics-copyfromscreen
			// Bitmap capturedBitmap = new Bitmap(captureBounds.Width, captureBounds.Height);
			// using (Graphics graphics = Graphics.FromImage(capturedBitmap)) {
			//	graphics.CopyFromScreen(captureBounds.Location, Point.Empty, captureBounds.Size, CopyPixelOperation.CaptureBlt);
			// }
			// capture.Image = capturedBitmap;
			// capture.Location = captureBounds.Location;

			using (var desktopDcHandle = SafeWindowDcHandle.FromDesktop())
			{
				if (desktopDcHandle.IsInvalid)
				{
					// Get Exception before the error is lost
					var exceptionToThrow = CreateCaptureException("desktopDCHandle", captureBounds);
					// throw exception
					throw exceptionToThrow;
				}

				// create a device context we can copy to
				using (var safeCompatibleDcHandle = Gdi32.CreateCompatibleDC(desktopDcHandle))
				{
					// Check if the device context is there, if not throw an error with as much info as possible!
					if (safeCompatibleDcHandle.IsInvalid)
					{
						// Get Exception before the error is lost
						var exceptionToThrow = CreateCaptureException("CreateCompatibleDC", captureBounds);
						// throw exception
						throw exceptionToThrow;
					}
					// Create BITMAPINFOHEADER for CreateDIBSection
					var bmi = new BITMAPINFOHEADER(captureBounds.Width, captureBounds.Height, 24);

					// Make sure the last error is set to 0
					Win32.SetLastError(0);

					// create a bitmap we can copy it to, using GetDeviceCaps to get the width/height
					IntPtr bits0; // not used for our purposes. It returns a pointer to the raw bits that make up the bitmap.
					using (var safeDibSectionHandle = Gdi32.CreateDIBSection(desktopDcHandle, ref bmi, BITMAPINFOHEADER.DIB_RGB_COLORS, out bits0, IntPtr.Zero, 0))
					{
						if (safeDibSectionHandle.IsInvalid)
						{
							// Get Exception before the error is lost
							var exceptionToThrow = CreateCaptureException("CreateDIBSection", captureBounds);
							exceptionToThrow.Data.Add("hdcDest", safeCompatibleDcHandle.DangerousGetHandle().ToInt32());
							exceptionToThrow.Data.Add("hdcSrc", desktopDcHandle.DangerousGetHandle().ToInt32());

							// Throw so people can report the problem
							throw exceptionToThrow;
						}
						// select the bitmap object and store the old handle
						using (safeCompatibleDcHandle.SelectObject(safeDibSectionHandle))
						{
							// bitblt over (make copy)
							Gdi32.BitBlt(safeCompatibleDcHandle, 0, 0, captureBounds.Width, captureBounds.Height, desktopDcHandle, captureBounds.X, captureBounds.Y, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
						}

						// get a .NET image object for it
						// A suggestion for the "A generic error occurred in GDI+." E_FAIL/0×80004005 error is to re-try...
						bool success = false;
						ExternalException exception = null;
						for (int i = 0; i < 3; i++)
						{
							try
							{
								// Collect all screens inside this capture
								var screensInsideCapture = new List<DisplayInfo>();
								foreach (var display in User32.AllDisplays())
								{
									if (display.BoundsRectangle.IntersectsWith(captureBounds))
									{
										screensInsideCapture.Add(display);
									}
								}
								// Check all all screens are of an equal size
								bool offscreenContent;
								using (Region captureRegion = new Region(captureBounds))
								{
									// Exclude every visible part
									foreach (var display in screensInsideCapture)
									{
										captureRegion.Exclude(display.BoundsRectangle);
									}
									// If the region is not empty, we have "offscreenContent"
									using (var screenGraphics = Graphics.FromHwnd(User32.GetDesktopWindow()))
									{
										offscreenContent = !captureRegion.IsEmpty(screenGraphics);
									}
								}
								// Check if we need to have a transparent background, needed for offscreen content
								if (offscreenContent)
								{
									using (var tmpBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle()))
									{
										// Create a new bitmap which has a transparent background
										returnBitmap = ImageHelper.CreateEmpty(tmpBitmap.Width, tmpBitmap.Height, PixelFormat.Format32bppArgb, Color.Transparent, tmpBitmap.HorizontalResolution, tmpBitmap.VerticalResolution);
										// Content will be copied here
										using (var graphics = Graphics.FromImage(returnBitmap))
										{
											// For all screens copy the content to the new bitmap
											foreach (var display in User32.AllDisplays())
											{
												var screenBounds = display.BoundsRectangle;
												// Make sure the bounds are offsetted to the capture bounds
												screenBounds.Offset(-captureBounds.X, -captureBounds.Y);
												graphics.DrawImage(tmpBitmap, screenBounds, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height, GraphicsUnit.Pixel);
											}
										}
									}
								}
								else
								{
									// All screens, which are inside the capture, are of equal size
									// assign image to Capture, the image will be disposed there..
									returnBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());
								}
								// We got through the capture without exception
								success = true;
								break;
							}
							catch (ExternalException ee)
							{
								Log.Warn().WriteLine(ee, "Problem getting bitmap at try {0} : ", i);
								exception = ee;
							}
						}
						if (!success)
						{
							Log.Error().WriteLine("Still couldn't create Bitmap!");
							if (exception != null)
							{
								throw exception;
							}
						}
					}
				}
			}
			return returnBitmap;
		}

		/// <summary>
		///     This method will use User32 code to capture the specified captureBounds from the screen
		/// </summary>
		/// <param name="capture">ICapture where the captured Bitmap will be stored</param>
		/// <param name="captureBounds">Rectangle with the bounds to capture</param>
		/// <returns>A Capture Object with a part of the Screen as an Image</returns>
		public static ICapture CaptureRectangleFromDesktopScreen(ICapture capture, Rectangle captureBounds)
		{
			if (capture == null)
			{
				capture = new Capture();
			}
			capture.Image = CaptureRectangle(captureBounds);
			capture.Location = captureBounds.Location;
			return capture.Image == null ? null : capture;
		}

		/// <summary>
		///     This method will call the CaptureRectangle with the screenbounds, therefor Capturing the whole screen.
		/// </summary>
		/// <returns>A Capture Object with the Screen as an Image</returns>
		public static ICapture CaptureScreen(ICapture capture)
		{
			if (capture == null)
			{
				capture = new Capture();
			}
			return CaptureRectangle(capture, capture.ScreenBounds);
		}

		/// <summary>
		///     Helper method to create an exception that might explain what is wrong while capturing
		/// </summary>
		/// <param name="method">string with current method</param>
		/// <param name="captureBounds">Rectangle of what we want to capture</param>
		/// <returns></returns>
		private static Exception CreateCaptureException(string method, Rectangle captureBounds)
		{
			Exception exceptionToThrow = User32.CreateWin32Exception(method);
			if (!captureBounds.IsEmpty)
			{
				exceptionToThrow.Data.Add("Height", captureBounds.Height);
				exceptionToThrow.Data.Add("Width", captureBounds.Width);
			}
			return exceptionToThrow;
		}

		/// <summary>
		///     Helper method to check if it is allowed to capture the process using DWM
		/// </summary>
		/// <param name="process">Process owning the window</param>
		/// <returns>true if it's allowed</returns>
		public static bool IsDwmAllowed(Process process)
		{
			if (process != null)
			{
				if ((CaptureConfiguration.NoDWMCaptureForProduct != null) && (CaptureConfiguration.NoDWMCaptureForProduct.Count > 0))
				{
					try
					{
						string productName = process.MainModule.FileVersionInfo.ProductName;
						if ((productName != null) && CaptureConfiguration.NoDWMCaptureForProduct.Contains(productName.ToLower()))
						{
							return false;
						}
					}
					catch (Exception ex)
					{
						Log.Warn().WriteLine(ex.Message);
					}
				}
			}
			return true;
		}

		/// <summary>
		///     Helper method to check if it is allowed to capture the process using GDI
		/// </summary>
		/// <param name="process">Process owning the window</param>
		/// <returns>true if it's allowed</returns>
		public static bool IsGdiAllowed(Process process)
		{
			if (process != null)
			{
				if ((CaptureConfiguration.NoGDICaptureForProduct != null) && (CaptureConfiguration.NoGDICaptureForProduct.Count > 0))
				{
					try
					{
						string productName = process.MainModule.FileVersionInfo.ProductName;
						if ((productName != null) && CaptureConfiguration.NoGDICaptureForProduct.Contains(productName.ToLower()))
						{
							return false;
						}
					}
					catch (Exception ex)
					{
						Log.Warn().WriteLine(ex.Message);
					}
				}
			}
			return true;
		}
	}
}