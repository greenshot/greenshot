/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Globalization;
using System.Runtime.InteropServices;

using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Interop;
using Greenshot.Interop.IE;
using Greenshot.IniFile;

namespace Greenshot.Helpers.IEInterop {
	public class ElementContainer {
		public Rectangle rectangle;
		public string id;
		public Dictionary<string, string> attributes = new Dictionary<string, string>();
	}

	public class DocumentContainer {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(DocumentContainer));
		private static CoreConfiguration configuration = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly List<string> CAPTURE_TAGS = new List<string>();
        private const int  E_ACCESSDENIED = unchecked((int)0x80070005L);
        private static readonly Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
		private static readonly Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E");
		private static int counter = 0;
		private int id = counter++;
		private IHTMLDocument2 document2;
		private IHTMLDocument3 document3;
		private Point sourceLocation;
		private Point destinationLocation;
		private Point startLocation = Point.Empty;
		private Rectangle viewportRectangle = Rectangle.Empty;
		private string name = null;
		private string url;
		private bool isDTD;
		private DocumentContainer parent;
		private WindowDetails contentWindow;
		private double zoomLevelX = 1;
		private double zoomLevelY = 1;
		private List<DocumentContainer> frames = new List<DocumentContainer>();
		
		static DocumentContainer() {
			CAPTURE_TAGS.Add("LABEL");
			CAPTURE_TAGS.Add("DIV");
			CAPTURE_TAGS.Add("IMG");
			CAPTURE_TAGS.Add("INPUT");
			CAPTURE_TAGS.Add("BUTTON");
			CAPTURE_TAGS.Add("TD");
			CAPTURE_TAGS.Add("TR");
			CAPTURE_TAGS.Add("TH");
			CAPTURE_TAGS.Add("TABLE");
			CAPTURE_TAGS.Add("TBODY");
			CAPTURE_TAGS.Add("SPAN");
			CAPTURE_TAGS.Add("A");
			CAPTURE_TAGS.Add("UL");
			CAPTURE_TAGS.Add("LI");
			CAPTURE_TAGS.Add("H1");
			CAPTURE_TAGS.Add("H2");
			CAPTURE_TAGS.Add("H3");
			CAPTURE_TAGS.Add("H4");
			CAPTURE_TAGS.Add("H5");
			CAPTURE_TAGS.Add("FORM");
			CAPTURE_TAGS.Add("FIELDSET");
		}

		private DocumentContainer(IHTMLWindow2 frameWindow, WindowDetails contentWindow, DocumentContainer parent) {
			//IWebBrowser2 webBrowser2 = frame as IWebBrowser2;
			//IHTMLDocument2 document2 = webBrowser2.Document as IHTMLDocument2;
			IHTMLDocument2 document2 = GetDocumentFromWindow(frameWindow);
			try {
				LOG.DebugFormat("frameWindow.name {0}", frameWindow.name);
				name = frameWindow.name;
			} catch {
				
			}
			try {
				LOG.DebugFormat("document2.url {0}",document2.url);
			} catch {
				
			}
			try {
				LOG.DebugFormat("document2.title {0}", document2.title);
			} catch {
				
			}

			this.parent = parent;
			// Calculate startLocation for the frames
			IHTMLWindow3 window3 = (IHTMLWindow3)document2.parentWindow;
//			IHTMLElement element = window2.document.body;
//			long x = 0;
//			long y = 0;
//			do {
//				x += element.offsetLeft;
//				y += element.offsetTop;
//				element = element.offsetParent;
//			} while (element != null);
//			startLocation = new Point((int)x, (int)y);
			Point contentWindowLocation = contentWindow.WindowRectangle.Location;
			int x = window3.screenLeft - contentWindowLocation.X;
			int y = window3.screenTop - contentWindowLocation.Y;
			startLocation = new Point(x, y);
			Init(document2, contentWindow);
		}

