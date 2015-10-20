/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Config.Ini;
using Greenshot.Forms;
using Greenshot.Plugin;
using GreenshotEditorPlugin.Drawing;
using GreenshotPlugin.Core;
using Dapplo.Windows.Native;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;
using Dapplo.Windows.App;

namespace Greenshot.Helpers
{
	/// <summary>
	/// CaptureHelper contains all the capture logic 
	/// </summary>
	public class CaptureHelper : IDisposable
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (CaptureHelper));
		private static ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();
		// TODO: when we get the screen capture code working correctly, this needs to be enabled
		//private static ScreenCaptureHelper screenCapture = null;
		private WindowDetails _selectedCaptureWindow;
		private Rectangle _captureRect = Rectangle.Empty;
		private readonly bool _captureMouseCursor;
		private ICapture _capture;
		private CaptureMode _captureMode;
		private ScreenCaptureMode _screenCaptureMode = ScreenCaptureMode.Auto;

		/// <summary>
		/// The public accessible Dispose
		/// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/// <summary>
		/// This Dispose is called from the Dispose and the Destructor.
		/// When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Cleanup
			}
			// Unfortunately we can't dispose the capture, this might still be used somewhere else.
			_selectedCaptureWindow = null;
			_capture = null;
			// Empty working set after capturing
			if (conf.MinimizeWorkingSetSize)
			{
				PsAPI.EmptyWorkingSet();
			}
		}

		public static async Task CaptureClipboardAsync(CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Clipboard))
			{
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureRegionAsync(bool captureMouse, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse))
			{
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureRegionAsync(bool captureMouse, ILegacyDestination destination, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse, destination))
			{
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureRegionAsync(bool captureMouse, Rectangle region, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse))
			{
				await captureHelper.MakeCaptureAsync(region, token);
			}
		}

		public static async Task CaptureFullscreenAsync(bool captureMouse, ScreenCaptureMode screenCaptureMode, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.FullScreen, captureMouse))
			{
				captureHelper._screenCaptureMode = screenCaptureMode;
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureLastRegionAsync(bool captureMouse, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.LastRegion, captureMouse))
			{
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureIEAsync(bool captureMouse, WindowDetails windowToCapture, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.IE, captureMouse))
			{
				captureHelper.SelectedCaptureWindow = windowToCapture;
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureWindowAsync(bool captureMouse, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.ActiveWindow, captureMouse))
			{
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureWindowAsync(WindowDetails windowToCapture, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.ActiveWindow))
			{
				captureHelper.SelectedCaptureWindow = windowToCapture;
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureWindowInteractiveAsync(bool captureMouse, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Window))
			{
				await captureHelper.MakeCaptureAsync(token);
			}
		}

		public static async Task CaptureFileAsync(string filename, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File))
			{
				await captureHelper.MakeCaptureAsync(filename, token);
			}
		}

		public static async Task CaptureFileAsync(string filename, ILegacyDestination destination, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File))
			{
				await captureHelper.AddDestination(destination).MakeCaptureAsync(filename, token);
			}
		}

		public static async Task ImportCaptureAsync(ICapture captureToImport, CancellationToken token = default(CancellationToken))
		{
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File))
			{
				captureHelper._capture = captureToImport;
				await captureHelper.HandleCaptureAsync(token);
			}
		}

		public CaptureHelper AddDestination(ILegacyDestination destination)
		{
			_capture.CaptureDetails.AddDestination(destination);
			return this;
		}

		public CaptureHelper(CaptureMode captureMode)
		{
			_captureMode = captureMode;
			_capture = new Capture();
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor) : this(captureMode)
		{
			_captureMouseCursor = captureMouseCursor;
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, ScreenCaptureMode screenCaptureMode) : this(captureMode)
		{
			_captureMouseCursor = captureMouseCursor;
			_screenCaptureMode = screenCaptureMode;
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, ILegacyDestination destination) : this(captureMode, captureMouseCursor)
		{
			_capture.CaptureDetails.AddDestination(destination);
		}

		public WindowDetails SelectedCaptureWindow
		{
			get
			{
				return _selectedCaptureWindow;
			}
			set
			{
				_selectedCaptureWindow = value;
			}
		}

		private Task DoCaptureFeedbackAsync(CancellationToken token = default(CancellationToken))
		{
			if (conf.PlayCameraSound)
			{
				SoundHelper.Play(token);
			}
			return Task.FromResult(true);
		}

		/// <summary>
		/// Make Capture with file name
		/// </summary>
		/// <param name="filename">filename</param>
		/// <param name="token"></param>
		private async Task MakeCaptureAsync(string filename, CancellationToken token = default(CancellationToken))
		{
			_capture.CaptureDetails.Filename = filename;
			await MakeCaptureAsync(token);
		}

		/// <summary>
		/// Make Capture for region
		/// </summary>
		/// <param name="filename">filename</param>
		private async Task MakeCaptureAsync(Rectangle region, CancellationToken token = default(CancellationToken))
		{
			_captureRect = region;
			await MakeCaptureAsync(token);
		}


		/// <summary>
		/// Make Capture with specified destinations
		/// </summary>
		private async Task MakeCaptureAsync(CancellationToken token = default(CancellationToken))
		{
			LOG.Debug("Starting MakeCaptureAsync");
			var retrieveWindowDetailsTask = Task.FromResult<IList<WindowDetails>>(new List<WindowDetails>());

			// This fixes a problem when a balloon is still visible and a capture needs to be taken
			// forcefully removes the balloon!
			if (!conf.HideTrayicon)
			{
				MainForm.Instance.NotifyIcon.Visible = false;
				MainForm.Instance.NotifyIcon.Visible = true;
			}
			LOG.Debug(String.Format("Capturing with mode {0} and using Cursor {1}", _captureMode, _captureMouseCursor));
			_capture.CaptureDetails.CaptureMode = _captureMode;

			// Get the windows details in a seperate thread, only for those captures that have a Feedback
			// As currently the "elements" aren't used, we don't need them yet
			bool prepareNeeded = false;
			switch (_captureMode)
			{
				case CaptureMode.Region:
					// Check if a region is pre-supplied!
					if (Rectangle.Empty.Equals(_captureRect))
					{
						prepareNeeded = true;
					}
					break;
				case CaptureMode.Window:
					prepareNeeded = true;
					break;
			}
			if (prepareNeeded)
			{
				retrieveWindowDetailsTask = PrepareForCaptureWithFeedbackAsync(token);
			}

			// Add destinations if no-one passed a handler
			if (_capture.CaptureDetails.CaptureDestinations == null || _capture.CaptureDetails.CaptureDestinations.Count == 0)
			{
				AddConfiguredDestination();
			}

			// Delay for the Context menu
			if (conf.CaptureDelay > 0)
			{
				await Task.Delay(conf.CaptureDelay);
			}
			else
			{
				conf.CaptureDelay = 0;
			}

			// Capture Mousecursor if we are not loading from file or clipboard, only show when needed
			if (_captureMode != CaptureMode.File && _captureMode != CaptureMode.Clipboard)
			{
				_capture = WindowCapture.CaptureCursor(_capture);
				if (_captureMouseCursor)
				{
					_capture.CursorVisible = conf.CaptureMousepointer;
				}
				else
				{
					_capture.CursorVisible = false;
				}
			}

			switch (_captureMode)
			{
				case CaptureMode.Window:
					_capture = WindowCapture.CaptureScreen(_capture);
					_capture.CaptureDetails.AddMetaData("source", "Screen");
					SetDPI();
					await CaptureWithFeedbackAsync(retrieveWindowDetailsTask, token);
					break;
				case CaptureMode.ActiveWindow:
					if (CaptureActiveWindow())
					{
						// Capture worked, offset mouse according to screen bounds and capture location
						_capture.MoveMouseLocation(_capture.ScreenBounds.Location.X - _capture.Location.X, _capture.ScreenBounds.Location.Y - _capture.Location.Y);
						_capture.CaptureDetails.AddMetaData("source", "Window");
					}
					else
					{
						_captureMode = CaptureMode.FullScreen;
						_capture = WindowCapture.CaptureScreen(_capture);
						_capture.CaptureDetails.AddMetaData("source", "Screen");
						_capture.CaptureDetails.Title = "Screen";
					}
					SetDPI();
					await HandleCaptureAsync(token);
					break;
				case CaptureMode.IE:
					if (IECaptureHelper.CaptureIE(_capture, SelectedCaptureWindow) != null)
					{
						_capture.CaptureDetails.AddMetaData("source", "Internet Explorer");
						SetDPI();
						await HandleCaptureAsync(token);
					}
					break;
				case CaptureMode.FullScreen:
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
							if (conf.ScreenToCapture > 0 && conf.ScreenToCapture <= User32.AllDisplays().Count)
							{
								_capture = WindowCapture.CaptureRectangle(_capture, User32.AllDisplays()[conf.ScreenToCapture].BoundsRectangle);
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
					SetDPI();
					await HandleCaptureAsync(token);
					break;
				case CaptureMode.Clipboard:
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
						if (_capture.CaptureDetails.HasDestination(BuildInDestinationEnum.Picker.ToString()))
						{
							_capture.CaptureDetails.ClearDestinations();
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(BuildInDestinationEnum.Editor.ToString()));
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(BuildInDestinationEnum.Picker.ToString()));
						}
						else
						{
							_capture.CaptureDetails.ClearDestinations();
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(BuildInDestinationEnum.Editor.ToString()));
						}
						await HandleCaptureAsync(token);
					}
					break;
				case CaptureMode.File:
					Image fileImage = null;
					string filename = _capture.CaptureDetails.Filename;

					if (!string.IsNullOrEmpty(filename))
					{
						try
						{
							// Editor format
							if (filename.ToLower().EndsWith("." + OutputFormat.greenshot))
							{
								await DestinationHelper.GetDestination(BuildInDestinationEnum.Editor.ToString()).ExportCaptureAsync(true, null, _capture.CaptureDetails, token);
								break;
							}
						}
						catch (Exception e)
						{
							LOG.Error(e.Message, e);
							MessageBox.Show(string.Format(language.ErrorOpenfile, filename));
						}
						try
						{
							fileImage = ImageHelper.LoadImage(filename);
						}
						catch (Exception e)
						{
							LOG.Error(e.Message, e);
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
						if (_capture.CaptureDetails.HasDestination(BuildInDestinationEnum.Picker.ToString()))
						{
							_capture.CaptureDetails.ClearDestinations();
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(BuildInDestinationEnum.Editor.ToString()));
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(BuildInDestinationEnum.Picker.ToString()));
						}
						else
						{
							_capture.CaptureDetails.ClearDestinations();
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(BuildInDestinationEnum.Editor.ToString()));
						}
						await HandleCaptureAsync(token);
					}
					break;
				case CaptureMode.LastRegion:
					if (!conf.LastCapturedRegion.IsEmpty)
					{
						_capture = WindowCapture.CaptureRectangle(_capture, conf.LastCapturedRegion);

						// Set capture title, fixing bug #3569703
						foreach (WindowDetails window in WindowDetails.GetVisibleWindows())
						{
							Point estimatedLocation = new Point(conf.LastCapturedRegion.X + (conf.LastCapturedRegion.Width/2), conf.LastCapturedRegion.Y + (conf.LastCapturedRegion.Height/2));
							if (window.Contains(estimatedLocation))
							{
								_selectedCaptureWindow = window;
								_capture.CaptureDetails.Title = _selectedCaptureWindow.Text;
								break;
							}
						}
						// Move cursor, fixing bug #3569703
						_capture.MoveMouseLocation(_capture.ScreenBounds.Location.X - _capture.Location.X, _capture.ScreenBounds.Location.Y - _capture.Location.Y);
						//capture.MoveElements(capture.ScreenBounds.Location.X - capture.Location.X, capture.ScreenBounds.Location.Y - capture.Location.Y);

						_capture.CaptureDetails.AddMetaData("source", "screen");
						SetDPI();
						await HandleCaptureAsync(token);
					}
					break;
				case CaptureMode.Region:
					// Check if a region is pre-supplied!
					if (Rectangle.Empty.Equals(_captureRect))
					{
						_capture = WindowCapture.CaptureScreen(_capture);
						_capture.CaptureDetails.AddMetaData("source", "screen");
						SetDPI();
						await CaptureWithFeedbackAsync(retrieveWindowDetailsTask, token);
					}
					else
					{
						_capture = WindowCapture.CaptureRectangle(_capture, _captureRect);
						_capture.CaptureDetails.AddMetaData("source", "screen");
						SetDPI();
						await HandleCaptureAsync(token);
					}
					break;
				default:
					LOG.Warn("Unknown capture mode: " + _captureMode);
					break;
			}
			// Wait for thread, otherwise we can't dipose the CaptureHelper
			await retrieveWindowDetailsTask;
			if (_capture != null)
			{
				LOG.Debug("Disposing capture");
				_capture.Dispose();
			}
			LOG.Debug("Ended MakeCaptureAsync");
		}

		/// <summary>
		/// Pre-Initialization for CaptureWithFeedback, this will get all the windows before we change anything
		/// </summary>
		private async Task<IList<WindowDetails>> PrepareForCaptureWithFeedbackAsync(CancellationToken token = default(CancellationToken))
		{
			var result = new List<WindowDetails>();
			var appLauncherWindow = WindowDetails.GetAppLauncher();
			if (appLauncherWindow != null && appLauncherWindow.Visible)
			{
				result.Add(appLauncherWindow);
				// TODO: do not return when Windows 10???
				return result;
			}
			await Task.Run(() =>
			{
				// Force children retrieval, sometimes windows close on losing focus and this is solved by caching
				int goLevelDeep = conf.WindowCaptureAllChildLocations ? 20 : 3;
                var visibleWindows = from window in WindowDetails.GetMetroApps().Concat(WindowDetails.GetAllWindows())
					where window.Visible && (window.WindowRectangle.Width != 0 && window.WindowRectangle.Height != 0)
					select window;

				// Start Enumeration of "active" windows
				foreach (var window in visibleWindows)
				{
					// Make sure the details are retrieved once
					window.FreezeDetails();

					window.GetChildren(goLevelDeep);
					result.Add(window);
				}
			}, token).ConfigureAwait(false);
			return result;
		}

		private void AddConfiguredDestination()
		{
			foreach (string destinationDesignation in conf.OutputDestinations)
			{
				ILegacyDestination destination = DestinationHelper.GetDestination(destinationDesignation);
				if (destination != null)
				{
					_capture.CaptureDetails.AddDestination(destination);
				}
			}
		}

		/// <summary>
		/// If a balloon tip is show for a taken capture, this handles the click on it
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OpenCaptureOnClick(object sender, EventArgs e)
		{
			SurfaceMessageEventArgs eventArgs = MainForm.Instance.NotifyIcon.Tag as SurfaceMessageEventArgs;
			if (eventArgs == null)
			{
				LOG.Warn("OpenCaptureOnClick called without SurfaceMessageEventArgs");
				RemoveEventHandler(sender, e);
				return;
			}
			ISurface surface = eventArgs.Surface;
			if (surface != null && eventArgs.MessageType == SurfaceMessageTyp.FileSaved)
			{
				if (!string.IsNullOrEmpty(surface.LastSaveFullPath))
				{
					string errorMessage = null;

					try
					{
						var psi = new ProcessStartInfo("explorer.exe");
						psi.Arguments = Path.GetDirectoryName(surface.LastSaveFullPath);
						psi.UseShellExecute = false;
						using (Process p = new Process())
						{
							p.StartInfo = psi;
							p.Start();
						}
					}
					catch (Exception ex)
					{
						errorMessage = ex.Message;
					}
					// Added fallback for when the explorer can't be found
					if (errorMessage != null)
					{
						try
						{
							string windowsPath = Environment.GetEnvironmentVariable("SYSTEMROOT");
							string explorerPath = Path.Combine(windowsPath, "explorer.exe");
							if (File.Exists(explorerPath))
							{
								var psi = new ProcessStartInfo(explorerPath);
								psi.Arguments = Path.GetDirectoryName(surface.LastSaveFullPath);
								psi.UseShellExecute = false;
								using (var p = new Process())
								{
									p.StartInfo = psi;
									p.Start();
								}
								errorMessage = null;
							}
						}
						catch
						{
						}
					}
					if (errorMessage != null)
					{
						MessageBox.Show(string.Format("{0}\r\nexplorer.exe {1}", errorMessage, surface.LastSaveFullPath), "explorer.exe", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
			else if (surface != null && surface.UploadUri != null)
			{
				Process.Start(surface.UploadUri.AbsoluteUri);
			}
			LOG.DebugFormat("Deregistering the BalloonTipClicked");
			RemoveEventHandler(sender, e);
		}

		private void RemoveEventHandler(object sender, EventArgs e)
		{
			MainForm.Instance.NotifyIcon.BalloonTipClicked -= OpenCaptureOnClick;
			MainForm.Instance.NotifyIcon.BalloonTipClosed -= RemoveEventHandler;
			MainForm.Instance.NotifyIcon.Tag = null;
		}

		/// <summary>
		/// This is the SufraceMessageEvent receiver
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
		{
			if (eventArgs == null || string.IsNullOrEmpty(eventArgs.Message))
			{
				return;
			}
			switch (eventArgs.MessageType)
			{
				case SurfaceMessageTyp.Error:
					MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", eventArgs.Message, ToolTipIcon.Error);
					break;
				case SurfaceMessageTyp.Info:
					MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", eventArgs.Message, ToolTipIcon.Info);
					break;
				case SurfaceMessageTyp.FileSaved:
				case SurfaceMessageTyp.UploadedUri:
					// Show a balloon and register an event handler to open the "capture" for if someone clicks the balloon.
					MainForm.Instance.NotifyIcon.BalloonTipClicked += OpenCaptureOnClick;
					MainForm.Instance.NotifyIcon.BalloonTipClosed += RemoveEventHandler;
					// Store for later usage
					MainForm.Instance.NotifyIcon.Tag = eventArgs;
					MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", eventArgs.Message, ToolTipIcon.Info);
					break;
			}
		}


		private async Task HandleCaptureAsync(CancellationToken token = default(CancellationToken))
		{
			// Flag to see if the image was "exported" so the FileEditor doesn't
			// ask to save the file as long as nothing is done.
			bool outputMade = false;

			// Make sure the user sees that the capture is made
			if (_capture.CaptureDetails.CaptureMode == CaptureMode.File || _capture.CaptureDetails.CaptureMode == CaptureMode.Clipboard)
			{
				// Maybe not "made" but the original is still there... somehow
				outputMade = true;
			}
			else
			{
				// Make sure the resolution is set correctly!
				if (_capture.CaptureDetails != null && _capture.Image != null)
				{
					((Bitmap) _capture.Image).SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
				}
				await DoCaptureFeedbackAsync(token);
			}

			LOG.Debug("A capture of: " + _capture.CaptureDetails.Title);

			// check if someone has passed a destination
			if (_capture.CaptureDetails.CaptureDestinations == null || _capture.CaptureDetails.CaptureDestinations.Count == 0)
			{
				AddConfiguredDestination();
			}

			// Create Surface with capture, this way elements can be added automatically (like the mouse cursor)
			var surface = new Surface(_capture);
			surface.Modified = !outputMade;

			// Register notify events if this is wanted			
			if (conf.ShowTrayNotification && !conf.HideTrayicon)
			{
				surface.SurfaceMessage += SurfaceMessageReceived;
			}
			// Let the processors do their job
			foreach (var processor in ProcessorHelper.GetAllProcessors())
			{
				if (processor.isActive)
				{
					LOG.InfoFormat("Calling processor {0}", processor.Description);
					processor.ProcessCapture(surface, _capture.CaptureDetails);
				}
			}

			// As the surfaces copies the reference to the image, make sure the image is not being disposed (a trick to save memory)
			_capture.Image = null;

			// Get CaptureDetails as we need it even after the capture is disposed
			var captureDetails = _capture.CaptureDetails;
			bool canDisposeSurface = true;

			if (captureDetails.HasDestination(BuildInDestinationEnum.Picker.ToString()))
			{
				await DestinationHelper.ExportCaptureAsync(false, BuildInDestinationEnum.Picker.ToString(), surface, captureDetails, token);
				captureDetails.CaptureDestinations.Clear();
				canDisposeSurface = false;
			}

			// Disable capturing
			_captureMode = CaptureMode.None;
			// Dispose the capture, we don't need it anymore (the surface copied all information and we got the title (if any)).
			_capture.Dispose();
			_capture = null;

			int destinationCount = captureDetails.CaptureDestinations.Count;
			if (destinationCount > 0)
			{
				// Flag to detect if we need to create a temp file for the email
				// or use the file that was written
				foreach (var destination in captureDetails.CaptureDestinations)
				{
					if (BuildInDestinationEnum.Picker.ToString().Equals(destination.Designation))
					{
						continue;
					}
					LOG.InfoFormat("Calling destination {0}", destination.Description);

					var exportInformation = await destination.ExportCaptureAsync(false, surface, captureDetails, token);
					if (BuildInDestinationEnum.Editor.ToString().Equals(destination.Designation) && exportInformation.ExportMade)
					{
						canDisposeSurface = false;
					}
				}
			}
			if (canDisposeSurface)
			{
				surface.Dispose();
			}
		}

		private bool CaptureActiveWindow()
		{
			bool presupplied = false;
			LOG.Debug("CaptureActiveWindow");
			if (_selectedCaptureWindow != null)
			{
				LOG.Debug("Using supplied window");
				presupplied = true;
			}
			else
			{
				_selectedCaptureWindow = WindowDetails.GetActiveWindow();
				if (_selectedCaptureWindow != null)
				{
					LOG.DebugFormat("Capturing window: {0} with {1}", _selectedCaptureWindow.Text, _selectedCaptureWindow.WindowRectangle);
				}
			}
			if (_selectedCaptureWindow == null || (!presupplied && _selectedCaptureWindow.Iconic))
			{
				LOG.Warn("No window to capture!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}
			if (!presupplied && _selectedCaptureWindow != null && _selectedCaptureWindow.Iconic)
			{
				// Restore the window making sure it's visible!
				// This is done mainly for a screen capture, but some applications like Excel and TOAD have weird behaviour!
				_selectedCaptureWindow.Restore();
			}
			_selectedCaptureWindow = _selectedCaptureWindow.WindowToCapture();
			if (_selectedCaptureWindow == null)
			{
				LOG.Warn("No window to capture, after SelectCaptureWindow!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}
			// Fix for Bug #3430560 
			conf.LastCapturedRegion = _selectedCaptureWindow.WindowRectangle;
			bool returnValue = CaptureWindow(_selectedCaptureWindow, _capture, conf.WindowCaptureMode) != null;
			return returnValue;
		}

		/// <summary>
		/// Check if Process uses PresentationFramework.dll -> meaning it uses WPF
		/// </summary>
		/// <param name="process">Proces to check for the presentation framework</param>
		/// <returns>true if the process uses WPF</returns>
		private static bool isWPF(Process process)
		{
			if (process != null)
			{
				try
				{
					foreach (ProcessModule module in process.Modules)
					{
						if (module.ModuleName.StartsWith("PresentationFramework"))
						{
							LOG.InfoFormat("Found that Process {0} uses {1}, assuming it's using WPF", process.ProcessName, module.FileName);
							return true;
						}
					}
				}
				catch (Exception)
				{
					// Access denied on the modules
					LOG.WarnFormat("No access on the modules from process {0}, assuming WPF is used.", process.ProcessName);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Capture the supplied Window
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
			Rectangle windowRectangle = windowToCapture.WindowRectangle;

			// When Vista & DWM (Aero) enabled
			bool dwmEnabled = Dwm.IsDwmEnabled;
			// get process name to be able to exclude certain processes from certain capture modes
			using (Process process = windowToCapture.Process)
			{
				bool isAutoMode = windowCaptureMode == WindowCaptureMode.Auto;
				// For WindowCaptureMode.Auto we check:
				// 1) Is window IE, use IE Capture
				// 2) Is Windows >= Vista & DWM enabled: use DWM
				// 3) Otherwise use GDI (Screen might be also okay but might lose content)
				if (isAutoMode)
				{
					if (conf.IECapture && IECaptureHelper.IsIEWindow(windowToCapture))
					{
						try
						{
							ICapture ieCapture = IECaptureHelper.CaptureIE(captureForWindow, windowToCapture);
							if (ieCapture != null)
							{
								return ieCapture;
							}
						}
						catch (Exception ex)
						{
							LOG.WarnFormat("Problem capturing IE, skipping to normal capture. Exception message was: {0}", ex.Message);
						}
					}

					// Take default screen
					windowCaptureMode = WindowCaptureMode.Screen;

					// Change to GDI, if allowed
					if (!windowToCapture.IsMetroApp && WindowCapture.IsGdiAllowed(process))
					{
						if (!dwmEnabled && isWPF(process))
						{
							// do not use GDI, as DWM is not enabled and the application uses PresentationFramework.dll -> isWPF
							LOG.InfoFormat("Not using GDI for windows of process {0}, as the process uses WPF", process.ProcessName);
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
				else if (windowCaptureMode == WindowCaptureMode.Aero || windowCaptureMode == WindowCaptureMode.AeroTransparent)
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
				else if (windowCaptureMode == WindowCaptureMode.GDI && !WindowCapture.IsGdiAllowed(process))
				{
					// GDI not allowed, take screen
					windowCaptureMode = WindowCaptureMode.Screen;
				}

				LOG.InfoFormat("Capturing window with mode {0}", windowCaptureMode);
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
								tmpCapture = windowToCapture.CaptureGDIWindow(captureForWindow);
								if (tmpCapture != null)
								{
									// check if GDI capture any good, by comparing it with the screen content
									int blackCountGDI = ImageHelper.CountColor(tmpCapture.Image, Color.Black, false);
									int GDIPixels = tmpCapture.Image.Width*tmpCapture.Image.Height;
									int blackPercentageGDI = (blackCountGDI*100)/GDIPixels;
									if (blackPercentageGDI >= 1)
									{
										int screenPixels = windowRectangle.Width*windowRectangle.Height;
										using (ICapture screenCapture = new Capture())
										{
											screenCapture.CaptureDetails = captureForWindow.CaptureDetails;
											if (WindowCapture.CaptureRectangleFromDesktopScreen(screenCapture, windowRectangle) != null)
											{
												int blackCountScreen = ImageHelper.CountColor(screenCapture.Image, Color.Black, false);
												int blackPercentageScreen = (blackCountScreen*100)/screenPixels;
												if (screenPixels == GDIPixels)
												{
													// "easy compare", both have the same size
													// If GDI has more black, use the screen capture.
													if (blackPercentageGDI > blackPercentageScreen)
													{
														LOG.Debug("Using screen capture, as GDI had additional black.");
														// changeing the image will automatically dispose the previous
														tmpCapture.Image = screenCapture.Image;
														// Make sure it's not disposed, else the picture is gone!
														screenCapture.NullImage();
													}
												}
												else if (screenPixels < GDIPixels)
												{
													// Screen capture is cropped, window is outside of screen
													if (blackPercentageGDI > 50 && blackPercentageGDI > blackPercentageScreen)
													{
														LOG.Debug("Using screen capture, as GDI had additional black.");
														// changeing the image will automatically dispose the previous
														tmpCapture.Image = screenCapture.Image;
														// Make sure it's not disposed, else the picture is gone!
														screenCapture.NullImage();
													}
												}
												else
												{
													// Use the GDI capture by doing nothing
													LOG.Debug("This should not happen, how can there be more screen as GDI pixels?");
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
								tmpCapture = windowToCapture.CaptureDWMWindow(captureForWindow, windowCaptureMode, isAutoMode);
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
								LOG.Error("Problem capturing", e);
								return null;
							}
							break;
					}
				}
			}

			if (captureForWindow != null)
			{
				if (windowToCapture != null)
				{
					captureForWindow.CaptureDetails.Title = windowToCapture.Text;
				}
			}

			return captureForWindow;
		}

		private void SetDPI()
		{
			// Workaround for proble with DPI retrieval, the FromHwnd activates the window...
			WindowDetails previouslyActiveWindow = WindowDetails.GetActiveWindow();
			// Workaround for changed DPI settings in Windows 7
			using (Graphics graphics = Graphics.FromHwnd(MainForm.Instance.Handle))
			{
				_capture.CaptureDetails.DpiX = graphics.DpiX;
				_capture.CaptureDetails.DpiY = graphics.DpiY;
			}
			if (previouslyActiveWindow != null)
			{
				// Set previouslyActiveWindow as foreground window
				previouslyActiveWindow.ToForeground();
			}
			if (_capture.CaptureDetails != null && _capture.Image != null)
			{
				((Bitmap) _capture.Image).SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
			}
		}

		#region capture with feedback

		private async Task CaptureWithFeedbackAsync(Task<IList<WindowDetails>> retrieveWindowsTask, CancellationToken token = default(CancellationToken))
		{
			LOG.Debug("CaptureWithFeedbackAsync start");

			using (var captureForm = new CaptureForm(_capture, retrieveWindowsTask))
			{
				// Make sure the form is hidden after showing, even if an exception occurs, so all errors will be shown
				DialogResult result = DialogResult.Cancel;
				try
				{
					result = captureForm.ShowDialog(MainForm.Instance);
				}
				finally
				{
					captureForm.Hide();
				}
				if (result == DialogResult.OK)
				{
					_selectedCaptureWindow = captureForm.SelectedCaptureWindow;
					_captureRect = captureForm.CaptureRectangle;
					// Get title
					if (_selectedCaptureWindow != null)
					{
						_capture.CaptureDetails.Title = _selectedCaptureWindow.Text;
					}

					if (_captureRect.Height > 0 && _captureRect.Width > 0)
					{
						// Take the captureRect, this already is specified as bitmap coordinates
						_capture.Crop(_captureRect);

						// save for re-capturing later and show recapture context menu option
						// Important here is that the location needs to be offsetted back to screen coordinates!
						Rectangle tmpRectangle = _captureRect;
						tmpRectangle.Offset(_capture.ScreenBounds.Location.X, _capture.ScreenBounds.Location.Y);
						conf.LastCapturedRegion = tmpRectangle;
						await HandleCaptureAsync(token);
					}
				}
			}
		}

		#endregion
	}
}