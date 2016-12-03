using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Windows.Native;
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Structs;
using Greenshot.Core;
using Greenshot.Core.Enumerations;
using Greenshot.Core.Extensions;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;

namespace Greenshot.CaptureCore.CaptureSources
{
	/// <summary>
	/// Logic to capture the screen
	/// </summary>
	public class ScreenCaptureSource : ICaptureSource
	{
		private static readonly LogSource Log = new LogSource();
		private readonly CaptureCursor _captureCursor = new CaptureCursor();

		/// <summary>
		/// Specify if the cursor should be captured
		/// </summary>
		public bool CaptureCursor { get; set; }

		/// <summary>
		/// Mode to use for the screen capture
		/// </summary>
		public ScreenCaptureMode Mode { get; set; } = ScreenCaptureMode.Auto;

		/// <summary>
		/// Capture the screen where the mouse is
		/// </summary>
		/// <returns>ICapture</returns>
		public ICapture CaptureActiveScreen()
		{
			ICapture capture = new Capture();
			var cursorInfo = _captureCursor.Capture(capture.ScreenBounds.Location);
			capture.CursorLocation = cursorInfo.Location;
			capture.Cursor = cursorInfo.Cursor;
			capture.CursorVisible = CaptureCursor;
			capture.CaptureDetails.CaptureMode = CaptureModes.Region;
			var mouseLocation = User32.GetCursorLocation();
			foreach (var display in User32.AllDisplays())
			{
				if (!display.Bounds.Contains(mouseLocation))
				{
					continue;
				}
				capture.Image = Capture<Bitmap>(display.BoundsRectangle);
				return capture;
			}
			return null;
		}

		/// <summary>
		///     Helper method to create an exception that might explain what is wrong while capturing
		/// </summary>
		/// <param name="method">string with current method</param>
		/// <param name="captureBounds">Rectangle of what we want to capture</param>
		/// <returns>Win32Exception</returns>
		private static Win32Exception CreateCaptureException(string method, Rectangle captureBounds)
		{
			var win32Exception = new Win32Exception();
			win32Exception.Data.Add("Method", method);
			if (!captureBounds.IsEmpty)
			{
				win32Exception.Data.Add("Height", captureBounds.Height);
				win32Exception.Data.Add("Width", captureBounds.Width);
			}
			return win32Exception;
		}


		/// <summary>
		///     This method will use User32 code to capture the specified captureBounds from the screen
		/// </summary>
		/// <typeparam name="TResult">Bitmap or BitmapSource</typeparam>
		/// <param name="captureBounds">Rectangle with the bounds to capture</param>
		/// <returns>TResult which is captured from the screen at the location specified by the captureBounds</returns>
		public static TResult Capture<TResult>(Rectangle captureBounds) where TResult : class
		{
			if ((captureBounds.Height <= 0) || (captureBounds.Width <= 0))
			{
				Log.Warn().WriteLine("Nothing to capture, ignoring!");
				return default(TResult);
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

			Bitmap capturedBitmap = null;
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
								var screensInsideCapture = User32.AllDisplays().Where(info => info.BoundsRectangle.IntersectsWith(captureBounds));

								// Check all all screens are of an equal size
								bool offscreenContent;
								using (var captureRegion = new Region(captureBounds))
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
										capturedBitmap = ImageHelper.CreateEmpty(tmpBitmap.Width, tmpBitmap.Height, PixelFormat.Format32bppArgb, Color.Transparent, tmpBitmap.HorizontalResolution, tmpBitmap.VerticalResolution);
										// Content will be copied here
										using (var graphics = Graphics.FromImage(capturedBitmap))
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
									capturedBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());
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
			if (typeof(TResult) == typeof(Bitmap))
			{
				return capturedBitmap as TResult;
			}
			using (capturedBitmap)
			{
				return capturedBitmap.ToBitmapSource() as TResult;
			}
		}

		/// <summary>
		///     Pre-Initialization for CaptureWithFeedback, this will get all the windows before we change anything
		/// </summary>
		/// <param name="depth">How many children deep do we go</param>
		/// <param name="token"></param>
		public async Task<IList<WindowDetails>> RetrieveAllWindows(int depth = 20, CancellationToken token = default(CancellationToken))
		{
			var result = new List<WindowDetails>();
			var appLauncherWindow = WindowDetails.GetAppLauncher();
			if ((appLauncherWindow != null) && appLauncherWindow.Visible)
			{
				result.Add(appLauncherWindow);
			}
			return await Task.Run(() =>
			{
				// Force children retrieval, sometimes windows close on losing focus and this is solved by caching
				var visibleWindows = from window in WindowDetails.GetMetroApps().Concat(WindowDetails.GetAllWindows())
									 where window.Visible && (window.WindowRectangle.Width != 0) && (window.WindowRectangle.Height != 0)
									 select window;

				// Start Enumeration of "active" windows
				foreach (var window in visibleWindows)
				{
					if (token.IsCancellationRequested)
					{
						break;
					}
					// Make sure the details are retrieved once
					window.FreezeDetails();

					window.GetChildren(depth);
					result.Add(window);
				}
				return result;
			}, token).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public string Name { get; } = nameof(ScreenCaptureSource);

		/// <inheritdoc />
		public Task TakeCaptureAsync(ICaptureContext captureContext, CancellationToken cancellationToken = new CancellationToken())
		{
			captureContext.Capture = CaptureActiveScreen();
			return Task.FromResult(true);
		}
	}
}