		public DocumentContainer(IHTMLDocument2 document2, WindowDetails contentWindow) {
			Init(document2, contentWindow);
			LOG.DebugFormat("Creating DocumentContainer for Document {0} found in window with rectangle {1}", name, SourceRectangle);
		}
		
		/// <summary>
		/// Private helper method for the constructors
		/// </summary>
		/// <param name="document2">IHTMLDocument2</param>
		/// <param name="contentWindow">WindowDetails</param>
		private void Init(IHTMLDocument2 document2, WindowDetails contentWindow) {
			this.document2 = document2;
			this.contentWindow = contentWindow;
			this.document3 = document2 as IHTMLDocument3;
			// Check what access method is needed for the document
			IHTMLDocument5 document5 = (IHTMLDocument5)document2;

			//compatibility mode affects how height is computed
			isDTD = false;
			try {
				if ((document3.documentElement != null) && (!document5.compatMode.Equals("BackCompat"))) {
					isDTD = true;
				}
			} catch (Exception ex) {
				LOG.Error("Error checking the compatibility mode:");
				LOG.Error(ex);
			}
			Rectangle clientRectangle = contentWindow.WindowRectangle;
			try {
				IHTMLWindow2 window2 = (IHTMLWindow2)document2.parentWindow;
				//IHTMLWindow3 window3 = (IHTMLWindow3)document2.parentWindow;
				IHTMLScreen2 screen2 = (IHTMLScreen2)window2.screen;
				IHTMLScreen screen = window2.screen;
				if (parent != null) {
					// Copy parent values
					zoomLevelX = parent.zoomLevelX;
					zoomLevelY = parent.zoomLevelY;
					viewportRectangle = parent.viewportRectangle;
				} else {
					//DisableScrollbars(document2);

					// Calculate zoom level
					zoomLevelX = (double)screen2.deviceXDPI/(double)screen2.logicalXDPI;
					zoomLevelY = (double)screen2.deviceYDPI/(double)screen2.logicalYDPI;


					// Calculate the viewport rectangle, needed if there is a frame around the html window
					LOG.DebugFormat("Screen {0}x{1}", ScaleX(screen.width), ScaleY(screen.height));
					//LOG.DebugFormat("Screen location {0},{1}", window3.screenLeft, window3.screenTop);
					LOG.DebugFormat("Window rectangle {0}", clientRectangle);
					LOG.DebugFormat("Client size {0}x{1}", ClientWidth, ClientHeight);
					int diffX = clientRectangle.Width - ClientWidth;
					int diffY = clientRectangle.Height - ClientHeight;
					// If there is a border around the inner window, the diff == 4
					// If there is a border AND a scrollbar the diff == 20
					if ((diffX == 4 || diffX >= 20) && (diffY == 4 || diffY >= 20)) {
						Point viewportOffset = new Point(2, 2);
						Size viewportSize = new Size(ClientWidth, ClientHeight);
						viewportRectangle = new Rectangle(viewportOffset, viewportSize);
						LOG.DebugFormat("viewportRect {0}", viewportRectangle);
					}
				}
				LOG.DebugFormat("Zoomlevel {0}, {1}", zoomLevelX, zoomLevelY);
			} catch (Exception e) {
				LOG.Warn("Can't get certain properties for documents, using default. Due to: ", e);
			}


			try {
				LOG.DebugFormat("Calculated location {0} for {1}", startLocation, document2.title);
				if (name == null) {
					name = document2.title;
				}
			} catch (Exception e) {
				LOG.Warn("Problem while trying to get document title!", e);
			}

			try {
				url = document2.url;
			} catch (Exception e) {
				LOG.Warn("Problem while trying to get document url!", e);
			}
			sourceLocation = new Point(ScaleX((int)startLocation.X), ScaleY((int)startLocation.Y));
			destinationLocation = new Point(ScaleX((int)startLocation.X), ScaleY((int)startLocation.Y));
			
			if (parent != null) {
				return;
			}
			try {
				IHTMLFramesCollection2 frameCollection = (IHTMLFramesCollection2)document2.frames;
				for (int frame = 0; frame < frameCollection.length; frame++) {
					try {
						IHTMLWindow2 frameWindow = frameCollection.item(frame);
						DocumentContainer frameData = new DocumentContainer(frameWindow, contentWindow, this);
						// check if frame is hidden
						if (!frameData.isHidden) {
							LOG.DebugFormat("Creating DocumentContainer for Frame {0} found in window with rectangle {1}", frameData.name, frameData.SourceRectangle);
							frames.Add(frameData);
						} else {
							LOG.DebugFormat("Skipping frame {0}", frameData.Name);
						}
					} catch (Exception e) {
						LOG.Warn("Problem while trying to get information from a frame, skipping the frame!", e);
					}
				}
			} catch (Exception ex) {
				LOG.Warn("Problem while trying to get the frames, skipping!", ex);
			}

			try {
				// Correct iframe locations
				foreach (IHTMLElement frameElement in document3.getElementsByTagName("IFRAME")) {
					try {
						CorrectFrameLocations(frameElement);
					} catch (Exception e) {
						LOG.Warn("Problem while trying to get information from an iframe, skipping the frame!", e);
					}
				}
			} catch (Exception ex) {
				LOG.Warn("Problem while trying to get the iframes, skipping!", ex);
			}
		}
		
