﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.IEInterop;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interop;
using Greenshot.Configuration;
using Greenshot.Helpers.IEInterop;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// The code for this helper comes from: https://www.codeproject.com/KB/graphics/IECapture.aspx
    /// The code is modified with some of the suggestions in different comments and there still were leaks which I fixed.
    /// On top I modified it to use the already available code in Greenshot.
    /// Many thanks to all the people who contributed here!
    /// </summary>
    public static class IeCaptureHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IeCaptureHelper));
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

        // Helper method to activate a certain IE Tab
        public static void ActivateIeTab(WindowDetails ieWindowDetails, int tabIndex)
        {
            WindowDetails directUiWindowDetails = IEHelper.GetDirectUI(ieWindowDetails);
            if (directUiWindowDetails == null) return;

            // Bring window to the front
            ieWindowDetails.Restore();
            // Get accessible
            Accessible ieAccessible = new(directUiWindowDetails.Handle);
            // Activate Tab
            ieAccessible.ActivateIETab(tabIndex);
        }

        /// <summary>
        /// Does the supplied window have a IE part?
        /// </summary>
        /// <param name="someWindow"></param>
        /// <returns></returns>
        public static bool IsIeWindow(WindowDetails someWindow)
        {
            if ("IEFrame".Equals(someWindow.ClassName))
            {
                return true;
            }

            return CoreConfig.WindowClassesToCheckForIE?.Contains(someWindow.ClassName) == true
&& someWindow.GetChild("Internet Explorer_Server") != null;
        }

        /// <summary>
        /// Get Windows displaying an IE
        /// </summary>
        /// <returns>IEnumerable WindowDetails</returns>
        public static IEnumerable<WindowDetails> GetIeWindows()
        {
            foreach (var possibleIeWindow in WindowDetails.GetAllWindows())
            {
                if (possibleIeWindow.Text.Length == 0)
                {
                    continue;
                }

                if (possibleIeWindow.ClientRectangle.IsEmpty)
                {
                    continue;
                }

                if (IsIeWindow(possibleIeWindow))
                {
                    yield return possibleIeWindow;
                }
            }
        }

        /// <summary>
        /// Simple check if IE is running
        /// </summary>
        /// <returns>bool</returns>
        public static bool IsIeRunning() => GetIeWindows().Any();

        /// <summary>
        /// Gets a list of all IE Windows & tabs with the captions of the instances
        /// </summary>
        /// <returns>List with KeyValuePair of WindowDetails and string</returns>
        public static List<KeyValuePair<WindowDetails, string>> GetBrowserTabs()
        {
            var ieHandleList = new List<IntPtr>();
            var browserWindows = new Dictionary<WindowDetails, List<string>>();

            // Find the IE windows
            foreach (var ieWindow in GetIeWindows())
            {
                try
                {
                    if (ieHandleList.Contains(ieWindow.Handle))
                    {
                        continue;
                    }

                    if ("IEFrame".Equals(ieWindow.ClassName))
                    {
                        var directUiwd = IEHelper.GetDirectUI(ieWindow);
                        if (directUiwd != null)
                        {
                            var accessible = new Accessible(directUiwd.Handle);
                            browserWindows.Add(ieWindow, accessible.IETabCaptions);
                        }
                    }
                    else if (CoreConfig.WindowClassesToCheckForIE?.Contains(ieWindow.ClassName) == true)
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
                catch (Exception)
                {
                    Log.Warn("Can't get Info from " + ieWindow.ClassName);
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
            var ieServer = "Internet Explorer_Server".Equals(mainWindow.ClassName) ? mainWindow : mainWindow.GetChild("Internet Explorer_Server");
            if (ieServer == null)
            {
                Log.WarnFormat("No Internet Explorer_Server for {0}", mainWindow.Text);
                return null;
            }

            uint windowMessage = WindowsMessage.RegisterWindowsMessage("WM_HTML_GETOBJECT");
            if (windowMessage == 0)
            {
                Log.WarnFormat("Couldn't register WM_HTML_GETOBJECT");
                return null;
            }

            Log.DebugFormat("Trying WM_HTML_GETOBJECT on {0}", ieServer.ClassName);
            User32Api.SendMessageTimeout(ieServer.Handle, windowMessage, IntPtr.Zero, IntPtr.Zero, SendMessageTimeoutFlags.Normal, 5000, out UIntPtr response);
            IHTMLDocument2 document2;
            if (response != UIntPtr.Zero)
            {
                document2 = (IHTMLDocument2)Accessible.ObjectFromLresult(response, typeof(IHTMLDocument).GUID, IntPtr.Zero);
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
            foreach (WindowDetails ieWindow in GetIeWindows())
            {
                Log.DebugFormat("Processing {0} - {1}", ieWindow.ClassName, ieWindow.Text);

                Accessible ieAccessible = null;
                WindowDetails directUiwd = IEHelper.GetDirectUI(ieWindow);
                if (directUiwd != null)
                {
                    ieAccessible = new Accessible(directUiwd.Handle);
                }

                if (ieAccessible == null)
                {
                    if (browserWindow != null)
                    {
                        Log.InfoFormat("Active Window is {0}", browserWindow.Text);
                    }

                    if (!ieWindow.Equals(browserWindow))
                    {
                        Log.WarnFormat("No ieAccessible for {0}", ieWindow.Text);
                        continue;
                    }

                    Log.DebugFormat("No ieAccessible, but the active window is an IE window: {0}, ", ieWindow.Text);
                }

                try
                {
                    // Get the Document
                    IHTMLDocument2 document2 = GetHtmlDocument(ieWindow);
                    if (document2 == null)
                    {
                        continue;
                    }

                    // Get the content window handle for the shellWindow.Document
                    IOleWindow oleWindow = (IOleWindow)document2;
                    IntPtr contentWindowHandle = IntPtr.Zero;
                    oleWindow?.GetWindow(out contentWindowHandle);

                    if (contentWindowHandle != IntPtr.Zero)
                    {
                        // Get the HTMLDocument to check the hasFocus
                        // See: https://social.msdn.microsoft.com/Forums/en-US/vbgeneral/thread/60c6c95d-377c-4bf4-860d-390840fce31c/
                        IHTMLDocument4 document4 = (IHTMLDocument4)document2;

                        if (document4.hasFocus())
                        {
                            Log.DebugFormat("Matched focused document: {0}", document2.title);
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
                                Log.DebugFormat("Title: {0}", document2.title);
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
                    Log.ErrorFormat("Major problem: Problem retrieving Document from {0}", ieWindow.Text);
                    Log.Error(e);
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
                    Log.Error("Major problem: Problem retrieving Document.");
                    Log.Error(e);
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
                    Log.Error("Major problem: Problem retrieving Document.");
                    Log.Error(e);
                }
            }

            return returnDocumentContainer;
        }

        /// <summary>
        /// Here the logic for capturing the IE Content is located
        /// </summary>
        /// <param name="capture">ICapture where the capture needs to be stored</param>
        /// <param name="windowToCapture">window to use</param>
        /// <returns>ICapture with the content (if any)</returns>
        public static ICapture CaptureIe(ICapture capture, WindowDetails windowToCapture)
        {
            windowToCapture ??= WindowDetails.GetActiveWindow();

            // Show backgroundform after retrieving the active window..
            BackgroundForm backgroundForm = new(Language.GetString(LangKey.contextmenu_captureie), Language.GetString(LangKey.wait_ie_capture));
            backgroundForm.Show();
            //BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(language.GetString(LangKey.contextmenu_captureie), language.GetString(LangKey.wait_ie_capture));
            try
            {
                //Get IHTMLDocument2 for the current active window
                DocumentContainer documentContainer = CreateDocumentContainer(windowToCapture);

                // Nothing found
                if (documentContainer == null)
                {
                    Log.Debug("Nothing to capture found");
                    return null;
                }

                try
                {
                    Log.DebugFormat("Window class {0}", documentContainer.ContentWindow.ClassName);
                    Log.DebugFormat("Window location {0}", documentContainer.ContentWindow.Location);
                }
                catch (Exception ex)
                {
                    Log.Warn("Error while logging information.", ex);
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
                //                LOG.Warn("An error occurred while creating the capture elements: ", ex);
                //            }
                //        }
                //        capture.AddElement(documentCaptureElement);
                //        // Offset the elements, as they are "back offseted" later...
                //        Point windowLocation = documentContainer.ContentWindow.WindowRectangle.Location;
                //        capture.MoveElements(-(capture.ScreenBounds.Location.X-windowLocation.X), -(capture.ScreenBounds.Location.Y-windowLocation.Y));
                //    }
                //} catch (Exception elementsException) {
                //    LOG.Warn("An error occurred while creating the capture elements: ", elementsException);
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
                    capture.CaptureDetails.Title = documentContainer.Name ?? windowToCapture.Text;
                }
                catch (Exception ex)
                {
                    Log.Warn("Problems getting some attributes...", ex);
                }

                // Store the URL of the page
                if (documentContainer.Url != null)
                {
                    try
                    {
                        Uri uri = new(documentContainer.Url);
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
                        Log.Warn("Exception when trying to use url in metadata " + documentContainer.Url, e);
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
                    Log.Warn("Error while correcting the mouse offset.", ex);
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
                        if (!otherFrame.DestinationRectangle.IntersectsWith(currentFrame.DestinationRectangle) ||
                            otherFrame.SourceRectangle.IntersectsWith(currentFrame.SourceRectangle)) continue;

                        bool horizontalResize = currentFrame.SourceSize.Width < currentFrame.DestinationSize.Width;
                        bool verticalResize = currentFrame.SourceSize.Width < currentFrame.DestinationSize.Width;
                        bool horizontalMove = currentFrame.SourceLeft < currentFrame.DestinationLeft;
                        bool verticalMove = currentFrame.SourceTop < currentFrame.DestinationTop;
                        bool leftOf = currentFrame.SourceRight <= otherFrame.SourceLeft;
                        bool belowOf = currentFrame.SourceBottom <= otherFrame.SourceTop;

                        if ((horizontalResize || horizontalMove) && leftOf)
                        {
                            // Current frame resized horizontally, so move other horizontally
                            Log.DebugFormat("Moving Frame {0} horizontally to the right of {1}", otherFrame.Name, currentFrame.Name);
                            otherFrame.DestinationLeft = currentFrame.DestinationRight;
                            movedFrame = true;
                        }
                        else if ((verticalResize || verticalMove) && belowOf)
                        {
                            // Current frame resized vertically, so move other vertically
                            Log.DebugFormat("Moving Frame {0} vertically to the bottom of {1}", otherFrame.Name, currentFrame.Name);
                            otherFrame.DestinationTop = currentFrame.DestinationBottom;
                            movedFrame = true;
                        }
                        else
                        {
                            Log.DebugFormat("Frame {0} intersects with {1}", otherFrame.Name, currentFrame.Name);
                        }
                    }
                }
            } while (movedFrame);

            bool movedMouse = false;
            // Correct cursor location to be inside the window
            capture.MoveMouseLocation(-documentContainer.ContentWindow.Location.X, -documentContainer.ContentWindow.Location.Y);
            // See if the page has the correct size, as we capture the full frame content AND might have moved them
            // the normal pageSize will no longer be enough
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
                Log.WarnFormat("Capture has a width of {0} which bigger than the maximum supported {1}, cutting width to the maxium.", pageWidth, short.MaxValue);
                pageWidth = Math.Min(pageWidth, short.MaxValue);
            }

            if (pageHeight > short.MaxValue)
            {
                Log.WarnFormat("Capture has a height of {0} which bigger than the maximum supported {1}, cutting height to the maxium", pageHeight, short.MaxValue);
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
            WindowDetails contentWindowDetails = documentContainer.ContentWindow;

            //Create a target bitmap to draw into with the calculated page size
            Bitmap returnBitmap = new(pageSize.Width, pageSize.Height, PixelFormat.Format24bppRgb);
            using (Graphics graphicsTarget = Graphics.FromImage(returnBitmap))
            {
                // Clear the target with the backgroundcolor
                Color clearColor = documentContainer.BackgroundColor;
                Log.DebugFormat("Clear color: {0}", clearColor);
                graphicsTarget.Clear(clearColor);

                // Get the base document & draw it
                DrawDocument(documentContainer, contentWindowDetails, graphicsTarget);

                // Loop over the frames and clear their source area so we don't see any artefacts
                foreach (DocumentContainer frameDocument in documentContainer.Frames)
                {
                    using Brush brush = new SolidBrush(clearColor);
                    graphicsTarget.FillRectangle(brush, frameDocument.SourceRectangle);
                }

                // Loop over the frames and capture their content
                foreach (DocumentContainer frameDocument in documentContainer.Frames)
                {
                    DrawDocument(frameDocument, contentWindowDetails, graphicsTarget);
                }
            }

            return returnBitmap;
        }

        /// <summary>
        /// This method takes the actual capture of the document (frame)
        /// </summary>
        /// <param name="documentContainer">DocumentContainer</param>
        /// <param name="contentWindowDetails">Needed for referencing the location of the frame</param>
        /// <param name="graphicsTarget">Graphics</param>
        /// <returns>Bitmap with the capture</returns>
        private static void DrawDocument(DocumentContainer documentContainer, WindowDetails contentWindowDetails, Graphics graphicsTarget)
        {
            documentContainer.SetAttribute("scroll", 1);

            //Get Browser Window Width & Height
            int pageWidth = documentContainer.ScrollWidth;
            int pageHeight = documentContainer.ScrollHeight;
            if (pageWidth * pageHeight == 0)
            {
                Log.WarnFormat("Empty page for DocumentContainer {0}: {1}", documentContainer.Name, documentContainer.Url);
                return;
            }

            //Get Screen Width & Height (this is better as the WindowDetails.ClientRectangle as the real visible parts are there!
            int viewportWidth = documentContainer.ClientWidth;
            int viewportHeight = documentContainer.ClientHeight;
            if (viewportWidth * viewportHeight == 0)
            {
                Log.WarnFormat("Empty viewport for DocumentContainer {0}: {1}", documentContainer.Name, documentContainer.Url);
                return;
            }

            // Store the current location so we can set the browser back and use it for the mouse cursor
            int startLeft = documentContainer.ScrollLeft;
            int startTop = documentContainer.ScrollTop;

            Log.DebugFormat("Capturing {4} with total size {0},{1} displayed with size {2},{3}", pageWidth, pageHeight, viewportWidth, viewportHeight, documentContainer.Name);

            // Variable used for looping horizontally
            int horizontalPage = 0;

            // The location of the browser, used as the destination into the bitmap target
            NativePoint targetOffset = NativePoint.Empty;

            // Loop of the pages and make a copy of the visible viewport
            while (horizontalPage * viewportWidth < pageWidth)
            {
                // Scroll to location
                documentContainer.ScrollLeft = viewportWidth * horizontalPage;
                targetOffset = targetOffset.ChangeX(documentContainer.ScrollLeft);

                // Variable used for looping vertically
                int verticalPage = 0;
                while (verticalPage * viewportHeight < pageHeight)
                {
                    // Scroll to location
                    documentContainer.ScrollTop = viewportHeight * verticalPage;
                    //Shoot visible window
                    targetOffset = targetOffset.ChangeY(documentContainer.ScrollTop);

                    // Draw the captured fragment to the target, but "crop" the scrollbars etc while capturing 
                    NativeSize viewPortSize = new(viewportWidth, viewportHeight);
                    NativeRect clientRectangle = new(documentContainer.SourceLocation, viewPortSize);
                    Image fragment = contentWindowDetails.PrintWindow();
                    if (fragment != null)
                    {
                        Log.DebugFormat("Captured fragment size: {0}x{1}", fragment.Width, fragment.Height);
                        try
                        {
                            // cut all junk, due to IE "border" we need to remove some parts
                            NativeRect viewportRect = documentContainer.ViewportRectangle;
                            if (!viewportRect.IsEmpty)
                            {
                                Log.DebugFormat("Cropping to viewport: {0}", viewportRect);
                                ImageHelper.Crop(ref fragment, ref viewportRect);
                            }

                            Log.DebugFormat("Cropping to clientRectangle: {0}", clientRectangle);
                            // Crop to clientRectangle
                            if (ImageHelper.Crop(ref fragment, ref clientRectangle))
                            {
                                NativePoint targetLocation = new(documentContainer.DestinationLocation.X, documentContainer.DestinationLocation.Y);
                                Log.DebugFormat("Fragment targetLocation is {0}", targetLocation);
                                targetLocation = targetLocation.Offset(targetOffset);
                                Log.DebugFormat("After offsetting the fragment targetLocation is {0}", targetLocation);
                                Log.DebugFormat("Drawing fragment of size {0} to {1}", fragment.Size, targetLocation);
                                graphicsTarget.DrawImage(fragment, targetLocation);
                                graphicsTarget.Flush();
                            }
                            else
                            {
                                // somehow we are capturing nothing!?
                                Log.WarnFormat("Crop of {0} failed?", documentContainer.Name);
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
                        Log.WarnFormat("Capture of {0} failed!", documentContainer.Name);
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