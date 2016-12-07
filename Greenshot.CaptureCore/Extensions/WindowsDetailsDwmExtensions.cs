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
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Log;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Enumerations;
using Greenshot.Core.Extensions;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.CaptureCore.Extensions
{
	/// <summary>
	///     Dwm code for the WindowsDetails
	/// </summary>
	public static class WindowsDetailsDwmExtensions
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ICaptureConfiguration Conf = IniConfig.Current.GetSubSection<ICaptureConfiguration>();

		/// <summary>
		///     Capture DWM Window
		/// </summary>
		/// <param name="window">WindowDetails for the window to capture</param>
		/// <param name="capture">Capture to fill</param>
		/// <param name="windowCaptureMode">Wanted WindowCaptureMode</param>
		/// <param name="autoMode">True if auto modus is used</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>ICapture with the capture</returns>
		public static async Task<ICapture> CaptureDwmWindowAsync(this WindowDetails window, ICapture capture, WindowCaptureMode windowCaptureMode, bool autoMode, CancellationToken cancellationToken = default(CancellationToken))
		{
			capture.Image = await window.DwmCaptureAsync(windowCaptureMode, autoMode, capture.ScreenBounds, cancellationToken);
			// Make sure the capture location is the location of the window, not the copy
			capture.Location = window.Location;

			return capture;
		}


		/// <summary>
		///     Capture DWM Window
		/// </summary>
		/// <param name="window">WindowDetails for the window to capture</param>
		/// <param name="windowCaptureMode">Wanted WindowCaptureMode</param>
		/// <param name="autoMode">True if auto modus is used</param>
		/// <param name="screenbounds">Rectangle for the screen bounds</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>ICapture with the capture</returns>
		public static async Task<Bitmap> DwmCaptureAsync(this WindowDetails window, WindowCaptureMode windowCaptureMode, bool autoMode, Rectangle screenbounds, CancellationToken cancellationToken = default(CancellationToken))
		{
			IntPtr thumbnailHandle = IntPtr.Zero;
			Form tempForm = null;
			bool tempFormShown = false;
			try
			{
				tempForm = new Form
				{
					ShowInTaskbar = false,
					FormBorderStyle = FormBorderStyle.None,
					TopMost = true
				};

				// Register the Thumbnail
				Dwm.DwmRegisterThumbnail(tempForm.Handle, window.Handle, out thumbnailHandle);

				// Get the original size
				SIZE sourceSize;
				Dwm.DwmQueryThumbnailSourceSize(thumbnailHandle, out sourceSize);

				if ((sourceSize.width <= 0) || (sourceSize.height <= 0))
				{
					return null;
				}

				// Calculate the location of the temp form
				Rectangle windowRectangle = window.WindowRectangle;
				// Assume using it's own location
				Point formLocation = windowRectangle.Location;
				Size borderSize = new Size();
				bool doesCaptureFit = false;
				if (!window.Maximised)
				{
					doesCaptureFit = window.GetVisibleLocation(out formLocation);
				}
				else if (!Environment.OSVersion.IsWindows8OrLater())
				{
					window.GetBorderSize(out borderSize);
					formLocation = new Point(windowRectangle.X - borderSize.Width, windowRectangle.Y - borderSize.Height);
				}

				tempForm.Location = formLocation;
				tempForm.Size = sourceSize.ToSystemDrawingSize();

				// Prepare rectangle to capture from the screen.
				Rectangle captureRectangle = new Rectangle(formLocation.X, formLocation.Y, sourceSize.width, sourceSize.height);
				if (window.Maximised)
				{
					// Correct capture size for maximized window by offsetting the X,Y with the border size
					// and subtracting the border from the size (2 times, as we move right/down for the capture without resizing)
					captureRectangle.Inflate(borderSize.Width, borderSize.Height);
				}
				else if (autoMode)
				{
					// check if the capture fits
					if (!doesCaptureFit)
					{
						// if GDI is allowed.. (a screenshot won't be better than we comes if we continue)
						using (var thisWindowProcess = window.Process)
						{
							if (!window.IsMetroApp && WindowCapture.IsGdiAllowed(thisWindowProcess))
							{
								// we return null which causes the capturing code to try another method.
								return null;
							}
						}
					}
				}

				// Prepare the displaying of the Thumbnail
				var dwmThumbnailProperties = new DWM_THUMBNAIL_PROPERTIES
				{
					Opacity = 255,
					Visible = true,
					Destination = new RECT(0, 0, sourceSize.width, sourceSize.height)
				};
				Dwm.DwmUpdateThumbnailProperties(thumbnailHandle, ref dwmThumbnailProperties);
				tempForm.Show();
				tempFormShown = true;

				if (screenbounds != Rectangle.Empty)
				{
					// Intersect with screen
					captureRectangle.Intersect(screenbounds);
				}

				// Destination bitmap for the capture
				Bitmap capturedBitmap = null;
				bool frozen = false;
				try
				{
					// Check if we make a transparent capture
					if (windowCaptureMode == WindowCaptureMode.AeroTransparent)
					{
						frozen = window.FreezeWindow();
						// Use white, later black to capture transparent
						tempForm.BackColor = Color.White;
						// Make sure everything is visible
						tempForm.Refresh();
						// Make sure the UI can draw changes
						await Task.Delay(100, cancellationToken);

						try
						{
							using (var whiteBitmap = WindowCapture.CaptureRectangle(captureRectangle))
							{
								// Apply a white color
								tempForm.BackColor = Color.Black;
								// Make sure everything is visible
								tempForm.Refresh();
								if (!window.IsMetroApp)
								{
									// Make sure the application window is active, so the colors & buttons are right
									window.ToForeground();
								}
								// Make sure all changes are processed and visible
								await Task.Delay(100, cancellationToken);
								using (var blackBitmap = WindowCapture.CaptureRectangle(captureRectangle))
								{
									capturedBitmap = ImageHelper.ApplyTransparency(blackBitmap, whiteBitmap);
								}
							}
						}
						catch (Exception e)
						{
							Log.Debug().WriteLine(e);
							// Some problem occurred, cleanup and make a normal capture
							if (capturedBitmap != null)
							{
								capturedBitmap.Dispose();
								capturedBitmap = null;
							}
						}
					}
					// If no capture up till now, create a normal capture.
					if (capturedBitmap == null)
					{
						// Remove transparency, this will break the capturing
						if (!autoMode)
						{
							tempForm.BackColor = Color.FromArgb(255, Conf.DWMBackgroundColor.R, Conf.DWMBackgroundColor.G, Conf.DWMBackgroundColor.B);
						}
						else
						{
							var colorizationColor = Dwm.ColorizationColor;
							// Modify by losing the transparency and increasing the intensity (as if the background color is white)
							tempForm.BackColor = Color.FromArgb(255, (colorizationColor.R + 255) >> 1, (colorizationColor.G + 255) >> 1, (colorizationColor.B + 255) >> 1);
						}
						// Make sure everything is visible
						tempForm.Refresh();
						if (!window.IsMetroApp)
						{
							// Make sure the application window is active, so the colors & buttons are right
							window.ToForeground();
						}
						// Make sure all changes are processed and visible
						await Task.Delay(100, cancellationToken);
						// Capture from the screen
						capturedBitmap = WindowCapture.CaptureRectangle(captureRectangle);
					}
					if (capturedBitmap != null)
					{
						// Not needed for Windows 8 or higher
						if (Environment.OSVersion.IsWindows8OrLater())
						{
							// Only if the Inivalue is set, not maximized and it's not a tool window.
							if (Conf.WindowCaptureRemoveCorners && !window.Maximised && ((window.ExtendedWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) == 0))
							{
								// Remove corners
								if (!Image.IsAlphaPixelFormat(capturedBitmap.PixelFormat))
								{
									Log.Debug().WriteLine("Changing pixelformat to Alpha for the RemoveCorners");
									var tmpBitmap = ImageHelper.Clone(capturedBitmap, PixelFormat.Format32bppArgb);
									capturedBitmap.Dispose();
									capturedBitmap = tmpBitmap;
								}
								RemoveCorners(capturedBitmap);
							}
						}
					}
				}
				finally
				{
					// Make sure to ALWAYS unfreeze!!
					if (frozen)
					{
						window.UnfreezeWindow();
					}
				}

				return capturedBitmap;
			}
			finally
			{
				if (thumbnailHandle != IntPtr.Zero)
				{
					// Unregister (cleanup), as we are finished we don't need the form or the thumbnail anymore
					Dwm.DwmUnregisterThumbnail(thumbnailHandle);
				}
				if (tempForm != null)
				{
					if (tempFormShown)
					{
						tempForm.Close();
					}
					tempForm.Dispose();
				}
			}
		}

		/// <summary>
		///     Helper method to remove the corners from a DMW capture
		/// </summary>
		/// <param name="image">The bitmap to remove the corners from.</param>
		private static void RemoveCorners(Bitmap image)
		{
			using (var fastBitmap = FastBitmap.Create(image))
			{
				for (int y = 0; y < Conf.WindowCornerCutShape.Count; y++)
				{
					for (int x = 0; x < Conf.WindowCornerCutShape[y]; x++)
					{
						fastBitmap.SetColorAt(x, y, Color.Transparent);
						fastBitmap.SetColorAt(image.Width - 1 - x, y, Color.Transparent);
						fastBitmap.SetColorAt(image.Width - 1 - x, image.Height - 1 - y, Color.Transparent);
						fastBitmap.SetColorAt(x, image.Height - 1 - y, Color.Transparent);
					}
				}
			}
		}
	}
}