		private void DisableScrollbars(IHTMLDocument2 document2) {
			try {
				setAttribute("scroll","no");
				IHTMLBodyElement body = (IHTMLBodyElement)document2.body;
				body.scroll="no";
				document2.body.style.borderStyle = "none";
			} catch (Exception ex) {
				LOG.Warn("Can't disable scroll", ex);
			}
		}
		
		private void CorrectFrameLocations(IHTMLElement frameElement) {
			long x = 0;
			long y = 0;
			IHTMLElement element = frameElement;
			do {
				x += element.offsetLeft;
				y += element.offsetTop;
				element = element.offsetParent;
			} while (element != null);
			Point elementLocation = new Point((int)x, (int)y);
			IHTMLElement2 element2 = (IHTMLElement2)frameElement;
			IHTMLRect rec = element2.getBoundingClientRect();
			Point elementBoundingLocation = new Point(rec.left, rec.top);
			LOG.DebugFormat("Looking for iframe to correct at {0}", elementBoundingLocation);
			foreach(DocumentContainer foundFrame in frames) {
				Point frameLocation = foundFrame.SourceLocation;
				if (frameLocation.Equals(elementBoundingLocation)) {
					// Match found, correcting location
					LOG.DebugFormat("Correcting frame from {0} to {1}", frameLocation, elementLocation);
					foundFrame.SourceLocation = elementLocation;
					foundFrame.DestinationLocation = elementLocation;
				} else {
					LOG.DebugFormat("{0} != {1}", frameLocation, elementBoundingLocation);
				}
			}
		}

