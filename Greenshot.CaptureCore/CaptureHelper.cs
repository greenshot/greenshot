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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Log;
using Dapplo.Utils;
using Dapplo.Windows.Native;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.CaptureCore.Extensions;
using Greenshot.CaptureCore.Forms;
using Greenshot.CaptureCore.Views;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Enumerations;
using Greenshot.Core.Gfx;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Drawing.Point;

#endregion

namespace Greenshot.CaptureCore
{
	/// <summary>
	///     CaptureHelper contains all the capture logic
	/// </summary>
	public class CaptureHelper : IDisposable
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ICoreConfiguration CoreConfiguration = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();
		private readonly bool _captureMouseCursor;
		private ICapture _capture;
		private CaptureModes _captureMode;
		private Rectangle _captureRect = Rectangle.Empty;
		private ScreenCaptureMode _screenCaptureMode = ScreenCaptureMode.Auto;
		// TODO: when we get the screen capture code working correctly, this needs to be enabled
		//private static ScreenCaptureHelper screenCapture = null;

		public CaptureHelper(CaptureModes captureMode)
		{
			_captureMode = captureMode;
			_capture = new Capture();
		}

		public CaptureHelper(CaptureModes captureMode, bool captureMouseCursor) : this(captureMode)
		{
			_captureMouseCursor = captureMouseCursor;
		}

		public CaptureHelper(CaptureModes captureMode, bool captureMouseCursor, ScreenCaptureMode screenCaptureMode) : this(captureMode)
		{
			_captureMouseCursor = captureMouseCursor;
			_screenCaptureMode = screenCaptureMode;
		}

		public CaptureHelper(CaptureModes captureMode, bool captureMouseCursor, IDestination destination) : this(captureMode, captureMouseCursor)
		{
			_capture.CaptureDetails.AddDestination(destination);
		}

		public WindowDetails SelectedCaptureWindow { get; set; }

		/// <summary>
		///     The public accessible Dispose
		///     Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void AddConfiguredDestination()
		{
			var destinations = Enumerable.Empty<IDestination>(); //Start.Dapplication.Bootstrapper.GetExports<IDestination>().Where(x => CoreConfiguration.OutputDestinations.Contains(x.Value.Designation)).Select(x => x.Value);
			foreach (var destination in destinations)
			{
				_capture.CaptureDetails.AddDestination(destination);
			}
		}

		public CaptureHelper AddDestination(IDestination destination)
		{
			_capture.CaptureDetails.AddDestination(destination);
			return this;
		}

