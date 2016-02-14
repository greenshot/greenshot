/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using Greenshot.Helpers.IEInterop;
using Dapplo.Windows.Native;
using mshtml;
using System.Threading;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Windows.Enums;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Controls;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Interop;

namespace Greenshot.Helpers
{
	/// <summary>
	/// The code for this helper comes from: http://www.codeproject.com/KB/graphics/IECapture.aspx
	/// The code is modified with some of the suggestions in different comments and there still were leaks which I fixed.
	/// On top I modified it to use the already available code in Greenshot.
	/// Many thanks to all the people who contributed here!
	/// </summary>
	public static class IECaptureHelper
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(IECaptureHelper));
		private static readonly ICoreConfiguration configuration = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();

		// Helper method to activate a certain IE Tab
		public static void ActivateIETab(WindowDetails ieWindowDetails, int tabIndex)
		{
			var directUiWindowDetails = IEHelper.GetDirectUI(ieWindowDetails);
			if (directUiWindowDetails != null)
			{
				// Bring window to the front
				ieWindowDetails.Restore();
				// Get accessible
				var ieAccessible = new Accessible(directUiWindowDetails.Handle);
				// Activate Tab
				ieAccessible.ActivateIETab(tabIndex);
			}
		}

		/// <summary>
		/// Return true if the supplied window has a sub-window which covers more than the supplied percentage
		/// </summary>
		/// <param name="someWindow">WindowDetails to check</param>
		/// <param name="minimumPercentage">min percentage</param>
		/// <returns></returns>
		public static bool IsMostlyIEWindow(WindowDetails someWindow, int minimumPercentage)
		{
			var ieWindow = someWindow.GetChild("Internet Explorer_Server");
			if (ieWindow != null)
			{
				var wholeClient = someWindow.ClientRectangle;
				var partClient = ieWindow.ClientRectangle;
				int percentage = (int) (100*((float) (partClient.Width*partClient.Height))/((float) (wholeClient.Width*wholeClient.Height)));
				Log.Information("Window {0}, ie part {1}, percentage {2}", wholeClient, partClient, percentage);
				if (percentage > minimumPercentage)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Does the supplied window have a IE part?
		/// </summary>
		/// <param name="someWindow"></param>
		/// <returns></returns>
		public static bool IsIEWindow(WindowDetails someWindow)
		{
			if ("IEFrame".Equals(someWindow.ClassName))
			{
				return true;
			}
			if (configuration.WindowClassesToCheckForIE != null && configuration.WindowClassesToCheckForIE.Contains(someWindow.ClassName))
			{
				return someWindow.GetChild("Internet Explorer_Server") != null;
			}
			return false;
		}

		/// <summary>
		/// Get Windows displaying an IE
		/// </summary>
		/// <returns>IEnumerable WindowDetails</returns>
		public static IEnumerable<WindowDetails> GetIEWindows()
		{
			foreach (var possibleIEWindow in WindowDetails.GetAllWindows())
			{
				if (possibleIEWindow.Text.Length == 0)
				{
					continue;
				}
				if (possibleIEWindow.ClientRectangle.IsEmpty)
				{
					continue;
				}
				if (IsIEWindow(possibleIEWindow))
				{
					yield return possibleIEWindow;
				}
			}
		}

		/// <summary>
		/// Simple check if IE is running
		/// </summary>
		/// <returns>bool</returns>
		public static bool IsIERunning()
		{
			return GetIEWindows().Any();
		}

		/// <summary>
		/// Gets a list of all IE Windows & tabs with the captions of the instances
		/// </summary>
		/// <returns>List of KeyValuePair with WindowDetails and string</returns>
		public static List<KeyValuePair<WindowDetails, string>> GetBrowserTabs()
		{
			var ieHandleList = new List<IntPtr>();
			var browserWindows = new Dictionary<WindowDetails, List<string>>();

			// Find the IE windows
			foreach (var ieWindow in GetIEWindows())
			{
				try
				{
					if (!ieHandleList.Contains(ieWindow.Handle))
					{
						if ("IEFrame".Equals(ieWindow.ClassName))
						{
							var directUIWD = IEHelper.GetDirectUI(ieWindow);
							if (directUIWD != null)
							{
								var accessible = new Accessible(directUIWD.Handle);
								browserWindows.Add(ieWindow, accessible.IETabCaptions);
							}
						}
						else if (configuration.WindowClassesToCheckForIE != null && configuration.WindowClassesToCheckForIE.Contains(ieWindow.ClassName))
						{
							var singleWindowText = new List<string>();
							try
							{
								var document2 = GetHtmlDocument(ieWindow);
								string title = document2.title;
								Marshal.ReleaseComObject(document2);
								if (string.IsNullOrEmpty(title))
								{
									singleWindowText.Add(ieWindow.Text);
								}
								else
								{
									singleWindowText.Add(ieWindow.Text + " - " + title);
								}
							}
							catch
							{
								singleWindowText.Add(ieWindow.Text);
							}
							browserWindows.Add(ieWindow, singleWindowText);
						}
						ieHandleList.Add(ieWindow.Handle);
					}
				}
				catch (Exception)
				{
					Log.Warning("Can't get Info from " + ieWindow.ClassName);
				}
			}

			var returnList = new List<KeyValuePair<WindowDetails, string>>();
			foreach (var windowDetails in browserWindows.Keys)
			{
				foreach (string tab in browserWindows[windowDetails])
				{
					returnList.Add(new KeyValuePair<WindowDetails, string>(windowDetails, tab));
				}
			}
			return returnList;
		}

		/// <summary>
		/// Helper Method to get the IHTMLDocument2
		/// </summary>
		/// <param name="mainWindow"></param>
		/// <returns></returns>
		private static IHTMLDocument2 GetHtmlDocument(WindowDetails mainWindow)
		{
			WindowDetails ieServer;
			if ("Internet Explorer_Server".Equals(mainWindow.ClassName))
			{
				ieServer = mainWindow;
			}
			else
			{
				ieServer = mainWindow.GetChild("Internet Explorer_Server");
			}
			if (ieServer == null)
			{
				Log.Warning("No Internet Explorer_Server for {0}", mainWindow.Text);
				return null;
			}

			IHTMLDocument2 document2 = null;
			uint windowMessage = User32.RegisterWindowMessage("WM_HTML_GETOBJECT");
			if (windowMessage == 0)
			{
				Log.Warning("Couldn't register WM_HTML_GETOBJECT");
				return null;
			}

			Log.Debug("Trying WM_HTML_GETOBJECT on {0}", ieServer.ClassName);
			UIntPtr response;
			User32.SendMessageTimeout(ieServer.Handle, windowMessage, IntPtr.Zero, IntPtr.Zero, SendMessageTimeoutFlags.SMTO_NORMAL, 5000, out response);
			if (response != UIntPtr.Zero)
			{
				document2 = (IHTMLDocument2) Accessible.ObjectFromLresult(response, typeof (IHTMLDocument).GUID, IntPtr.Zero);
				if (document2 == null)
				{
					Log.Error("No IHTMLDocument2 found");
					return null;
				}
			}
			else
			{
				Log.Error("No answer on WM_HTML_GETOBJECT.");
				return null;
			}
			return document2;
		}

		/// <summary>
		/// Helper method which will retrieve the IHTMLDocument2 for the supplied window,
		///  or return the first if none is supplied.
		/// </summary>
		/// <param name="browserWindow">The WindowDetails to get the IHTMLDocument2 for</param>
		/// <returns>The WindowDetails to which the IHTMLDocument2 belongs</returns>
		private static DocumentContainer CreateDocumentContainer(WindowDetails browserWindow)
		{
			DocumentContainer returnDocumentContainer = null;
			WindowDetails returnWindow = null;
			IHTMLDocument2 returnDocument2 = null;
			// alternative if no match
			WindowDetails alternativeReturnWindow = null;
			IHTMLDocument2 alternativeReturnDocument2 = null;

			// Find the IE windows
			foreach (var ieWindow in GetIEWindows())
			{
				Log.Debug("Processing {0} - {1}", ieWindow.ClassName, ieWindow.Text);

				Accessible ieAccessible = null;
				var directUIWD = IEHelper.GetDirectUI(ieWindow);
				if (directUIWD != null)
				{
					ieAccessible = new Accessible(directUIWD.Handle);
				}
				if (ieAccessible == null)
				{
					if (browserWindow != null)
					{
						Log.Information("Active Window is {0}", browserWindow.Text);
					}
					if (!ieWindow.Equals(browserWindow))
					{
						Log.Warning("No ieAccessible for {0}", ieWindow.Text);
						continue;
					}
					Log.Debug("No ieAccessible, but the active window is an IE window: {0}, ", ieWindow.Text);
				}

				try
				{
					// Get the Document
					var document2 = GetHtmlDocument(ieWindow);
					if (document2 == null)
					{
						continue;
					}

					// Get the content window handle for the shellWindow.Document
					IOleWindow oleWindow = (IOleWindow) document2;
					IntPtr contentWindowHandle = IntPtr.Zero;
					if (oleWindow != null)
					{
						oleWindow.GetWindow(out contentWindowHandle);
					}

					if (contentWindowHandle != IntPtr.Zero)
					{
						// Get the HTMLDocument to check the hasFocus
						// See: http://social.msdn.microsoft.com/Forums/en-US/vbgeneral/thread/60c6c95d-377c-4bf4-860d-390840fce31c/
						var document4 = (IHTMLDocument4) document2;

						if (document4.hasFocus())
						{
							Log.Debug("Matched focused document: {0}", document2.title);
							// Look no further, we got what we wanted!
							returnDocument2 = document2;
							returnWindow = new WindowDetails(contentWindowHandle);
							break;
						}
						try
						{
							if (ieWindow.Equals(browserWindow))
							{
								returnDocument2 = document2;
								returnWindow = new WindowDetails(contentWindowHandle);
								break;
							}
							if (ieAccessible != null && returnWindow == null && document2.title.Equals(ieAccessible.IEActiveTabCaption))
							{
								Log.Debug("Title: {0}", document2.title);
								returnDocument2 = document2;
								returnWindow = new WindowDetails(contentWindowHandle);
							}
							else
							{
								alternativeReturnDocument2 = document2;
								alternativeReturnWindow = new WindowDetails(contentWindowHandle);
							}
						}
						catch (Exception)
						{
							alternativeReturnDocument2 = document2;
							alternativeReturnWindow = new WindowDetails(contentWindowHandle);
						}
					}
				}
				catch (Exception e)
				{
					Log.Error(e, "Major problem: Problem retrieving Document from {Title}", ieWindow.Text);
				}
			}

			// check if we have something to return
			if (returnWindow != null)
			{
				// As it doesn't have focus, make sure it's active
				returnWindow.Restore();
				returnWindow.GetParent();

				// Create the container
				try
				{
					returnDocumentContainer = new DocumentContainer(returnDocument2, returnWindow);
				}
				catch (Exception e)
				{
					Log.Error(e, "Major problem: Problem retrieving Document.");
				}
			}

			if (returnDocumentContainer == null && alternativeReturnDocument2 != null)
			{
				// As it doesn't have focus, make sure it's active
				alternativeReturnWindow.Restore();
				alternativeReturnWindow.GetParent();
				// Create the container
				try
				{
					returnDocumentContainer = new DocumentContainer(alternativeReturnDocument2, alternativeReturnWindow);
				}
				catch (Exception e)
				{
					Log.Error(e, "Major problem: Problem retrieving Document.");
				}
			}
			return returnDocumentContainer;
		}

		/// <summary>
		/// Here the logic for capturing the IE Content is located
		/// </summary>
		/// <param name="capture">ICapture where the capture needs to be stored</param>
		/// <returns>ICapture with the content (if any)</returns>
		public static ICapture CaptureIE(ICapture capture)
		{
			return CaptureIE(capture, WindowDetails.GetActiveWindow());
		}

		/// <summary>
		/// Here the logic for capturing the IE Content is located
		/// </summary>
		/// <param name="capture">ICapture where the capture needs to be stored</param>
		/// <param name="windowToCapture">window to use</param>
		/// <returns>ICapture with the content (if any)</returns>
		public static ICapture CaptureIE(ICapture capture, WindowDetails windowToCapture)
		{
			if (windowToCapture == null)
			{
				windowToCapture = WindowDetails.GetActiveWindow();
			}

			bool isScreenCapture = configuration.IECaptureMode == WindowCaptureMode.Screen;
			WindowDetails topWindow = null;
			Point oldTopWindowLocation = Point.Empty;

			// Check if we capture from the screen, if this is the case we need to make sure the window is visible.
			if (isScreenCapture)
			{
				topWindow = windowToCapture;
				while (topWindow.HasParent)
				{
					topWindow = topWindow.GetParent();
				}
				// TODO: Z-Order needs to be restored!
				topWindow.ToForeground();
				oldTopWindowLocation = topWindow.Location;
				Point newLocation;
				if (!topWindow.Maximised && topWindow.GetVisibleLocation(out newLocation))
				{
					topWindow.Location = newLocation;
				}
			}

			// Show backgroundform after retrieving the active window..
			BackgroundForm backgroundForm = null;
			// do not show the please wait when we capture from the screen
			if (!isScreenCapture)
			{
				// TODO: Use PleaseWaitWindow
				backgroundForm = new BackgroundForm(language.ContextmenuCaptureie, language.WaitIeCapture);
				backgroundForm.Show();
			}
			//BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(language.GetString(IGreenshotLanguage.contextmenu_captureie), language.GetString(IGreenshotLanguage.wait_ie_capture));
			DocumentContainer documentContainer = null;
			try
			{
				//Get IHTMLDocument2 for the current active window
				documentContainer = CreateDocumentContainer(windowToCapture);

				// Nothing found
				if (documentContainer == null)
				{
					Log.Debug("Nothing to capture found");
					return null;
				}

				try
				{
					Log.Debug("Window class {0}", documentContainer.ContentWindow.ClassName);
					Log.Debug("Window location {0}", documentContainer.ContentWindow.Location);
				}
				catch (Exception ex)
				{
					Log.Warning("Error while logging information.", ex);
				}

				// bitmap to return
				Bitmap returnBitmap = null;
				try
				{
					Size pageSize = PrepareCapture(documentContainer, capture);
					returnBitmap = CapturePage(documentContainer, pageSize);
				}
				catch (Exception captureException)
				{
					Log.Error("Exception found, ignoring and returning nothing! Error was: ", captureException);
				}
				// TODO: Enable when the elements are usable again.
				// Capture the element on the page
				//try {
				//    if (configuration.IEFieldCapture && capture.CaptureDetails.HasDestination("Editor")) {
				//        // clear the current elements, as they are for the window itself
				//        capture.Elements.Clear();
				//        CaptureElement documentCaptureElement = documentContainer.CreateCaptureElements(pageSize);
				//        foreach(DocumentContainer frameDocument in documentContainer.Frames) {
				//            try {
				//                CaptureElement frameCaptureElement = frameDocument.CreateCaptureElements(Size.Empty);
				//                if (frameCaptureElement != null) {
				//                    documentCaptureElement.Children.Add(frameCaptureElement);
				//                }
				//            } catch (Exception ex) {
				//                Log.Warning("An error occurred while creating the capture elements: ", ex);
				//            }
				//        }
				//        capture.AddElement(documentCaptureElement);
				//        // Offset the elements, as they are "back offseted" later...
				//        Point windowLocation = documentContainer.ContentWindow.WindowRectangle.Location;
				//        capture.MoveElements(-(capture.ScreenBounds.Location.X-windowLocation.X), -(capture.ScreenBounds.Location.Y-windowLocation.Y));
				//    }
				//} catch (Exception elementsException) {
				//    Log.Warning("An error occurred while creating the capture elements: ", elementsException);
				//}


				if (returnBitmap == null)
				{
					return null;
				}

				// Store the bitmap for further processing
				capture.Image = returnBitmap;
				try
				{
					// Store the location of the window
					capture.Location = documentContainer.ContentWindow.Location;

					// The URL is available unter "document2.url" and can be used to enhance the meta-data etc.
					capture.CaptureDetails.AddMetaData("url", documentContainer.Url);
					// Store the title of the page
					if (documentContainer.Name != null)
					{
						capture.CaptureDetails.Title = documentContainer.Name;
					}
					else
					{
						capture.CaptureDetails.Title = windowToCapture.Text;
					}
				}
				catch (Exception ex)
				{
					Log.Warning("Problems getting some attributes...", ex);
				}

				// Store the URL of the page
				if (documentContainer.Url != null)
				{
					try
					{
						Uri uri = new Uri(documentContainer.Url);
						capture.CaptureDetails.AddMetaData("URL", uri.OriginalString);
						// As the URL can hardly be used in a filename, the following can be used
						if (!string.IsNullOrEmpty(uri.Scheme))
						{
							capture.CaptureDetails.AddMetaData("URL_SCHEME", uri.Scheme);
						}
						if (!string.IsNullOrEmpty(uri.DnsSafeHost))
						{
							capture.CaptureDetails.AddMetaData("URL_HOSTNAME", uri.DnsSafeHost);
						}
						if (!string.IsNullOrEmpty(uri.AbsolutePath))
						{
							capture.CaptureDetails.AddMetaData("URL_PATH", uri.AbsolutePath);
						}
						if (!string.IsNullOrEmpty(uri.Query))
						{
							capture.CaptureDetails.AddMetaData("URL_QUERY", uri.Query);
						}
						if (!string.IsNullOrEmpty(uri.UserInfo))
						{
							capture.CaptureDetails.AddMetaData("URL_USER", uri.UserInfo);
						}
						capture.CaptureDetails.AddMetaData("URL_PORT", uri.Port.ToString());
					}
					catch (Exception e)
					{
						Log.Warning("Exception when trying to use url in metadata " + documentContainer.Url, e);
					}
				}
				try
				{
					// Only move the mouse to correct for the capture offset
					capture.MoveMouseLocation(-documentContainer.ViewportRectangle.X, -documentContainer.ViewportRectangle.Y);
					// Used to be: capture.MoveMouseLocation(-(capture.Location.X + documentContainer.CaptureOffset.X), -(capture.Location.Y + documentContainer.CaptureOffset.Y));
				}
				catch (Exception ex)
				{
					Log.Warning("Error while correcting the mouse offset.", ex);
				}
			}
			finally
			{
				if (documentContainer != null)
				{
					documentContainer.Dispose();
				}
				if (backgroundForm != null)
				{
					// Always close the background form
					backgroundForm.CloseDialog();
				}
				if (topWindow != null && !topWindow.Maximised)
				{
					topWindow.Location = oldTopWindowLocation;
				}
			}
			return capture;
		}

		/// <summary>
		/// Prepare the calculates for all the frames, move and fit...
		/// </summary>
		/// <param name="documentContainer"></param>
		/// <param name="capture"></param>
		/// <returns>Size of the complete page</returns>
		private static Size PrepareCapture(DocumentContainer documentContainer, ICapture capture)
		{
			// Calculate the page size
			int pageWidth = documentContainer.ScrollWidth;
			int pageHeight = documentContainer.ScrollHeight;

			// Here we loop over all the frames and try to make sure they don't overlap
			bool movedFrame;
			do
			{
				movedFrame = false;
				foreach (DocumentContainer currentFrame in documentContainer.Frames)
				{
					foreach (DocumentContainer otherFrame in documentContainer.Frames)
					{
						if (otherFrame.Id == currentFrame.Id)
						{
							continue;
						}
						// check if we need to move
						if (otherFrame.DestinationRectangle.IntersectsWith(currentFrame.DestinationRectangle) && !otherFrame.SourceRectangle.IntersectsWith(currentFrame.SourceRectangle))
						{
							bool horizalResize = currentFrame.SourceSize.Width < currentFrame.DestinationSize.Width;
							bool verticalResize = currentFrame.SourceSize.Width < currentFrame.DestinationSize.Width;
							bool horizalMove = currentFrame.SourceLeft < currentFrame.DestinationLeft;
							bool verticalMove = currentFrame.SourceTop < currentFrame.DestinationTop;
							bool leftOf = currentFrame.SourceRight <= otherFrame.SourceLeft;
							bool belowOf = currentFrame.SourceBottom <= otherFrame.SourceTop;

							if ((horizalResize || horizalMove) && leftOf)
							{
								// Current frame resized horizontally, so move other horizontally
								Log.Debug("Moving Frame {0} horizontally to the right of {1}", otherFrame.Name, currentFrame.Name);
								otherFrame.DestinationLeft = currentFrame.DestinationRight;
								movedFrame = true;
							}
							else if ((verticalResize || verticalMove) && belowOf)
							{
								// Current frame resized vertically, so move other vertically
								Log.Debug("Moving Frame {0} vertically to the bottom of {1}", otherFrame.Name, currentFrame.Name);
								otherFrame.DestinationTop = currentFrame.DestinationBottom;
								movedFrame = true;
							}
							else
							{
								Log.Debug("Frame {0} intersects with {1}", otherFrame.Name, currentFrame.Name);
							}
						}
					}
				}
			}
			while (movedFrame);

			bool movedMouse = false;
			// Correct cursor location to be inside the window
			capture.MoveMouseLocation(-documentContainer.ContentWindow.Location.X, -documentContainer.ContentWindow.Location.Y);
			// See if the page has the correct size, as we capture the full frame content AND might have moved them
			// the normal pagesize will no longer be enough
			foreach (DocumentContainer frameData in documentContainer.Frames)
			{
				if (!movedMouse && frameData.SourceRectangle.Contains(capture.CursorLocation))
				{
					// Correct mouse cursor location for scrolled position (so it shows on the capture where it really was)
					capture.MoveMouseLocation(frameData.ScrollLeft, frameData.ScrollTop);
					movedMouse = true;
					// Apply any other offset changes
					int offsetX = frameData.DestinationLocation.X - frameData.SourceLocation.X;
					int offsetY = frameData.DestinationLocation.Y - frameData.SourceLocation.Y;
					capture.MoveMouseLocation(offsetX, offsetY);
				}

				//Get Frame Width & Height
				pageWidth = Math.Max(pageWidth, frameData.DestinationRight);
				pageHeight = Math.Max(pageHeight, frameData.DestinationBottom);
			}

			// If the mouse hasn't been moved, it wasn't on a frame. So correct the mouse according to the scroll position of the document
			if (!movedMouse)
			{
				// Correct mouse cursor location
				capture.MoveMouseLocation(documentContainer.ScrollLeft, documentContainer.ScrollTop);
			}

			// Limit the size as the editor currently can't work with sizes > short.MaxValue
			if (pageWidth > short.MaxValue)
			{
				Log.Warning("Capture has a width of {0} which bigger than the maximum supported {1}, cutting width to the maxium.", pageWidth, short.MaxValue);
				pageWidth = Math.Min(pageWidth, short.MaxValue);
			}
			if (pageHeight > short.MaxValue)
			{
				Log.Warning("Capture has a height of {0} which bigger than the maximum supported {1}, cutting height to the maxium", pageHeight, short.MaxValue);
				pageHeight = Math.Min(pageHeight, short.MaxValue);
			}
			return new Size(pageWidth, pageHeight);
		}

		/// <summary>
		/// Capture the actual page (document)
		/// </summary>
		/// <param name="documentContainer">The document wrapped in a container</param>
		/// <param name="pageSize"></param>
		/// <returns>Bitmap with the page content as an image</returns>
		private static Bitmap CapturePage(DocumentContainer documentContainer, Size pageSize)
		{
			var contentWindowDetails = documentContainer.ContentWindow;

			//Create a target bitmap to draw into with the calculated page size
			var returnBitmap = new Bitmap(pageSize.Width, pageSize.Height, PixelFormat.Format24bppRgb);
			using (var graphicsTarget = Graphics.FromImage(returnBitmap))
			{
				// Clear the target with the backgroundcolor
				var clearColor = documentContainer.BackgroundColor;
				Log.Debug("Clear color: {0}", clearColor);
				graphicsTarget.Clear(clearColor);

				// Get the base document & draw it
				DrawDocument(documentContainer, contentWindowDetails, graphicsTarget);

				// Loop over the frames and clear their source area so we don't see any artefacts
				foreach (var frameDocument in documentContainer.Frames)
				{
					using (Brush brush = new SolidBrush(clearColor))
					{
						graphicsTarget.FillRectangle(brush, frameDocument.SourceRectangle);
					}
				}
				// Loop over the frames and capture their content
				foreach (var frameDocument in documentContainer.Frames)
				{
					DrawDocument(frameDocument, contentWindowDetails, graphicsTarget);
				}
			}

			return returnBitmap;
		}

		/// <summary>
		/// This method takes the actual capture of the document (frame)
		/// </summary>
		/// <param name="documentContainer"></param>
		/// <param name="contentWindowDetails">Needed for referencing the location of the frame</param>
		/// <param name="graphicsTarget"></param>
		/// <returns>Bitmap with the capture</returns>
		private static void DrawDocument(DocumentContainer documentContainer, WindowDetails contentWindowDetails, Graphics graphicsTarget)
		{
			documentContainer.SetAttribute("scroll", 1);

			//Get Browser Window Width & Height
			int pageWidth = documentContainer.ScrollWidth;
			int pageHeight = documentContainer.ScrollHeight;
			if (pageWidth*pageHeight == 0)
			{
				Log.Warning("Empty page for DocumentContainer {0}: {1}", documentContainer.Name, documentContainer.Url);
				return;
			}

			//Get Screen Width & Height (this is better as the WindowDetails.ClientRectangle as the real visible parts are there!
			int viewportWidth = documentContainer.ClientWidth;
			int viewportHeight = documentContainer.ClientHeight;
			if (viewportWidth*viewportHeight == 0)
			{
				Log.Warning("Empty viewport for DocumentContainer {0}: {1}", documentContainer.Name, documentContainer.Url);
				return;
			}

			// Store the current location so we can set the browser back and use it for the mouse cursor
			int startLeft = documentContainer.ScrollLeft;
			int startTop = documentContainer.ScrollTop;

			Log.Debug("Capturing {4} with total size {0},{1} displayed with size {2},{3}", pageWidth, pageHeight, viewportWidth, viewportHeight, documentContainer.Name);

			// Variable used for looping horizontally
			int horizontalPage = 0;

			// The location of the browser, used as the destination into the bitmap target
			var targetOffset = new Point();

			// Loop of the pages and make a copy of the visible viewport
			while ((horizontalPage*viewportWidth) < pageWidth)
			{
				// Scroll to location
				documentContainer.ScrollLeft = viewportWidth*horizontalPage;
				targetOffset.X = documentContainer.ScrollLeft;

				// Variable used for looping vertically
				int verticalPage = 0;
				while ((verticalPage*viewportHeight) < pageHeight)
				{
					// Scroll to location
					documentContainer.ScrollTop = viewportHeight*verticalPage;
					//Shoot visible window
					targetOffset.Y = documentContainer.ScrollTop;

					// Draw the captured fragment to the target, but "crop" the scrollbars etc while capturing 
					var viewPortSize = new Size(viewportWidth, viewportHeight);
					var clientRectangle = new Rectangle(documentContainer.SourceLocation, viewPortSize);
					Image fragment;
					if (configuration.IECaptureMode == WindowCaptureMode.Screen)
					{
						// TODO: Task.Delay??
						Thread.Sleep(20);
						fragment = contentWindowDetails.CaptureFromScreen();
					}
					else
					{
						fragment = contentWindowDetails.PrintWindow();
					}
					if (fragment != null)
					{
						Log.Debug("Captured fragment size: {0}x{1}", fragment.Width, fragment.Height);
						try
						{
							// cut all junk, due to IE "border" we need to remove some parts
							var viewportRect = documentContainer.ViewportRectangle;
							if (!viewportRect.IsEmpty)
							{
								Log.Debug("Cropping to viewport: {0}", viewportRect);
								ImageHelper.Crop(ref fragment, ref viewportRect);
							}
							Log.Debug("Cropping to clientRectangle: {0}", clientRectangle);
							// Crop to clientRectangle
							if (ImageHelper.Crop(ref fragment, ref clientRectangle))
							{
								var targetLocation = new Point(documentContainer.DestinationLocation.X, documentContainer.DestinationLocation.Y);
								Log.Debug("Fragment targetLocation is {0}", targetLocation);
								targetLocation.Offset(targetOffset);
								Log.Debug("After offsetting the fragment targetLocation is {0}", targetLocation);
								Log.Debug("Drawing fragment of size {0} to {1}", fragment.Size, targetLocation);
								graphicsTarget.DrawImage(fragment, targetLocation);
								graphicsTarget.Flush();
							}
							else
							{
								// somehow we are capturing nothing!?
								Log.Warning("Crop of {0} failed?", documentContainer.Name);
								break;
							}
						}
						finally
						{
							fragment.Dispose();
						}
					}
					else
					{
						Log.Warning("Capture of {0} failed!", documentContainer.Name);
					}
					verticalPage++;
				}
				horizontalPage++;
			}
			// Return to where we were
			documentContainer.ScrollLeft = startLeft;
			documentContainer.ScrollTop = startTop;
		}
	}
}