		/// <summary>
		/// A "workaround" for Access Denied when dealing with Frames from different domains
		/// </summary>
		/// <param name="htmlWindow">The IHTMLWindow2 to get the document from</param>
		/// <returns>IHTMLDocument2 or null</returns>
		private static IHTMLDocument2 GetDocumentFromWindow(IHTMLWindow2 htmlWindow) {
			if (htmlWindow == null) {
				LOG.Warn("htmlWindow == null");
				return null;
			}
			
			// First try the usual way to get the document.
			try {
				IHTMLDocument2 doc = htmlWindow.document;
				return doc;
			} catch (COMException comEx) {
				// I think COMException won't be ever fired but just to be sure ...
				if (comEx.ErrorCode != E_ACCESSDENIED) {
					LOG.Warn("comEx.ErrorCode != E_ACCESSDENIED but", comEx);
					return null;
				}
			} catch (System.UnauthorizedAccessException) {
				// This error is okay, ignoring it
			} catch (Exception ex1) {
				LOG.Warn("Some error: ", ex1);
				// Any other error.
				return null;
			}
			
			// At this point the error was E_ACCESSDENIED because the frame contains a document from another domain.
			// IE tries to prevent a cross frame scripting security issue.
			try {
				// Convert IHTMLWindow2 to IWebBrowser2 using IServiceProvider.
				Interop.IServiceProvider sp = (Interop.IServiceProvider)htmlWindow;
				
				// Use IServiceProvider.QueryService to get IWebBrowser2 object.
				Object brws = null;
				Guid webBrowserApp = IID_IWebBrowserApp.Clone();
				Guid webBrowser2 = IID_IWebBrowser2.Clone();
				sp.QueryService(ref webBrowserApp, ref webBrowser2, out brws);
				
				// Get the document from IWebBrowser2.
				IWebBrowser2 browser = (IWebBrowser2)(brws);
				
				return (IHTMLDocument2)browser.Document;
			} catch (Exception ex2) {
				LOG.Warn("another error: ", ex2);
			}
			return null;
		}

		/// <summary>
		/// Wrapper around getElementsByTagName
		/// </summary>
		/// <param name="tagName">tagName is the name of the tag to look for, e.g. "input"</param>
		/// <param name="retrieveAttributes">If true then all attributes are retrieved. This is slow!</param>
		/// <returns></returns>
		public List<ElementContainer> GetElementsByTagName(string tagName, string[] attributes) {
			List<ElementContainer> elements = new List<ElementContainer>();
			foreach(IHTMLElement element in document3.getElementsByTagName(tagName)) {
				if (element.offsetWidth <= 0 || element.offsetHeight <= 0) {
					// not visisble
					continue;
				}
				ElementContainer elementContainer = new ElementContainer();
				elementContainer.id = element.id;
				
				if (attributes != null) {
					foreach(string attributeName in attributes) {
						object attributeValue = element.getAttribute(attributeName, 0);
						if (attributeValue != null && attributeValue != DBNull.Value && !elementContainer.attributes.ContainsKey(attributeName)) {
							elementContainer.attributes.Add(attributeName, attributeValue.ToString());
						}
					}
				}
				
				Point elementLocation = new Point((int)element.offsetLeft, (int)element.offsetTop);
				elementLocation.Offset(this.DestinationLocation);
				IHTMLElement parent = element.offsetParent;
				while (parent != null) {
					elementLocation.Offset((int)parent.offsetLeft, (int)parent.offsetTop);
					parent = parent.offsetParent;
				}
				Rectangle elementRectangle = new Rectangle(elementLocation, new Size((int)element.offsetWidth, (int)element.offsetHeight));
				elementContainer.rectangle = elementRectangle;
				elements.Add(elementContainer);
			}
			return elements;
		}

		/// <summary>
		/// Create a CaptureElement for every element on the page, which can be used by the editor.
		/// </summary>
		/// <returns></returns>
		public CaptureElement CreateCaptureElements(Size documentSize) {
			LOG.DebugFormat("CreateCaptureElements for {0}", Name);
			IHTMLElement baseElement;
			if (!isDTD) {
				baseElement = document2.body;
			} else {
				baseElement = document3.documentElement;
			}
			IHTMLElement2 baseElement2 = baseElement as IHTMLElement2;
			IHTMLRect htmlRect = baseElement2.getBoundingClientRect();
			if (Size.Empty.Equals(documentSize)) {
				documentSize = new Size(ScrollWidth, ScrollHeight);
			}
			Rectangle baseElementBounds = new Rectangle(DestinationLocation.X + htmlRect.left, DestinationLocation.Y + htmlRect.top, documentSize.Width, documentSize.Height);
			if (baseElementBounds.Width <= 0 || baseElementBounds.Height <= 0) {
				// not visisble
				return null;
			}

			CaptureElement captureBaseElement = new CaptureElement(name, baseElementBounds);
		
			foreach(IHTMLElement bodyElement in baseElement.children) {
				if ("BODY".Equals(bodyElement.tagName)) {
					captureBaseElement.Children.AddRange(RecurseElements(bodyElement));
				}
			}
			return captureBaseElement;
		}
		