		private async Task<bool> CaptureActiveWindow()
		{
			bool presupplied = false;
			Log.Debug().WriteLine("CaptureActiveWindow");
			if (SelectedCaptureWindow != null)
			{
				Log.Debug().WriteLine("Using supplied window");
				presupplied = true;
			}
			else
			{
				SelectedCaptureWindow = WindowDetails.GetActiveWindow();
				if (SelectedCaptureWindow != null)
				{
					Log.Debug().WriteLine("Capturing window: {0} with {1}", SelectedCaptureWindow.Text, SelectedCaptureWindow.WindowRectangle);
				}
			}
			if ((SelectedCaptureWindow == null) || (!presupplied && SelectedCaptureWindow.Iconic))
			{
				Log.Warn().WriteLine("No window to capture!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}

			// Used to be !presupplied &&, but I don't know why
			if ((SelectedCaptureWindow != null) && SelectedCaptureWindow.Iconic)
			{
				// Restore the window making sure it's visible, and responding (TOAD / Excel)
				SelectedCaptureWindow.Restore();
				// Await the animation of maximizing the wiEndow
				await Task.Delay(300).ConfigureAwait(false);
			}
			else if (presupplied)
			{
				SelectedCaptureWindow.ToForeground();
			}

			// Some applications like Excel and TOAD have weird behaviour, and the window selected is not the one that is visible!
			SelectedCaptureWindow = SelectedCaptureWindow.WindowToCapture();
			if (SelectedCaptureWindow == null)
			{
				Log.Warn().WriteLine("No window to capture, after SelectCaptureWindow!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}
			_captureRect = SelectedCaptureWindow.WindowRectangle;
			// Fix for Bug #3430560 
			CoreConfiguration.LastCapturedRegion = _captureRect;
			bool returnValue = CaptureWindow(SelectedCaptureWindow, _capture, CoreConfiguration.WindowCaptureMode) != null;
			return returnValue;
		}

		public static async Task CaptureClipboardAsync(CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.Clipboard))
			{
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureFileAsync(string filename, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.File))
			{
				await captureHelper.MakeCaptureAsync(filename, token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureFileAsync(string filename, IDestination destination, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.File))
			{
				await captureHelper.AddDestination(destination).MakeCaptureAsync(filename, token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureFullscreenAsync(bool captureMouse, ScreenCaptureMode screenCaptureMode, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.FullScreen, captureMouse))
			{
				captureHelper._screenCaptureMode = screenCaptureMode;
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureIEAsync(bool captureMouse, WindowDetails windowToCapture, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.IE, captureMouse))
			{
				captureHelper.SelectedCaptureWindow = windowToCapture;
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureLastRegionAsync(bool captureMouse, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.LastRegion, captureMouse))
			{
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureRegionAsync(bool captureMouse, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.Region, captureMouse))
			{
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureRegionAsync(bool captureMouse, IDestination destination, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.Region, captureMouse, destination))
			{
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureRegionAsync(bool captureMouse, Rectangle region, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.Region, captureMouse))
			{
				await captureHelper.MakeCaptureAsync(region, token).ConfigureAwait(false);
			}
		}

		/// <summary>
		///     Capture the supplied Window
		/// </summary>
		/// <param name="windowToCapture">Window to capture</param>
		/// <param name="captureForWindow">The capture to store the details</param>
		/// <param name="windowCaptureMode">What WindowCaptureMode to use</param>
		/// <returns></returns>
		public static ICapture CaptureWindow(WindowDetails windowToCapture, ICapture captureForWindow, WindowCaptureMode windowCaptureMode)
		{
			if (captureForWindow == null)
			{
				captureForWindow = new Capture();
			}
			windowToCapture.Reset();
			var windowRectangle = windowToCapture.WindowRectangle;

			// When Vista & DWM (Aero) enabled
			bool dwmEnabled = Dwm.IsDwmEnabled;
			// get process name to be able to exclude certain processes from certain capture modes
			using (var process = windowToCapture.Process)
			{
				bool isAutoMode = windowCaptureMode == WindowCaptureMode.Auto;
				// For WindowCaptureMode.Auto we check:
				// 1) Is window IE, use IE Capture
				// 2) Is Windows >= Vista & DWM enabled: use DWM
				// 3) Otherwise use GDI (Screen might be also okay but might lose content)
				if (isAutoMode)
				{
					if (CoreConfiguration.IECapture && IECaptureHelper.IsIEWindow(windowToCapture))
					{
						try
						{
							var ieCapture = IECaptureHelper.CaptureIE(captureForWindow, windowToCapture);
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
							windowCaptureMode = WindowCaptureMode.Aero;
						}
					}
				}
				else if ((windowCaptureMode == WindowCaptureMode.Aero) || (windowCaptureMode == WindowCaptureMode.AeroTransparent))
				{
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
				}
				else if ((windowCaptureMode == WindowCaptureMode.GDI) && !WindowCapture.IsGdiAllowed(process))
				{
					// GDI not allowed, take screen
					windowCaptureMode = WindowCaptureMode.Screen;
				}

				Log.Info().WriteLine("Capturing window with mode {0}", windowCaptureMode);
				bool captureTaken = false;
				windowRectangle.Intersect(captureForWindow.ScreenBounds);
				// Try to capture
				while (!captureTaken)
				{
					ICapture tmpCapture = null;
					switch (windowCaptureMode)
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
								tmpCapture = windowToCapture.CaptureGdiWindow(captureForWindow);
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
											screenCapture.CaptureDetails = captureForWindow.CaptureDetails;
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
								captureForWindow = tmpCapture;
								captureTaken = true;
							}
							else
							{
								// A problem, try Screen
								windowCaptureMode = WindowCaptureMode.Screen;
							}
							break;
						case WindowCaptureMode.Aero:
						case WindowCaptureMode.AeroTransparent:
							if (windowToCapture.IsMetroApp || WindowCapture.IsDwmAllowed(process))
							{
								tmpCapture = windowToCapture.CaptureDwmWindow(captureForWindow, windowCaptureMode, isAutoMode);
							}
							if (tmpCapture != null)
							{
								captureForWindow = tmpCapture;
								captureTaken = true;
							}
							else
							{
								// A problem, try GDI
								windowCaptureMode = WindowCaptureMode.GDI;
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
								captureForWindow = WindowCapture.CaptureRectangleFromDesktopScreen(captureForWindow, windowRectangle);
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

			if (captureForWindow != null)
			{
				captureForWindow.CaptureDetails.Title = windowToCapture.Text;
			}

			return captureForWindow;
		}

		public static async Task CaptureWindowAsync(bool captureMouse, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.ActiveWindow, captureMouse))
			{
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureWindowAsync(WindowDetails windowToCapture, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.ActiveWindow))
			{
				captureHelper.SelectedCaptureWindow = windowToCapture;
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		public static async Task CaptureWindowInteractiveAsync(bool captureMouse, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.Window))
			{
				await captureHelper.MakeCaptureAsync(token).ConfigureAwait(false);
			}
		}

		#region capture with feedback

		private async Task CaptureWithFeedbackAsync(Task<IList<WindowDetails>> retrieveWindowsTask, CancellationToken token = default(CancellationToken))
		{
			Log.Debug().WriteLine("CaptureWithFeedbackAsync start");
			bool isOk = false;
			await UiContext.RunOn(() =>
			{
				using (var captureForm = new CaptureForm(_capture, retrieveWindowsTask, CoreConfiguration))
				{
					DialogResult result;
					// Make sure the form is hidden after showing, even if an exception occurs, so all errors will be shown
					try
					{
						//result = captureForm.ShowDialog(MainForm.Instance);
						result = captureForm.ShowDialog();
					}
					finally
					{
						captureForm.Hide();
					}
					if (result == DialogResult.OK)
					{
						isOk = true;
						SelectedCaptureWindow = captureForm.SelectedCaptureWindow;
						_captureRect = captureForm.CaptureRectangle;
					}
				}
			}, token).ConfigureAwait(false);

			if (isOk)
			{
				// Get title
				if (SelectedCaptureWindow != null)
				{
					_capture.CaptureDetails.Title = SelectedCaptureWindow.Text;
				}

				if ((_captureRect.Height > 0) && (_captureRect.Width > 0))
				{
					// Take the captureRect, this already is specified as bitmap coordinates
					_capture.ApplyCrop(_captureRect);

					// save for re-capturing later and show recapture context menu option
					// Important here is that the location needs to be offsetted back to screen coordinates!
					Rectangle tmpRectangle = _captureRect;
					tmpRectangle.Offset(_capture.ScreenBounds.Location.X, _capture.ScreenBounds.Location.Y);
					CoreConfiguration.LastCapturedRegion = tmpRectangle;
					await HandleCaptureAsync(token);
				}
			}
		}

		#endregion

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/// <summary>
		///     This Dispose is called from the Dispose and the Destructor.
		///     When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Cleanup
			}
			// Unfortunately we can't dispose the capture, this might still be used somewhere else.
			SelectedCaptureWindow = null;
			_capture = null;
			// Empty working set after capturing
			if (CoreConfiguration.MinimizeWorkingSetSize)
			{
				PsAPI.EmptyWorkingSet();
			}
		}

		/// <summary>
		///     Play sound / show flash
		/// </summary>
		/// <returns>Task to wait for</returns>
		private async Task DoCaptureFeedbackAsync()
		{
			var tasks = new List<Task>();
			if (CoreConfiguration.PlayCameraSound)
			{
				tasks.Add(Task.Run(() => SoundHelper.Play()));
			}
			if (CoreConfiguration.ShowFlash)
			{
				var bounds = new Rect(_captureRect.X, _captureRect.Y, _captureRect.Width, _captureRect.Height);
				tasks.Add(FlashlightWindow.Flash(bounds));
			}
			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private async Task HandleCaptureAsync(CancellationToken token = default(CancellationToken))
		{
			// Flag to see if the image was "exported" so the FileEditor doesn't
			// ask to save the file as long as nothing is done.
			// Make sure the user sees that the capture is made
			if ((_capture.CaptureDetails.CaptureMode == CaptureModes.File) || (_capture.CaptureDetails.CaptureMode == CaptureModes.Clipboard))
			{
				// Maybe not "made" but the original is still there... somehow
				_capture.Modified = false;
			}
			else
			{
				// Make sure the resolution is set correctly!
				if (_capture.CaptureDetails != null)
				{
					((Bitmap) _capture.Image)?.SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
				}
				await DoCaptureFeedbackAsync().ConfigureAwait(false);
			}

			Log.Debug().WriteLine("A capture of: {0}", _capture.CaptureDetails.Title);

			// check if someone has passed a destination
			if ((_capture.CaptureDetails.CaptureDestinations == null) || (_capture.CaptureDetails.CaptureDestinations.Count == 0))
			{
				AddConfiguredDestination();
			}

			// TODO:
			// Register notify events if this is wanted			
			//if (CoreConfiguration.ShowTrayNotification && !CoreConfiguration.HideTrayicon)

			// TODO:
			// Let the processors do their job

			// Get CaptureDetails as we need it even after the capture is disposed
			var captureDetails = _capture.CaptureDetails;
			bool canDisposeSurface = true;

			foreach (var destination in captureDetails.CaptureDestinations)
			{
				if (destination.Designation == BuildInDestinations.Picker.ToString())
				{
					// TODO: Caller?
					await destination.Export(null, _capture, token);
					captureDetails.CaptureDestinations.Clear();
					canDisposeSurface = false;
					break;
				}
			}

			// Disable capturing
			_captureMode = CaptureModes.None;
			// Dispose the capture, we don't need it anymore (the surface copied all information and we got the title (if any)).

			int destinationCount = captureDetails.CaptureDestinations.Count;
			if (destinationCount > 0)
			{
				// Flag to detect if we need to create a temp file for the email
				// or use the file that was written
				foreach (var destination in captureDetails.CaptureDestinations)
				{
					if (BuildInDestinations.Picker.ToString().Equals(destination.Designation))
					{
						continue;
					}
					Log.Info().WriteLine("Calling destination {0}", destination.Text);

					// TODO: Caller?
					var notification = await destination.Export(null, _capture, token);
					if (BuildInDestinations.Editor.ToString().Equals(notification.Source) && (notification.NotificationType == NotificationTypes.Success))
					{
						canDisposeSurface = false;
					}
				}
			}
			if (canDisposeSurface)
			{
				_capture.Dispose();
			}
		}

		public static async Task ImportCaptureAsync(ICapture captureToImport, CancellationToken token = default(CancellationToken))
		{
			using (var captureHelper = new CaptureHelper(CaptureModes.File))
			{
				captureHelper._capture = captureToImport;
				await captureHelper.HandleCaptureAsync(token).ConfigureAwait(false);
			}
		}

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

		/// <summary>
		///     Make Capture with file name
		/// </summary>
		/// <param name="filename">filename</param>
		/// <param name="token"></param>
		private async Task MakeCaptureAsync(string filename, CancellationToken token = default(CancellationToken))
		{
			_capture.CaptureDetails.Filename = filename;
			await MakeCaptureAsync(token).ConfigureAwait(false);
		}

		/// <summary>
		///     Make Capture for region
		/// </summary>
		/// <param name="region"></param>
		/// <param name="token"></param>
		private async Task MakeCaptureAsync(Rectangle region, CancellationToken token = default(CancellationToken))
		{
			_captureRect = region;
			await MakeCaptureAsync(token).ConfigureAwait(false);
		}


		/// <summary>
		///     Make Capture with specified destinations
		/// </summary>
		private async Task MakeCaptureAsync(CancellationToken token = default(CancellationToken))
		{
			Log.Debug().WriteLine("Starting MakeCaptureAsync");
			Task<IList<WindowDetails>> retrieveWindowDetailsTask = null;

			Log.Debug().WriteLine("Capturing with mode {0} and using Cursor {1}", _captureMode, _captureMouseCursor);
			_capture.CaptureDetails.CaptureMode = _captureMode;

			// Get the windows details in a seperate thread, only for those captures that have a Feedback
			// As currently the "elements" aren't used, we don't need them yet
			bool prepareNeeded = false;
			switch (_captureMode)
			{
				case CaptureModes.Region:
					// Check if a region is pre-supplied!
					if (Rectangle.Empty.Equals(_captureRect))
					{
						prepareNeeded = true;
					}
					break;
				case CaptureModes.Window:
					prepareNeeded = true;
					break;
			}
			if (prepareNeeded)
			{
				retrieveWindowDetailsTask = PrepareForCaptureWithFeedbackAsync(token);
			}

			// Add destinations if no-one passed a handler
			if ((_capture.CaptureDetails.CaptureDestinations == null) || (_capture.CaptureDetails.CaptureDestinations.Count == 0))
			{
				AddConfiguredDestination();
			}

			// Delay for the Context menu
			if (CoreConfiguration.CaptureDelay > 0)
			{
				await Task.Delay(CoreConfiguration.CaptureDelay, token).ConfigureAwait(false);
			}
			else
			{
				CoreConfiguration.CaptureDelay = 0;
			}

			// Capture Mousecursor if we are not loading from file or clipboard, only show when needed
			if ((_captureMode != CaptureModes.File) && (_captureMode != CaptureModes.Clipboard))
			{
				_capture = WindowCapture.CaptureCursor(_capture);
				_capture.CursorVisible = _captureMouseCursor && CoreConfiguration.CaptureMousepointer;
			}

			switch (_captureMode)
			{
				case CaptureModes.Window:
					_capture = WindowCapture.CaptureScreen(_capture);
					_capture.CaptureDetails.AddMetaData("source", "Screen");
					await SetDpi().ConfigureAwait(false);
					await CaptureWithFeedbackAsync(retrieveWindowDetailsTask, token).ConfigureAwait(false);
					break;
				case CaptureModes.ActiveWindow:
					if (await CaptureActiveWindow().ConfigureAwait(false))
					{
						// Capture worked, offset mouse according to screen bounds and capture location
						_capture.MoveMouseLocation(_capture.ScreenBounds.Location.X - _capture.Location.X, _capture.ScreenBounds.Location.Y - _capture.Location.Y);
						_capture.CaptureDetails.AddMetaData("source", "Window");
					}
					else
					{
						_captureMode = CaptureModes.FullScreen;
						_capture = WindowCapture.CaptureScreen(_capture);
						_capture.CaptureDetails.AddMetaData("source", "Screen");
						_capture.CaptureDetails.Title = "Screen";
					}
					await SetDpi().ConfigureAwait(false);
					await HandleCaptureAsync(token).ConfigureAwait(false);
					break;
				case CaptureModes.IE:
					if (IECaptureHelper.CaptureIE(_capture, SelectedCaptureWindow) != null)
					{
						_capture.CaptureDetails.AddMetaData("source", "Internet Explorer");
						await SetDpi().ConfigureAwait(false);
						await HandleCaptureAsync(token).ConfigureAwait(false);
					}
					break;
				case CaptureModes.FullScreen:
					// Check how we need to capture the screen
					bool captureTaken = false;
					switch (_screenCaptureMode)
					{
						case ScreenCaptureMode.Auto:
							var mouseLocation = User32.GetCursorLocation();
							foreach (var display in User32.AllDisplays())
							{
								if (display.Bounds.Contains(mouseLocation))
								{
									_capture = WindowCapture.CaptureRectangle(_capture, display.BoundsRectangle);
									captureTaken = true;
									break;
								}
							}
							break;
						case ScreenCaptureMode.Fixed:
							if ((CoreConfiguration.ScreenToCapture > 0) && (CoreConfiguration.ScreenToCapture <= User32.AllDisplays().Count))
							{
								_capture = WindowCapture.CaptureRectangle(_capture, User32.AllDisplays()[CoreConfiguration.ScreenToCapture].BoundsRectangle);
								captureTaken = true;
							}
							break;
						case ScreenCaptureMode.FullScreen:
							// Do nothing, we take the fullscreen capture automatically
							break;
					}
					if (!captureTaken)
					{
						_capture = WindowCapture.CaptureScreen(_capture);
					}
					await SetDpi().ConfigureAwait(false);
					await HandleCaptureAsync(token).ConfigureAwait(false);
					break;
				case CaptureModes.Clipboard:
					var clipboardImage = ClipboardHelper.GetImage();
					if (clipboardImage != null)
					{
						if (_capture != null)
						{
							_capture.Image = clipboardImage;
						}
						else
						{
							_capture = new Capture(clipboardImage);
						}
						_capture.CaptureDetails.Title = "Clipboard";
						_capture.CaptureDetails.AddMetaData("source", "Clipboard");
						// Force Editor, keep picker
						if (_capture.CaptureDetails.HasDestination(BuildInDestinations.Picker.ToString()))
						{
							_capture.CaptureDetails.ClearDestinations();
							// TODO: add editor & Picker
							//_capture.CaptureDetails.AddDestination(LegacyDestinationHelper.GetLegacyDestination(BuildInDestinations.Editor.ToString()));
						}
						else
						{
							_capture.CaptureDetails.ClearDestinations();
							// TODO: add editor
							//_capture.CaptureDetails.AddDestination(LegacyDestinationHelper.GetLegacyDestination(BuildInDestinations.Editor.ToString()));
						}
						await HandleCaptureAsync(token).ConfigureAwait(false);
					}
					break;
				case CaptureModes.File:
					Image fileImage = null;
					string filename = _capture.CaptureDetails.Filename;

					if (!string.IsNullOrEmpty(filename))
					{
						try
						{
							// Editor format
							if (filename.ToLower().EndsWith("." + OutputFormat.greenshot))
							{
								// TODO: Fix
								// await LegacyDestinationHelper.GetLegacyDestination(BuildInDestinations.Editor.ToString()).ExportCaptureAsync(true, _capture, token);
								break;
							}
						}
						catch (Exception e)
						{
							Log.Error().WriteLine(e.Message, e);
							MessageBox.Show(string.Format(language.ErrorOpenfile, filename));
						}
						try
						{
							fileImage = ImageHelper.LoadImage(filename, CoreConfiguration.ProcessEXIFOrientation);
						}
						catch (Exception e)
						{
							Log.Error().WriteLine(e.Message, e);
							MessageBox.Show(string.Format(language.ErrorOpenfile, filename));
						}
					}
					if (fileImage != null)
					{
						_capture.CaptureDetails.Title = Path.GetFileNameWithoutExtension(filename);
						_capture.CaptureDetails.AddMetaData("file", filename);
						_capture.CaptureDetails.AddMetaData("source", "file");
						if (_capture != null)
						{
							_capture.Image = fileImage;
						}
						else
						{
							_capture = new Capture(fileImage);
						}
						// Force Editor, keep picker, this is currently the only usefull destination
						if (_capture.CaptureDetails.HasDestination(BuildInDestinations.Picker.ToString()))
						{
							_capture.CaptureDetails.ClearDestinations();
							// TODO: Add picker & editor
							//_capture.CaptureDetails.AddDestination(LegacyDestinationHelper.GetLegacyDestination(BuildInDestinations.Editor.ToString()));
							//_capture.CaptureDetails.AddDestination(LegacyDestinationHelper.GetLegacyDestination(BuildInDestinations.Picker.ToString()));
						}
						else
						{
							_capture.CaptureDetails.ClearDestinations();
							// TODO: Add editor
							//_capture.CaptureDetails.AddDestination(LegacyDestinationHelper.GetLegacyDestination(BuildInDestinations.Editor.ToString()));
						}
						await HandleCaptureAsync(token).ConfigureAwait(false);
					}
					break;
				case CaptureModes.LastRegion:
					if (!CoreConfiguration.LastCapturedRegion.IsEmpty)
					{
						_capture = WindowCapture.CaptureRectangle(_capture, CoreConfiguration.LastCapturedRegion);

						// Set capture title, fixing bug #3569703
						foreach (var window in WindowDetails.GetVisibleWindows())
						{
							var estimatedLocation = new Point(CoreConfiguration.LastCapturedRegion.X + CoreConfiguration.LastCapturedRegion.Width/2, CoreConfiguration.LastCapturedRegion.Y + CoreConfiguration.LastCapturedRegion.Height/2);
							if (!window.Contains(estimatedLocation))
							{
								continue;
							}
							SelectedCaptureWindow = window;
							_capture.CaptureDetails.Title = SelectedCaptureWindow.Text;
							break;
						}
						// Move cursor, fixing bug #3569703
						_capture.MoveMouseLocation(_capture.ScreenBounds.Location.X - _capture.Location.X, _capture.ScreenBounds.Location.Y - _capture.Location.Y);
						//capture.MoveElements(capture.ScreenBounds.Location.X - capture.Location.X, capture.ScreenBounds.Location.Y - capture.Location.Y);

						_capture.CaptureDetails.AddMetaData("source", "screen");
						await SetDpi().ConfigureAwait(false);
						await HandleCaptureAsync(token).ConfigureAwait(false);
					}
					break;
				case CaptureModes.Region:
					// Check if a region is pre-supplied!
					if (Rectangle.Empty.Equals(_captureRect))
					{
						_capture = WindowCapture.CaptureScreen(_capture);
						_capture.CaptureDetails.AddMetaData("source", "screen");
						await SetDpi().ConfigureAwait(false);
						await CaptureWithFeedbackAsync(retrieveWindowDetailsTask, token).ConfigureAwait(false);
					}
					else
					{
						_capture = WindowCapture.CaptureRectangle(_capture, _captureRect);
						_capture.CaptureDetails.AddMetaData("source", "screen");
						await SetDpi().ConfigureAwait(false);
						await HandleCaptureAsync(token).ConfigureAwait(false);
					}
					break;
				default:
					Log.Warn().WriteLine("Unknown capture mode: {0}", _captureMode);
					break;
			}
			// Wait for thread, otherwise we can't dipose the CaptureHelper
			if (retrieveWindowDetailsTask != null)
			{
				await retrieveWindowDetailsTask;
			}
			if (_capture != null)
			{
				Log.Debug().WriteLine("Disposing capture");
				_capture.Dispose();
			}
			Log.Debug().WriteLine("Ended MakeCaptureAsync");
		}

		/// <summary>
		///     Pre-Initialization for CaptureWithFeedback, this will get all the windows before we change anything
		/// </summary>
		private async Task<IList<WindowDetails>> PrepareForCaptureWithFeedbackAsync(CancellationToken token = default(CancellationToken))
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
				int goLevelDeep = CoreConfiguration.WindowCaptureAllChildLocations ? 20 : 3;
				var visibleWindows = from window in WindowDetails.GetMetroApps().Concat(WindowDetails.GetAllWindows())
					where window.Visible && (window.WindowRectangle.Width != 0) && (window.WindowRectangle.Height != 0)
					select window;

				// Start Enumeration of "active" windows
				foreach (var window in visibleWindows)
				{
					// Make sure the details are retrieved once
					window.FreezeDetails();

					window.GetChildren(goLevelDeep);
					result.Add(window);
				}
				return result;
			}, token).ConfigureAwait(false);
		}

		private async Task SetDpi()
		{
			// Workaround for proble with DPI retrieval, the FromHwnd activates the window...
			var previouslyActiveWindow = WindowDetails.GetActiveWindow();

			await UiContext.RunOn(() =>
			{
				// Workaround for changed DPI settings in Windows 7
				// TODO: Check why I wrote Windows 7, and if the DesktopWindow works just like MainForm.Instance
				using (var graphics = Graphics.FromHwnd(User32.GetDesktopWindow()))
				{
					_capture.CaptureDetails.DpiX = graphics.DpiX;
					_capture.CaptureDetails.DpiY = graphics.DpiY;
				}
				// Set previouslyActiveWindow as foreground window
				previouslyActiveWindow?.ToForeground();
				if (_capture.CaptureDetails != null)
				{
					((Bitmap) _capture.Image)?.SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
				}
			}).ConfigureAwait(false);
		}
	}
}