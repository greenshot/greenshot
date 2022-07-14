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
using System.Globalization;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.IEInterop;
using log4net;
using IServiceProvider = Greenshot.Base.Interop.IServiceProvider;

namespace Greenshot.Helpers.IEInterop
{
    public class DocumentContainer
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(DocumentContainer));
        private const int E_ACCESSDENIED = unchecked((int)0x80070005L);
        private static readonly Guid IID_IWebBrowserApp = new("0002DF05-0000-0000-C000-000000000046");
        private static readonly Guid IID_IWebBrowser2 = new("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E");
        private static int _counter;
        private readonly NativePoint _startLocation = NativePoint.Empty;
        private IHTMLDocument2 _document2;
        private IHTMLDocument3 _document3;
        private NativeRect _viewportRectangle = NativeRect.Empty;
        private bool _isDtd;
        private WindowDetails _contentWindow;
        private double _zoomLevelX = 1;
        private double _zoomLevelY = 1;

        private DocumentContainer(IHTMLWindow2 frameWindow, WindowDetails contentWindow, DocumentContainer parent)
        {
            //IWebBrowser2 webBrowser2 = frame as IWebBrowser2;
            //IHTMLDocument2 document2 = webBrowser2.Document as IHTMLDocument2;
            IHTMLDocument2 document2 = GetDocumentFromWindow(frameWindow);
            try
            {
                LOG.DebugFormat("frameWindow.name {0}", frameWindow.name);
                Name = frameWindow.name;
            }
            catch
            {
                // Ignore
            }

            try
            {
                LOG.DebugFormat("document2.url {0}", document2.url);
            }
            catch
            {
                // Ignore
            }

            try
            {
                LOG.DebugFormat("document2.title {0}", document2.title);
            }
            catch
            {
                // Ignore
            }

            Parent = parent;
            // Calculate startLocation for the frames
            IHTMLWindow2 window2 = document2.parentWindow;
            IHTMLWindow3 window3 = (IHTMLWindow3)window2;
            NativePoint contentWindowLocation = contentWindow.WindowRectangle.Location;
            int x = window3.screenLeft - contentWindowLocation.X;
            int y = window3.screenTop - contentWindowLocation.Y;

            // Release IHTMLWindow 2+3 com objects
            ReleaseCom(window2);
            ReleaseCom(window3);

            _startLocation = new NativePoint(x, y);
            Init(document2, contentWindow);
        }

        public DocumentContainer(IHTMLDocument2 document2, WindowDetails contentWindow)
        {
            Init(document2, contentWindow);
            LOG.DebugFormat("Creating DocumentContainer for Document {0} found in window with rectangle {1}", Name, SourceRectangle);
        }

        /// <summary>
        /// Helper method to release com objects
        /// </summary>
        /// <param name="comObject"></param>
        private void ReleaseCom(object comObject)
        {
            if (comObject != null)
            {
                Marshal.ReleaseComObject(comObject);
            }
        }

        /// <summary>
        /// Private helper method for the constructors
        /// </summary>
        /// <param name="document2">IHTMLDocument2</param>
        /// <param name="contentWindow">WindowDetails</param>
        private void Init(IHTMLDocument2 document2, WindowDetails contentWindow)
        {
            _document2 = document2;
            _contentWindow = contentWindow;
            _document3 = document2 as IHTMLDocument3;
            // Check what access method is needed for the document
            IHTMLDocument5 document5 = (IHTMLDocument5)document2;

            //compatibility mode affects how height is computed
            _isDtd = false;
            try
            {
                if (_document3?.documentElement != null && !document5.compatMode.Equals("BackCompat"))
                {
                    _isDtd = true;
                }
            }
            catch (Exception ex)
            {
                LOG.Error("Error checking the compatibility mode:");
                LOG.Error(ex);
            }
            // Do not release IHTMLDocument5 com object, as this also gives problems with the document2!
            //Marshal.ReleaseComObject(document5);

            NativeRect clientRectangle = contentWindow.WindowRectangle;
            try
            {
                IHTMLWindow2 window2 = document2.parentWindow;
                //IHTMLWindow3 window3 = (IHTMLWindow3)document2.parentWindow;
                IHTMLScreen screen = window2.screen;
                IHTMLScreen2 screen2 = (IHTMLScreen2)screen;
                if (Parent != null)
                {
                    // Copy parent values
                    _zoomLevelX = Parent._zoomLevelX;
                    _zoomLevelY = Parent._zoomLevelY;
                    _viewportRectangle = Parent._viewportRectangle;
                }
                else
                {
                    //DisableScrollbars(document2);

                    // Calculate zoom level
                    _zoomLevelX = screen2.deviceXDPI / (double)screen2.logicalXDPI;
                    _zoomLevelY = screen2.deviceYDPI / (double)screen2.logicalYDPI;

                    // Calculate the viewport rectangle, needed if there is a frame around the html window
                    LOG.DebugFormat("Screen {0}x{1}", ScaleX(screen.width), ScaleY(screen.height));
                    //LOG.DebugFormat("Screen location {0},{1}", window3.screenLeft, window3.screenTop);
                    LOG.DebugFormat("Window rectangle {0}", clientRectangle);
                    LOG.DebugFormat("Client size {0}x{1}", ClientWidth, ClientHeight);
                    int diffX = clientRectangle.Width - ClientWidth;
                    int diffY = clientRectangle.Height - ClientHeight;
                    // If there is a border around the inner window, the diff == 4
                    // If there is a border AND a scrollbar the diff == 20
                    if (diffX is 4 or >= 20 && diffY is 4 or >= 20)
                    {
                        var viewportOffset = new NativePoint(2, 2);
                        var viewportSize = new NativeSize(ClientWidth, ClientHeight);
                        _viewportRectangle = new NativeRect(viewportOffset, viewportSize);
                        LOG.DebugFormat("viewportRect {0}", _viewportRectangle);
                    }
                }

                LOG.DebugFormat("Zoomlevel {0}, {1}", _zoomLevelX, _zoomLevelY);
                // Release com objects
                ReleaseCom(window2);
                ReleaseCom(screen);
                ReleaseCom(screen2);
            }
            catch (Exception e)
            {
                LOG.Warn("Can't get certain properties for documents, using default. Due to: ", e);
            }

            try
            {
                LOG.DebugFormat("Calculated location {0} for {1}", _startLocation, document2.title);
                if (Name == null)
                {
                    Name = document2.title;
                }
            }
            catch (Exception e)
            {
                LOG.Warn("Problem while trying to get document title!", e);
            }

            try
            {
                Url = document2.url;
            }
            catch (Exception e)
            {
                LOG.Warn("Problem while trying to get document url!", e);
            }

            SourceLocation = new NativePoint(ScaleX(_startLocation.X), ScaleY(_startLocation.Y));
            DestinationLocation = new NativePoint(ScaleX(_startLocation.X), ScaleY(_startLocation.Y));

            if (Parent != null)
            {
                return;
            }

            try
            {
                IHTMLFramesCollection2 frameCollection = (IHTMLFramesCollection2)document2.frames;
                for (int frame = 0; frame < frameCollection.length; frame++)
                {
                    try
                    {
                        IHTMLWindow2 frameWindow = frameCollection.item(frame);
                        DocumentContainer frameData = new(frameWindow, contentWindow, this);
                        // check if frame is hidden
                        if (!frameData.IsHidden)
                        {
                            LOG.DebugFormat("Creating DocumentContainer for Frame {0} found in window with rectangle {1}", frameData.Name, frameData.SourceRectangle);
                            Frames.Add(frameData);
                        }
                        else
                        {
                            LOG.DebugFormat("Skipping frame {0}", frameData.Name);
                        }

                        // Clean up frameWindow
                        ReleaseCom(frameWindow);
                    }
                    catch (Exception e)
                    {
                        LOG.Warn("Problem while trying to get information from a frame, skipping the frame!", e);
                    }
                }

                // Clean up collection
                ReleaseCom(frameCollection);
            }
            catch (Exception ex)
            {
                LOG.Warn("Problem while trying to get the frames, skipping!", ex);
            }

            try
            {
                // Correct iframe locations
                foreach (IHTMLElement frameElement in _document3.getElementsByTagName("IFRAME"))
                {
                    try
                    {
                        CorrectFrameLocations(frameElement);
                        // Clean up frameElement
                        ReleaseCom(frameElement);
                    }
                    catch (Exception e)
                    {
                        LOG.Warn("Problem while trying to get information from an iframe, skipping the frame!", e);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Warn("Problem while trying to get the iframes, skipping!", ex);
            }
        }

        /// <summary>
        /// Correct the frame locations with the information
        /// </summary>
        /// <param name="frameElement"></param>
        private void CorrectFrameLocations(IHTMLElement frameElement)
        {
            long x = 0;
            long y = 0;
            IHTMLElement element = frameElement;
            IHTMLElement oldElement = null;
            do
            {
                x += element.offsetLeft;
                y += element.offsetTop;
                element = element.offsetParent;
                // Release element, but prevent the frameElement to be released
                if (oldElement != null)
                {
                    ReleaseCom(oldElement);
                }

                oldElement = element;
            } while (element != null);

            var elementLocation = new NativePoint((int)x, (int)y);
            IHTMLElement2 element2 = (IHTMLElement2)frameElement;
            IHTMLRect rec = element2.getBoundingClientRect();
            var elementBoundingLocation = new NativePoint(rec.left, rec.top);
            // Release IHTMLRect
            ReleaseCom(rec);
            LOG.DebugFormat("Looking for iframe to correct at {0}", elementBoundingLocation);
            foreach (DocumentContainer foundFrame in Frames)
            {
                NativePoint frameLocation = foundFrame.SourceLocation;
                if (frameLocation.Equals(elementBoundingLocation))
                {
                    // Match found, correcting location
                    LOG.DebugFormat("Correcting frame from {0} to {1}", frameLocation, elementLocation);
                    foundFrame.SourceLocation = elementLocation;
                    foundFrame.DestinationLocation = elementLocation;
                }
                else
                {
                    LOG.DebugFormat("{0} != {1}", frameLocation, elementBoundingLocation);
                }
            }
        }

        /// <summary>
        /// A "workaround" for Access Denied when dealing with Frames from different domains
        /// </summary>
        /// <param name="htmlWindow">The IHTMLWindow2 to get the document from</param>
        /// <returns>IHTMLDocument2 or null</returns>
        private static IHTMLDocument2 GetDocumentFromWindow(IHTMLWindow2 htmlWindow)
        {
            if (htmlWindow == null)
            {
                LOG.Warn("htmlWindow == null");
                return null;
            }

            // First try the usual way to get the document.
            try
            {
                IHTMLDocument2 doc = htmlWindow.document;
                return doc;
            }
            catch (COMException comEx)
            {
                // I think COMException won't be ever fired but just to be sure ...
                if (comEx.ErrorCode != E_ACCESSDENIED)
                {
                    LOG.Warn("comEx.ErrorCode != E_ACCESSDENIED but", comEx);
                    return null;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // This error is okay, ignoring it
            }
            catch (Exception ex1)
            {
                LOG.Warn("Some error: ", ex1);
                // Any other error.
                return null;
            }

            // At this point the error was E_ACCESSDENIED because the frame contains a document from another domain.
            // IE tries to prevent a cross frame scripting security issue.
            try
            {
                // Convert IHTMLWindow2 to IWebBrowser2 using IServiceProvider.
                IServiceProvider sp = (IServiceProvider)htmlWindow;

                // Use IServiceProvider.QueryService to get IWebBrowser2 object.
                Guid webBrowserApp = IID_IWebBrowserApp;
                Guid webBrowser2 = IID_IWebBrowser2;
                sp.QueryService(ref webBrowserApp, ref webBrowser2, out var brws);

                // Get the document from IWebBrowser2.
                IWebBrowser2 browser = (IWebBrowser2)brws;

                return (IHTMLDocument2)browser.Document;
            }
            catch (Exception ex2)
            {
                LOG.Warn("another error: ", ex2);
            }

            return null;
        }

        public Color BackgroundColor
        {
            get
            {
                try
                {
                    string bgColor = (string)_document2.bgColor;
                    if (bgColor != null)
                    {
                        int rgbInt = int.Parse(bgColor.Substring(1), NumberStyles.HexNumber);
                        return Color.FromArgb(rgbInt >> 16, (rgbInt >> 8) & 255, rgbInt & 255);
                    }
                }
                catch (Exception ex)
                {
                    LOG.Error("Problem retrieving the background color: ", ex);
                }

                return Color.White;
            }
        }

        public NativeRect ViewportRectangle => _viewportRectangle;

        public WindowDetails ContentWindow => _contentWindow;

        public DocumentContainer Parent { get; set; }

        private int ScaleX(int physicalValue) => (int)Math.Round(physicalValue * _zoomLevelX, MidpointRounding.AwayFromZero);

        private int ScaleY(int physicalValue) => (int)Math.Round(physicalValue * _zoomLevelY, MidpointRounding.AwayFromZero);

        private int UnscaleX(int physicalValue) => (int)Math.Round(physicalValue / _zoomLevelX, MidpointRounding.AwayFromZero);

        private int UnscaleY(int physicalValue) => (int)Math.Round(physicalValue / _zoomLevelY, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Set/change an int attribute on a document
        /// </summary>
        public void SetAttribute(string attribute, int value) => SetAttribute(attribute, value.ToString());

        /// <summary>
        /// Set/change an attribute on a document
        /// </summary>
        /// <param name="attribute">Attribute to set</param>
        /// <param name="value">Value to set</param>
        public void SetAttribute(string attribute, string value)
        {
            var element = !_isDtd ? _document2.body : _document3.documentElement;
            element.setAttribute(attribute, value, 1);
            // Release IHTMLElement com object
            ReleaseCom(element);
        }

        /// <summary>
        /// Get the attribute from a document
        /// </summary>
        /// <param name="attribute">Attribute to get</param>
        /// <returns>object with the attribute value</returns>
        public object GetAttribute(string attribute)
        {
            var element = !_isDtd ? _document2.body : _document3.documentElement;
            var retVal = element.getAttribute(attribute, 1);
            // Release IHTMLElement com object
            ReleaseCom(element);
            return retVal;
        }

        /// <summary>
        /// Get the attribute as int from a document
        /// </summary>
        public int GetAttributeAsInt(string attribute)
        {
            int retVal = (int)GetAttribute(attribute);
            return retVal;
        }

        public int Id { get; } = _counter++;

        public string Name { get; private set; }

        public string Url { get; private set; }

        public bool IsHidden => ClientWidth == 0 || ClientHeight == 0;

        public int ClientWidth => ScaleX(GetAttributeAsInt("clientWidth"));

        public int ClientHeight => ScaleY(GetAttributeAsInt("clientHeight"));

        public int ScrollWidth => ScaleX(GetAttributeAsInt("scrollWidth"));

        public int ScrollHeight => ScaleY(GetAttributeAsInt("scrollHeight"));

        public NativePoint SourceLocation { get; set; }

        public NativeSize SourceSize => new(ClientWidth, ClientHeight);

        public NativeRect SourceRectangle => new(SourceLocation, SourceSize);

        public int SourceLeft => SourceLocation.X;

        public int SourceTop => SourceLocation.Y;

        public int SourceRight => SourceLocation.X + ClientWidth;

        public int SourceBottom => SourceLocation.Y + ClientHeight;

        public NativePoint DestinationLocation { get; set; }

        public NativeSize DestinationSize => new(ScrollWidth, ScrollHeight);

        public NativeRect DestinationRectangle => new(DestinationLocation, DestinationSize);

        public int DestinationLeft
        {
            get { return DestinationLocation.X; }
            set { DestinationLocation = DestinationLocation.ChangeX(value); }
        }

        public int DestinationTop
        {
            get { return DestinationLocation.Y; }
            set { DestinationLocation = DestinationLocation.ChangeY(value); }
        }

        public int DestinationRight => DestinationLocation.X + ScrollWidth;

        public int DestinationBottom => DestinationLocation.Y + ScrollHeight;

        public int ScrollLeft
        {
            get { return ScaleX(GetAttributeAsInt("scrollLeft")); }
            set { SetAttribute("scrollLeft", UnscaleX(value)); }
        }

        public int ScrollTop
        {
            get { return ScaleY(GetAttributeAsInt("scrollTop")); }
            set { SetAttribute("scrollTop", UnscaleY(value)); }
        }

        public IList<DocumentContainer> Frames { get; } = new List<DocumentContainer>();
    }
}