		/// <summary>
		/// Recurse into the document tree
		/// </summary>
		/// <param name="parentElement">IHTMLElement we want to recurse into</param>
		/// <returns>List of ICaptureElements with child elements</returns>
		private List<ICaptureElement> RecurseElements(IHTMLElement parentElement) {
			List<ICaptureElement> childElements = new List<ICaptureElement>();
			foreach(IHTMLElement element in parentElement.children) {
				string tagName = element.tagName;

				// Skip elements we aren't interested in
				if (!CAPTURE_TAGS.Contains(tagName)) {
					continue;
				}

				ICaptureElement captureElement = new CaptureElement(tagName);
				captureElement.Children.AddRange(RecurseElements(element));

				// Get Bounds
				IHTMLElement2 element2 = element as IHTMLElement2;
				IHTMLRect htmlRect = element2.getBoundingClientRect();
				
				int left = htmlRect.left;
				int top = htmlRect.top;
				int right = htmlRect.right;
				int bottom = htmlRect.bottom;
				
				// Offset
				left += DestinationLocation.X;
				top += DestinationLocation.Y;
				right += DestinationLocation.X;
				bottom += DestinationLocation.Y;

				// Fit to floating children
				foreach(ICaptureElement childElement in captureElement.Children) {
					//left = Math.Min(left, childElement.Bounds.Left);
					//top = Math.Min(top, childElement.Bounds.Top);
					right = Math.Max(right, childElement.Bounds.Right);
					bottom = Math.Max(bottom, childElement.Bounds.Bottom);
				}
				Rectangle bounds = new Rectangle(left, top, right-left, bottom-top);

				if (bounds.Width > 0 && bounds.Height > 0) {
					captureElement.Bounds = bounds;
					childElements.Add(captureElement);
				}
			}
			return childElements;
		}
		
		public Color BackgroundColor {
			get {
				if (document2.bgColor != null) {
					string bgColorString = (string)document2.bgColor;
					int rgbInt = Int32.Parse(bgColorString.Substring(1), NumberStyles.HexNumber);
					Color bgColor = Color.FromArgb(rgbInt >> 16, (rgbInt >> 8) & 255, rgbInt & 255);
					return bgColor;
				}
				return Color.White;
			}
		}

		public Rectangle ViewportRectangle {
			get {
				return viewportRectangle;
			}
		}

		public WindowDetails ContentWindow {
			get {
				return contentWindow;
			}
		}
		
