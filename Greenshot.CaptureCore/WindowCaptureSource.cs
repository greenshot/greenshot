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
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Config.Ini;
using Dapplo.Log;
using Dapplo.Windows.Native;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Enumerations;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.CaptureCore
{
	/// <summary>
	///     ICaptureSource implementation for capturing windows
	/// </summary>
	public class WindowCaptureSource : ICaptureSource
	{
		private static readonly LogSource Log = new LogSource();
		private readonly CaptureCursor _captureCursor = new CaptureCursor();

		/// <summary>
		/// Specify if the cursor should be captured
		/// </summary>
		public bool CaptureCursor { get; set; }

		/// <summary>
		/// Mode to use for the windows capture
		/// </summary>
		public WindowCaptureMode Mode { get; set; } = WindowCaptureMode.Auto;

		/// <summary>
		/// Specify if the target window is an IE process, this is captured.
		/// </summary>
		public bool IeCapture { get; set; } = true;

		/// <summary>
		/// Capture the current active window
		/// </summary>
		/// <returns></returns>
		public async Task<ICapture> CaptureActiveAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Starting to capture the active window.");

			var windowToCapture = WindowDetails.GetActiveWindow();

			if (windowToCapture != null)
			{
				Log.Debug().WriteLine("Capturing window: {0} with {1}", windowToCapture.Text, windowToCapture.WindowRectangle);
			}
			else
			{
				Log.Warn().WriteLine("No active window to capture!");
				// Nothing to capture, code up in the stack will capture the full screen
				return null;
			}

			// MAke sure the window is visible
			if (windowToCapture.Iconic)
			{
				// Restore the window making sure it's visible, and responding (TOAD / Excel)
				windowToCapture.Restore();
				// Await the animation of maximizing the wiEndow
				await Task.Delay(300, cancellationToken).ConfigureAwait(false);
			}

			// Some applications like Excel and TOAD have weird behaviour, and the window selected is not the one that is visible!
			windowToCapture = windowToCapture.WindowToCapture();

			if (windowToCapture == null)
			{
				Log.Warn().WriteLine("Window cannot be captured, no linked window!");
				// Nothing to capture, code up in the stack will capture the full screen
				return null;
			}

			return await CaptureAsync(windowToCapture, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// When WindowCaptureMode is set to auto, this logic will decide on the mode to use
		/// </summary>
		/// <param name="windowCaptureMode"></param>
		/// <param name="windowToCapture"></param>
		/// <param name="process"></param>
		/// <returns>WindowCaptureMode</returns>
		private WindowCaptureMode DecideCaptureMode(WindowCaptureMode windowCaptureMode, WindowDetails windowToCapture, Process process)
		{
			// When Vista & DWM (Aero) enabled
			bool dwmEnabled = Dwm.IsDwmEnabled;

			switch (windowCaptureMode)
			{
				case WindowCaptureMode.GDI:
					if (!WindowCapture.IsGdiAllowed(process))
					{
						// GDI not allowed, take screen
						windowCaptureMode = WindowCaptureMode.Screen;
					}
					break;
				case WindowCaptureMode.Aero:
				case WindowCaptureMode.AeroTransparent:
					if (!dwmEnabled || (!windowToCapture.IsMetroApp && !WindowCapture.IsDwmAllowed(process)))
					{
						// Take default screen
						windowCaptureMode = WindowCaptureMode.Screen;
						// Change to GDI, if allowed
						if (WindowCapture.IsGdiAllowed(process))
						{
							windowCaptureMode = WindowCaptureMode.GDI;
						}
					}
					break;
				case WindowCaptureMode.Auto:
					// Take default screen
					windowCaptureMode = WindowCaptureMode.Screen;

					// Change to GDI, if allowed
					if (!windowToCapture.IsMetroApp && WindowCapture.IsGdiAllowed(process))
					{
						if (!dwmEnabled && IsWpf(process))
						{
							// do not use GDI, as DWM is not enabled and the application uses PresentationFramework.dll -> isWPF
							Log.Info().WriteLine("Not using GDI for windows of process {0}, as the process uses WPF", process.ProcessName);
						}
						else
						{
							windowCaptureMode = WindowCaptureMode.GDI;
						}
					}

					// Change to DWM, if enabled and allowed
					if (dwmEnabled)
					{
						if (windowToCapture.IsMetroApp || WindowCapture.IsDwmAllowed(process))
						{
							// Recurse, to make sure this is possible
							windowCaptureMode = DecideCaptureMode(WindowCaptureMode.Aero, windowToCapture, process);
						}
					}
					break;
			}


			return windowCaptureMode;
		}

		/// <summary>
		///     Capture the supplied Window
		/// </summary>
		/// <param name="windowToCapture">Window to capture</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>ICapture</returns>
		public async Task<ICapture> CaptureAsync(WindowDetails windowToCapture, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (windowToCapture == null)
			{
				throw new ArgumentNullException();
			}
			ICapture resultCapture = new Capture();
			// TODO: Do we need this?
			windowToCapture.Reset();

			var cursor = _captureCursor.Capture(resultCapture.ScreenBounds.Location);
			resultCapture.CursorLocation = cursor.Location;
			resultCapture.Cursor = cursor.Cursor;
			resultCapture.CursorVisible = CaptureCursor;

			var windowRectangle = windowToCapture.WindowRectangle;

			// get process name to be able to exclude certain processes from certain capture modes
			using (var process = windowToCapture.Process)
			{
				bool isAutoMode = Mode == WindowCaptureMode.Auto;
				// For WindowCaptureMode.Auto we check:
				// 1) Is window IE, use IE Capture
				// 2) Is Windows >= Vista & DWM enabled: use DWM
				// 3) Otherwise use GDI (Screen might be also okay but might lose content)
				if (isAutoMode)
				{
					if (IeCapture)
					{
						var captureIe = new InternetExplorerCaptureSource()
						{
							IeCaptureConfiguration = IniConfig.Current.GetSubSection<IIECaptureConfiguration>()
						};
						if (captureIe.IsIEWindow(windowToCapture))
						{
							try
							{
								captureIe.IeWindowDetails = windowToCapture;
								var ieCapture = await captureIe.CaptureIeAsync(cancellationToken).ConfigureAwait(false);
								if (ieCapture != null)
								{
									return ieCapture;
								}
							}
							catch (Exception ex)
							{
								Log.Warn().WriteLine("Problem capturing IE, skipping to normal capture. Exception message was: {0}", ex.Message);
							}
						}
					}

				}
				var captureMode = DecideCaptureMode(Mode, windowToCapture, process);

				Log.Info().WriteLine("Capturing window with mode {0}", captureMode);
				bool captureTaken = false;

				windowRectangle.Intersect(resultCapture.ScreenBounds);

				// Try to capture
				while (!captureTaken)
				{
					ICapture tmpCapture = null;
					switch (captureMode)
					{
						case WindowCaptureMode.GDI:
							if (WindowCapture.IsGdiAllowed(process))
							{
								if (windowToCapture.Iconic)
								{
									// Restore the window making sure it's visible!
									windowToCapture.Restore();
								}
								else
								{
									windowToCapture.ToForeground();
								}
								tmpCapture = windowToCapture.CaptureGdiWindow(resultCapture);
								if (tmpCapture != null)
								{
									// check if GDI capture any good, by comparing it with the screen content
									int blackCountGdi = ImageHelper.CountColor(tmpCapture.Image, Color.Black, false);
									int gdiPixels = tmpCapture.Image.Width*tmpCapture.Image.Height;
									int blackPercentageGdi = blackCountGdi*100/gdiPixels;
									if (blackPercentageGdi >= 1)
									{
										int screenPixels = windowRectangle.Width*windowRectangle.Height;
										using (ICapture screenCapture = new Capture())
										{
											screenCapture.CaptureDetails = resultCapture.CaptureDetails;
											if (WindowCapture.CaptureRectangleFromDesktopScreen(screenCapture, windowRectangle) != null)
											{
												int blackCountScreen = ImageHelper.CountColor(screenCapture.Image, Color.Black, false);
												int blackPercentageScreen = blackCountScreen*100/screenPixels;
												if (screenPixels == gdiPixels)
												{
													// "easy compare", both have the same size
													// If GDI has more black, use the screen capture.
													if (blackPercentageGdi > blackPercentageScreen)
													{
														Log.Debug().WriteLine("Using screen capture, as GDI had additional black.");
														// changeing the image will automatically dispose the previous
														tmpCapture.Image = screenCapture.Image;
														// Make sure it's not disposed, else the picture is gone!
														screenCapture.NullImage();
													}
												}
												else if (screenPixels < gdiPixels)
												{
													// Screen capture is cropped, window is outside of screen
													if ((blackPercentageGdi > 50) && (blackPercentageGdi > blackPercentageScreen))
													{
														Log.Debug().WriteLine("Using screen capture, as GDI had additional black.");
														// changeing the image will automatically dispose the previous
														tmpCapture.Image = screenCapture.Image;
														// Make sure it's not disposed, else the picture is gone!
														screenCapture.NullImage();
													}
												}
												else
												{
													// Use the GDI capture by doing nothing
													Log.Debug().WriteLine("This should not happen, how can there be more screen as GDI pixels?");
												}
											}
										}
									}
								}
							}
							if (tmpCapture != null)
							{
								resultCapture = tmpCapture;
								captureTaken = true;
							}
							else
							{
								// A problem, try Screen
								captureMode = WindowCaptureMode.Screen;
							}
							break;
						case WindowCaptureMode.Aero:
						case WindowCaptureMode.AeroTransparent:
							if (windowToCapture.IsMetroApp || WindowCapture.IsDwmAllowed(process))
							{
								tmpCapture = windowToCapture.CaptureDwmWindow(resultCapture, captureMode, isAutoMode);
							}
							if (tmpCapture != null)
							{
								resultCapture = tmpCapture;
								captureTaken = true;
							}
							else
							{
								// A problem, try GDI
								captureMode = WindowCaptureMode.GDI;
							}
							break;
						default:
							// Screen capture
							if (windowToCapture.Iconic)
							{
								// Restore the window making sure it's visible!
								windowToCapture.Restore();
							}
							else
							{
								windowToCapture.ToForeground();
							}

							try
							{
								resultCapture = WindowCapture.CaptureRectangleFromDesktopScreen(resultCapture, windowRectangle);
								captureTaken = true;
							}
							catch (Exception e)
							{
								Log.Error().WriteLine("Problem capturing", e);
								return null;
							}
							break;
					}
				}
			}

			if (resultCapture != null)
			{
				resultCapture.CaptureDetails.Title = windowToCapture.Text;
			}

			return resultCapture;
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/// <summary>
		///     Check if Process uses PresentationFramework.dll -> meaning it uses WPF
		/// </summary>
		/// <param name="process">Proces to check for the presentation framework</param>
		/// <returns>true if the process uses WPF</returns>
		private static bool IsWpf(Process process)
		{
			if (process == null)
			{
				return false;
			}
			try
			{
				foreach (ProcessModule module in process.Modules)
				{
					if (!module.ModuleName.StartsWith("PresentationFramework"))
					{
						continue;
					}
					Log.Info().WriteLine("Found that Process {0} uses {1}, assuming it's using WPF", process.ProcessName, module.FileName);
					return true;
				}
			}
			catch (Exception)
			{
				// Access denied on the modules
				Log.Warn().WriteLine("No access on the modules from process {0}, assuming WPF is used.", process.ProcessName);
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public string Name { get; } = nameof(WindowCaptureSource);

		public async Task TakeCaptureAsync(ICaptureFlow captureFlow, CancellationToken cancellationToken = new CancellationToken())
		{
			captureFlow.Capture = await CaptureActiveAsync(cancellationToken);

		}
	}
}