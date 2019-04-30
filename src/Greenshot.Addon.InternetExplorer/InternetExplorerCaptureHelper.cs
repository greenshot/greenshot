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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Com;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32;
using Greenshot.Addon.InternetExplorer.InternetExplorerInterop;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;
using Greenshot.Gfx.Structs;
using mshtml;

namespace Greenshot.Addon.InternetExplorer
{
	/// <summary>
	///     The code for this helper comes from: http://www.codeproject.com/KB/graphics/IECapture.aspx
	///     The code is modified with some of the suggestions in different comments and there still were leaks which I fixed.
	///     On top I modified it to use the already available code in Greenshot.
	///     Many thanks to all the people who contributed here!
	/// </summary>
	public class InternetExplorerCaptureHelper
	{
		private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;

        public InternetExplorerCaptureHelper(ICoreConfiguration coreConfiguration)
        {
            _coreConfiguration = coreConfiguration;
        }

        /// <summary>
        /// Helper method to activate a certain IE Tab
        /// </summary>
        /// <param name="nativeIeWindow">IInteropWindow</param>
        /// <param name="tabIndex">int</param>
        public void ActivateIeTab(IInteropWindow nativeIeWindow, int tabIndex)
		{
            var directUiInteropWindow = InternetExplorerHelper.GetDirectUi(nativeIeWindow);
		    if (directUiInteropWindow == null)
		    {
		        return;
		    }

		    // Bring window to the front
		    nativeIeWindow.Restore();
		    // Get accessible
		    var ieAccessible = new Accessible(directUiInteropWindow.Handle);
		    // Activate Tab
		    ieAccessible.ActivateIETab(tabIndex);
		}

		/// <summary>
		///     Return true if the supplied window has a sub-window which covers more than the supplied percentage
		/// </summary>
		/// <param name="someWindow">InteropWindow to check</param>
		/// <param name="minimumPercentage">min percentage</param>
		/// <returns></returns>
		public bool IsMostlyIeWindow(IInteropWindow someWindow, int minimumPercentage)
		{
			var ieWindow = someWindow.GetChildren().FirstOrDefault(window => window.GetClassname() == "Internet Explorer_Server");
		    if (ieWindow == null)
		    {
		        return false;
		    }
		    var wholeClient = someWindow.GetInfo().ClientBounds;
		    var partClient = ieWindow.GetInfo().ClientBounds;
		    var percentage = (int) (100 * (float) (partClient.Width * partClient.Height) / (wholeClient.Width * wholeClient.Height));
		    Log.Info().WriteLine("Window {0}, ie part {1}, percentage {2}", wholeClient, partClient, percentage);
		    return percentage > minimumPercentage;
		}

		/// <summary>
		///     Does the supplied window have a IE part?
		/// </summary>
		/// <param name="someWindow"></param>
		/// <returns></returns>
		public bool IsIeWindow(IInteropWindow someWindow)
		{
			if ("IEFrame".Equals(someWindow.GetClassname()))
			{
				return true;
			}
			if (_coreConfiguration.WindowClassesToCheckForIE != null && _coreConfiguration.WindowClassesToCheckForIE.Contains(someWindow.Classname))
			{
				return someWindow.GetChildren().Any(window => window.GetClassname() == "Internet Explorer_Server");
			}
			return false;
		}

		/// <summary>
		///     Get Windows displaying an IE
		/// </summary>
		/// <returns>IEnumerable IInteropWindow</returns>
		public IEnumerable<IInteropWindow> GetIeWindows()
		{
		    return WindowsEnumerator.EnumerateWindows().Where(IsIeWindow);
		}

		/// <summary>
		///     Simple check if IE is running
		/// </summary>
		/// <returns>bool</returns>
		public bool IsIeRunning()
		{
			return GetIeWindows().Any();
		}