		public DocumentContainer Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}
		
		private int ScaleX(int physicalValue) {
			return (int)Math.Round(physicalValue * zoomLevelX, MidpointRounding.AwayFromZero);
		}

		private int ScaleY(int physicalValue) {
			return (int)Math.Round(physicalValue * zoomLevelY, MidpointRounding.AwayFromZero);
		}

		private int UnscaleX(int physicalValue) {
			return (int)Math.Round(physicalValue / zoomLevelX, MidpointRounding.AwayFromZero);
		}

		private int UnscaleY(int physicalValue) {
			return (int)Math.Round(physicalValue / zoomLevelY, MidpointRounding.AwayFromZero);
		}

		/// <summary>
		/// Set/change an int attribute on a document
		/// </summary>
		public void setAttribute(string attribute, int value) {
			setAttribute(attribute, value.ToString());
		}

		/// <summary>
		/// Set/change an attribute on a document
		/// </summary>
		/// <param name="attribute">Attribute to set</param>
		/// <param name="value">Value to set</param>
		/// <param name="document2">The IHTMLDocument2</param>
		/// <param name="document3">The IHTMLDocument3</param>
		public void setAttribute(string attribute, string value) {
			if (!isDTD) {
				document2.body.setAttribute(attribute, value, 1);
			} else {
				document3.documentElement.setAttribute(attribute, value, 1);
			}
		}

		/// <summary>
		/// Get the attribute from a document
		/// </summary>
		/// <param name="attribute">Attribute to get</param>
		/// <param name="document2">The IHTMLDocument2</param>
		/// <param name="document3">The IHTMLDocument3</param>
		/// <returns>object with the attribute value</returns>
		public object getAttribute(string attribute) {
			object retVal = 0;
			if (!isDTD) {
				retVal = document2.body.getAttribute(attribute, 1);
			} else {
				retVal = document3.documentElement.getAttribute(attribute, 1);
			}
			return retVal;
		}
		
		/// <summary>
		/// Get the attribute as int from a document
		/// </summary>
		public int getAttributeAsInt(string attribute) {
			int retVal = (int)getAttribute(attribute);
			return retVal;
		}
		
		public int ID {
			get {
				return id;
			}
		}

		public string Name {
			get {
				return name;
			}
		}

		public string Url {
			get {
				return url;
			}
		}

		public bool isHidden {
			get {
				return ClientWidth == 0 || ClientHeight == 0;
			}
		}

		public int ClientWidth {
			get {
				return ScaleX(getAttributeAsInt("clientWidth"));
			}
		}

		public int ClientHeight {
			get {
				return ScaleY(getAttributeAsInt("clientHeight"));
			}
		}

		public int ScrollWidth {
			get {
				return ScaleX(getAttributeAsInt("scrollWidth"));
			}
		}

		public int ScrollHeight {
			get {
				return ScaleY(getAttributeAsInt("scrollHeight"));
			}
		}

		public Point SourceLocation {
			get {
				return sourceLocation;
			}
			set {
				sourceLocation = value;
			}
		}

		public Size SourceSize {
			get {
				return new Size(ClientWidth, ClientHeight);
			}
		}

		public Rectangle SourceRectangle {
			get {
				return new Rectangle(SourceLocation, SourceSize);
			}
		}

		public int SourceLeft {
			get {
				return sourceLocation.X;
			}
		}

		public int SourceTop {
			get {
				return sourceLocation.Y;
			}
		}

		public int SourceRight {
			get {
				return sourceLocation.X + ClientWidth;
			}
		}

		public int SourceBottom {
			get {
				return sourceLocation.Y + ClientHeight;
			}
		}

		public Point DestinationLocation {
			get {
				return destinationLocation;
			}
			set {
				destinationLocation = value;
			}

		}
		
		public Size DestinationSize {
			get {
				return new Size(ScrollWidth, ScrollHeight);
			}
		}
		
		public Rectangle DestinationRectangle {
			get {
				return new Rectangle(this.DestinationLocation, this.DestinationSize);
			}
		}

		public int DestinationLeft {
			get {
				return destinationLocation.X;
			}
			set {
				destinationLocation.X = value;
			}
		}

		public int DestinationTop {
			get {
				return destinationLocation.Y;
			}
			set {
				destinationLocation.Y = value;
			}
		}

		public int DestinationRight {
			get {
				return destinationLocation.X + ScrollWidth;
			}
		}

		public int DestinationBottom {
			get {
				return destinationLocation.Y + ScrollHeight;
			}
		}

		public int ScrollLeft {
			get{
				return ScaleX(getAttributeAsInt("scrollLeft"));
			}
			set {
				setAttribute("scrollLeft", UnscaleX(value));
			}
		}

		public int ScrollTop {
			get{
				return ScaleY(getAttributeAsInt("scrollTop"));
			}
			set {
				setAttribute("scrollTop", UnscaleY(value));
			}
		}
		
		public List<DocumentContainer> Frames {
			get {
				return frames;
			}
		}
	}
}