		/// <summary>
		///     Gets a list of all IE Windows and tabs with the captions of the instances
		/// </summary>
		/// <returns>List with KeyValuePair of InteropWindow and string</returns>
		public IList<KeyValuePair<IInteropWindow, string>> GetBrowserTabs()
		{
			var ieHandleList = new List<IntPtr>();
			var browserWindows = new Dictionary<IInteropWindow, List<string>>();

			// Find the IE windows
			foreach (var ieWindow in GetIeWindows())
			{
				try
				{
					if (ieHandleList.Contains(ieWindow.Handle))
					{
						continue;
					}
					if ("IEFrame".Equals(ieWindow.GetClassname()))
					{
						var directUiwd = InternetExplorerHelper.GetDirectUi(ieWindow);
						if (directUiwd != null)
						{
							var accessible = new Accessible(directUiwd.Handle);
							browserWindows.Add(ieWindow, accessible.IETabCaptions);
						}
					}
					else if (_coreConfiguration.WindowClassesToCheckForIE != null && _coreConfiguration.WindowClassesToCheckForIE.Contains(ieWindow.Classname))
					{
						var singleWindowText = new List<string>();
						try
						{
							var document2 = GetHtmlDocument(ieWindow);
							var title = document2.title;
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
				catch (Exception)
				{
					Log.Warn().WriteLine("Can't get Info from " + ieWindow.Classname);
				}
			}

			var returnList = new List<KeyValuePair<IInteropWindow, string>>();
			foreach (var nativeWindow in browserWindows.Keys)
			{
				foreach (var tab in browserWindows[nativeWindow])
				{
					returnList.Add(new KeyValuePair<IInteropWindow, string>(nativeWindow, tab));
				}
			}
			return returnList;
		}

		/// <summary>
		///     Helper Method to get the IHTMLDocument2
		/// </summary>
		/// <param name="mainWindow"></param>
		/// <returns></returns>
		private IHTMLDocument2 GetHtmlDocument(IInteropWindow mainWindow)
		{
			var ieServer = "Internet Explorer_Server".Equals(mainWindow.GetClassname())
				? mainWindow
				: mainWindow.GetChildren().FirstOrDefault(window => window.GetClassname() == "Internet Explorer_Server");
			if (ieServer == null)
			{
				Log.Warn().WriteLine("No Internet Explorer_Server for {0}", mainWindow.Text);
				return null;
			}

			var windowMessage = WindowsMessage.RegisterWindowsMessage("WM_HTML_GETOBJECT");
			if (windowMessage == 0)
			{
				Log.Warn().WriteLine("Couldn't register WM_HTML_GETOBJECT");
				return null;
			}

		    Log.Debug().WriteLine("Trying WM_HTML_GETOBJECT on {0}", ieServer.Classname);
		    if (User32Api.TrySendMessage(ieServer.Handle, windowMessage, IntPtr.Zero, out UIntPtr response))
		    {
		        var document2 = (IHTMLDocument2)Accessible.ObjectFromLresult(response, typeof(IHTMLDocument).GUID, IntPtr.Zero);
		        if (document2 != null)
		        {
		            return document2;
		        }
		        Log.Error().WriteLine(null, "No IHTMLDocument2 found");
		        return null;
		    }
		    Log.Error().WriteLine(null, "No answer on WM_HTML_GETOBJECT.");
		    return null;
		}

		/// <summary>
		///     Helper method which will retrieve the IHTMLDocument2 for the supplied window,
		///     or return the first if none is supplied.
		/// </summary>
		/// <param name="browserWindow">The InteropWindow to get the IHTMLDocument2 for</param>
		/// <returns>DocumentContainer</returns>
		private DocumentContainer CreateDocumentContainer(IInteropWindow browserWindow)
		{
			DocumentContainer returnDocumentContainer = null;
			InteropWindow returnWindow = null;
			IHTMLDocument2 returnDocument2 = null;
			// alternative if no match
			InteropWindow alternativeReturnWindow = null;
			IHTMLDocument2 alternativeReturnDocument2 = null;

			// Find the IE windows
			foreach (var ieWindow in GetIeWindows())
			{
				Log.Debug().WriteLine("Processing {0} - {1}", ieWindow.Classname, ieWindow.Text);

				Accessible ieAccessible = null;
				var directUiwd = InternetExplorerHelper.GetDirectUi(ieWindow);
				if (directUiwd != null)
				{
					ieAccessible = new Accessible(directUiwd.Handle);
				}
				if (ieAccessible == null)
				{
					if (browserWindow != null)
					{
						Log.Info().WriteLine("Active Window is {0}", browserWindow.Text);
					}
					if (!ieWindow.Equals(browserWindow))
					{
						Log.Warn().WriteLine("No ieAccessible for {0}", ieWindow.Text);
						continue;
					}
					Log.Debug().WriteLine("No ieAccessible, but the active window is an IE window: {0}, ", ieWindow.Text);
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
					var oleWindow = (IOleWindow) document2;
					var contentWindowHandle = IntPtr.Zero;
					oleWindow?.GetWindow(out contentWindowHandle);

					if (contentWindowHandle != IntPtr.Zero)
					{
						// Get the HTMLDocument to check the hasFocus
						// See: http://social.msdn.microsoft.com/Forums/en-US/vbgeneral/thread/60c6c95d-377c-4bf4-860d-390840fce31c/
						var document4 = (IHTMLDocument4) document2;

						if (document4.hasFocus())
						{
							Log.Debug().WriteLine("Matched focused document: {0}", document2.title);
							// Look no further, we got what we wanted!
							returnDocument2 = document2;
							returnWindow = InteropWindowFactory.CreateFor(contentWindowHandle);
							break;
						}
						try
						{
							if (ieWindow.Equals(browserWindow))
							{
								returnDocument2 = document2;
								returnWindow = InteropWindowFactory.CreateFor(contentWindowHandle);
								break;
							}
							if (ieAccessible != null && returnWindow == null && document2.title.Equals(ieAccessible.IEActiveTabCaption))
							{
								Log.Debug().WriteLine("Title: {0}", document2.title);
								returnDocument2 = document2;
								returnWindow = InteropWindowFactory.CreateFor(contentWindowHandle);
							}
							else
							{
								alternativeReturnDocument2 = document2;
								alternativeReturnWindow = InteropWindowFactory.CreateFor(contentWindowHandle);
							}
						}
						catch (Exception)
						{
							alternativeReturnDocument2 = document2;
							alternativeReturnWindow = InteropWindowFactory.CreateFor(contentWindowHandle);
						}
					}
				}
				catch (Exception e)
				{
					Log.Error().WriteLine("Major problem: Problem retrieving Document from {0}", ieWindow.Text);
					Log.Error().WriteLine(e);
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
					Log.Error().WriteLine(null, "Major problem: Problem retrieving Document.");
					Log.Error().WriteLine(e);
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
					Log.Error().WriteLine(null, "Major problem: Problem retrieving Document.");
					Log.Error().WriteLine(e);
				}
			}
			return returnDocumentContainer;
		}

		/// <summary>
		///     Here the logic for capturing the IE Content is located
		/// </summary>
		/// <param name="capture">ICapture where the capture needs to be stored</param>
		/// <param name="windowToCapture">window to use</param>
		/// <returns>ICapture with the content (if any)</returns>
		public ICapture CaptureIe(ICapture capture, IInteropWindow windowToCapture = null)
		{
			if (windowToCapture == null)
			{
				windowToCapture = InteropWindowQuery.GetForegroundWindow();
			}
			// Show backgroundform after retrieving the active window..
			var backgroundForm = new BackgroundForm("Internet Explorer", "Please wait while the page in Internet Explorer is captured...");
			backgroundForm.Show();
			//BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(language.GetString(LangKey.contextmenu_captureie), language.GetString(LangKey.wait_ie_capture));
			try
			{
				//Get IHTMLDocument2 for the current active window
				var documentContainer = CreateDocumentContainer(windowToCapture);

				// Nothing found
				if (documentContainer == null)
				{
					Log.Debug().WriteLine("Nothing to capture found");
					return null;
				}

				try
				{
					Log.Debug().WriteLine("Window class {0}", documentContainer.ContentWindow.GetClassname());
					Log.Debug().WriteLine("Window location {0}", documentContainer.ContentWindow.GetInfo().Bounds.Location);
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex, "Error while logging information.");
				}

				// bitmap to return
				IBitmapWithNativeSupport returnBitmap = null;
				try
				{
					var pageSize = PrepareCapture(documentContainer, capture);
					returnBitmap = CapturePage(documentContainer, pageSize);
				}
				catch (Exception captureException)
				{
					Log.Error().WriteLine(captureException, "Exception found, ignoring and returning nothing! Error was: ");
				}
				// TODO: Enable when the elements are usable again.
				// Capture the element on the page
				//try {
				//    if (CoreConfig.IEFieldCapture && capture.CaptureDetails.HasDestination("Editor")) {
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
				//                Log.Warn().WriteLine("An error occurred while creating the capture elements: ", ex);
				//            }
				//        }
				//        capture.AddElement(documentCaptureElement);
				//        // Offset the elements, as they are "back offseted" later...
				//        NativePoint windowLocation = documentContainer.ContentWindow.WindowRectangle.Location;
				//        capture.MoveElements(-(capture.ScreenBounds.Location.X-windowLocation.X), -(capture.ScreenBounds.Location.Y-windowLocation.Y));
				//    }
				//} catch (Exception elementsException) {
				//    Log.Warn().WriteLine("An error occurred while creating the capture elements: ", elementsException);
				//}


				if (returnBitmap == null)
				{
					return null;
				}

				// Store the bitmap for further processing
				capture.Bitmap = returnBitmap;
				try
				{
					// Store the location of the window
					capture.Location = documentContainer.ContentWindow.GetInfo().Bounds.Location;

					// The URL is available unter "document2.url" and can be used to enhance the meta-data etc.
					capture.CaptureDetails.AddMetaData("url", documentContainer.Url);
					// Store the title of the page
					capture.CaptureDetails.Title = documentContainer.Name ?? windowToCapture.Text;
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex, "Problems getting some attributes...");
				}

				// Store the URL of the page
				if (documentContainer.Url != null)
				{
					try
					{
						var uri = new Uri(documentContainer.Url);
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
						Log.Warn().WriteLine(e, "Exception when trying to use url in metadata " + documentContainer.Url);
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
					Log.Warn().WriteLine(ex, "Error while correcting the mouse offset.");
				}
			}
			finally
			{
				// Always close the background form
				backgroundForm.CloseDialog();
			}
			return capture;
		}

		/// <summary>
		///     Prepare the calculates for all the frames, move and fit...
		/// </summary>
		/// <param name="documentContainer"></param>
		/// <param name="capture"></param>
		/// <returns>Size of the complete page</returns>
		private Size PrepareCapture(DocumentContainer documentContainer, ICapture capture)
		{
			// Calculate the page size
			var pageWidth = documentContainer.ScrollWidth;
			var pageHeight = documentContainer.ScrollHeight;

			// Here we loop over all the frames and try to make sure they don't overlap
			bool movedFrame;
			do
			{
				movedFrame = false;
				foreach (var currentFrame in documentContainer.Frames)
				{
					foreach (var otherFrame in documentContainer.Frames)
					{
						if (otherFrame.Id == currentFrame.Id)
						{
							continue;
						}
						// check if we need to move
						if (otherFrame.DestinationRectangle.IntersectsWith(currentFrame.DestinationRectangle) && !otherFrame.SourceRectangle.IntersectsWith(currentFrame.SourceRectangle))
						{
							var horizalResize = currentFrame.SourceSize.Width < currentFrame.DestinationSize.Width;
							var verticalResize = currentFrame.SourceSize.Width < currentFrame.DestinationSize.Width;
							var horizalMove = currentFrame.SourceLeft < currentFrame.DestinationLeft;
							var verticalMove = currentFrame.SourceTop < currentFrame.DestinationTop;
							var leftOf = currentFrame.SourceRight <= otherFrame.SourceLeft;
							var belowOf = currentFrame.SourceBottom <= otherFrame.SourceTop;

							if ((horizalResize || horizalMove) && leftOf)
							{
								// Current frame resized horizontally, so move other horizontally
								Log.Debug().WriteLine("Moving Frame {0} horizontally to the right of {1}", otherFrame.Name, currentFrame.Name);
								otherFrame.DestinationLeft = currentFrame.DestinationRight;
								movedFrame = true;
							}
							else if ((verticalResize || verticalMove) && belowOf)
							{
								// Current frame resized vertically, so move other vertically
								Log.Debug().WriteLine("Moving Frame {0} vertically to the bottom of {1}", otherFrame.Name, currentFrame.Name);
								otherFrame.DestinationTop = currentFrame.DestinationBottom;
								movedFrame = true;
							}
							else
							{
								Log.Debug().WriteLine("Frame {0} intersects with {1}", otherFrame.Name, currentFrame.Name);
							}
						}
					}
				}
			} while (movedFrame);

			var movedMouse = false;
			// Correct cursor location to be inside the window
			var location = documentContainer.ContentWindow.GetInfo().Bounds.Location;
			capture.MoveMouseLocation(-location.X, -location.Y);
			// See if the page has the correct size, as we capture the full frame content AND might have moved them
			// the normal pagesize will no longer be enough
			foreach (var frameData in documentContainer.Frames)
			{
				if (!movedMouse && frameData.SourceRectangle.Contains(capture.CursorLocation))
				{
					// Correct mouse cursor location for scrolled position (so it shows on the capture where it really was)
					capture.MoveMouseLocation(frameData.ScrollLeft, frameData.ScrollTop);
					movedMouse = true;
					// Apply any other offset changes
					var offsetX = frameData.DestinationLocation.X - frameData.SourceLocation.X;
					var offsetY = frameData.DestinationLocation.Y - frameData.SourceLocation.Y;
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
				Log.Warn().WriteLine("Capture has a width of {0} which bigger than the maximum supported {1}, cutting width to the maxium.", pageWidth, short.MaxValue);
				pageWidth = Math.Min(pageWidth, short.MaxValue);
			}
			if (pageHeight > short.MaxValue)
			{
				Log.Warn().WriteLine("Capture has a height of {0} which bigger than the maximum supported {1}, cutting height to the maxium", pageHeight, short.MaxValue);
				pageHeight = Math.Min(pageHeight, short.MaxValue);
			}
			return new Size(pageWidth, pageHeight);
		}

		/// <summary>
		///     Capture the actual page (document)
		/// </summary>
		/// <param name="documentContainer">The document wrapped in a container</param>
		/// <param name="pageSize"></param>
		/// <returns>Bitmap with the page content as an image</returns>
		private IBitmapWithNativeSupport CapturePage(DocumentContainer documentContainer, Size pageSize)
		{
			var contentWindowDetails = documentContainer.ContentWindow;

			//Create a target bitmap to draw into with the calculated page size
			var returnBitmap = new UnmanagedBitmap<Bgr24>(pageSize.Width, pageSize.Height);
			using (var graphicsTarget = Graphics.FromImage(returnBitmap.NativeBitmap))
			{
				// Clear the target with the background color
				var clearColor = documentContainer.BackgroundColor;
				Log.Debug().WriteLine("Clear color: {0}", clearColor);
				graphicsTarget.Clear(clearColor);

				// Get the base document & draw it
				DrawDocument(documentContainer, contentWindowDetails, graphicsTarget);

				// Loop over the frames and clear their source area so we don't see any artifacts
				foreach (var frameDocument in documentContainer.Frames)
				{
					using (var brush = new SolidBrush(clearColor))
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
		///     This method takes the actual capture of the document (frame)
		/// </summary>
		/// <param name="documentContainer">DocumentContainer</param>
		/// <param name="contentWindowDetails">Needed for referencing the location of the frame</param>
		/// <param name="graphicsTarget">Graphics</param>
		/// <returns>Bitmap with the capture</returns>
		private void DrawDocument(DocumentContainer documentContainer, IInteropWindow contentWindowDetails, Graphics graphicsTarget)
		{
			documentContainer.SetAttribute("scroll", 1);

			//Get Browser Window Width & Height
			var pageWidth = documentContainer.ScrollWidth;
			var pageHeight = documentContainer.ScrollHeight;
			if (pageWidth * pageHeight == 0)
			{
				Log.Warn().WriteLine("Empty page for DocumentContainer {0}: {1}", documentContainer.Name, documentContainer.Url);
				return;
			}

			//Get Screen Width & Height (this is better as the WindowDetails.ClientRectangle as the real visible parts are there!
			var viewportWidth = documentContainer.ClientWidth;
			var viewportHeight = documentContainer.ClientHeight;
			if (viewportWidth * viewportHeight == 0)
			{
				Log.Warn().WriteLine("Empty viewport for DocumentContainer {0}: {1}", documentContainer.Name, documentContainer.Url);
				return;
			}

			// Store the current location so we can set the browser back and use it for the mouse cursor
			var startLeft = documentContainer.ScrollLeft;
			var startTop = documentContainer.ScrollTop;

			Log.Debug().WriteLine("Capturing {4} with total size {0},{1} displayed with size {2},{3}", pageWidth, pageHeight, viewportWidth, viewportHeight, documentContainer.Name);

			// Variable used for looping horizontally
			var horizontalPage = 0;

			// The location of the browser, used as the destination into the bitmap target
			var targetOffset = new NativePoint();

			// Loop of the pages and make a copy of the visible viewport
			while (horizontalPage * viewportWidth < pageWidth)
			{
				// Scroll to location
				documentContainer.ScrollLeft = viewportWidth * horizontalPage;
				targetOffset = targetOffset.ChangeX(documentContainer.ScrollLeft);

				// Variable used for looping vertically
				var verticalPage = 0;
				while (verticalPage * viewportHeight < pageHeight)
				{
					// Scroll to location
					documentContainer.ScrollTop = viewportHeight * verticalPage;
                    //Shoot visible window
                    targetOffset = targetOffset.ChangeY(documentContainer.ScrollTop);

					// Draw the captured fragment to the target, but "crop" the scrollbars etc while capturing 
					var viewPortSize = new Size(viewportWidth, viewportHeight);
					var clientRectangle = new NativeRect(documentContainer.SourceLocation, viewPortSize);
					var fragment = contentWindowDetails.PrintWindow();
					if (fragment != null)
					{
						Log.Debug().WriteLine("Captured fragment size: {0}x{1}", fragment.Width, fragment.Height);
						try
						{
							// cut all junk, due to IE "border" we need to remove some parts
							var viewportRect = documentContainer.ViewportRectangle;
							if (!viewportRect.IsEmpty)
							{
								Log.Debug().WriteLine("Cropping to viewport: {0}", viewportRect);
								BitmapHelper.Crop(ref fragment, ref viewportRect);
							}
							Log.Debug().WriteLine("Cropping to clientRectangle: {0}", clientRectangle);
							// Crop to clientRectangle
							if (BitmapHelper.Crop(ref fragment, ref clientRectangle))
							{
								var targetLocation = new NativePoint(documentContainer.DestinationLocation.X, documentContainer.DestinationLocation.Y);
								Log.Debug().WriteLine("Fragment targetLocation is {0}", targetLocation);
							    targetLocation = targetLocation.Offset(targetOffset);
								Log.Debug().WriteLine("After offsetting the fragment targetLocation is {0}", targetLocation);
								Log.Debug().WriteLine("Drawing fragment of size {0} to {1}", fragment.Size, targetLocation);
								graphicsTarget.DrawImage(fragment.NativeBitmap, targetLocation);
								graphicsTarget.Flush();
							}
							else
							{
								// somehow we are capturing nothing!?
								Log.Warn().WriteLine("Crop of {0} failed?", documentContainer.Name);
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
						Log.Warn().WriteLine("Capture of {0} failed!", documentContainer.Name);
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