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
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Greenshot.Core;
using mshtml;
using SHDocVw;
using IServiceProvider = Greenshot.CaptureCore.Interop.IServiceProvider;

#endregion

namespace Greenshot.CaptureCore.IEInterop
{
	public class DocumentContainer
	{
		private const int E_ACCESSDENIED = unchecked((int) 0x80070005L);
		private static readonly LogSource Log = new LogSource();
		private static readonly Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
		private static readonly Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E");
		private static int _counter;
		private Point _destinationLocation;
		private IHTMLDocument2 _document2;
		private IHTMLDocument3 _document3;
		private bool _isDtd;
		private Point _startLocation = Point.Empty;
		private double _zoomLevelX = 1;
		private double _zoomLevelY = 1;

		private DocumentContainer(IHTMLWindow2 frameWindow, WindowDetails contentWindow, DocumentContainer parent)
		{
			//IWebBrowser2 webBrowser2 = frame as IWebBrowser2;
			//IHTMLDocument2 _document2 = webBrowser2.Document as IHTMLDocument2;
			var document2 = GetDocumentFromWindow(frameWindow);
			try
			{
				Log.Debug().WriteLine("frameWindow._name {0}", frameWindow.name);
				Name = frameWindow.name;
			}
			catch
			{
				// ignored
			}
			try
			{
				Log.Debug().WriteLine("_document2._url {0}", document2.url);
			}
			catch
			{
				// ignored
			}
			try
			{
				Log.Debug().WriteLine("_document2.title {0}", document2.title);
			}
			catch
			{
				// ignored
			}

			Parent = parent;
			try
			{
				var contentWindowLocation = contentWindow.WindowRectangle.Location;
				int x, y;
				// Calculate _startLocation for the _frames
				var window2 = document2.parentWindow;
				try
				{
					var window3 = (IHTMLWindow3) window2;
					try
					{
						x = window3.screenLeft - contentWindowLocation.X;
						y = window3.screenTop - contentWindowLocation.Y;
					}
					finally
					{
						ReleaseCom(window3);
					}
				}
				finally
				{
					// Release IHTMLWindow 2+3 com objects
					ReleaseCom(window2);
				}

				_startLocation = new Point(x, y);
				Init(document2, contentWindow);
			}
			catch
			{
				ReleaseCom(document2);
				throw;
			}
		}

		public DocumentContainer(IHTMLDocument2 document2, WindowDetails contentWindow)
		{
			Init(document2, contentWindow);
			Log.Debug().WriteLine("Creating DocumentContainer for Document {0} found in window with rectangle {1}", Name, SourceRectangle);
		}

		public Color BackgroundColor
		{
			get
			{
				try
				{
					string bgColor = (string) _document2.bgColor;
					if (bgColor != null)
					{
						int rgbInt = int.Parse(bgColor.Substring(1), NumberStyles.HexNumber);
						return Color.FromArgb(rgbInt >> 16, (rgbInt >> 8) & 255, rgbInt & 255);
					}
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Problem retrieving the background color: ");
				}
				return Color.White;
			}
		}

		public int ClientHeight => ScaleY(GetAttributeAsInt("clientHeight"));

		public int ClientWidth => ScaleX(GetAttributeAsInt("clientWidth"));

		public WindowDetails ContentWindow { get; private set; }

		public int DestinationBottom => _destinationLocation.Y + ScrollHeight;

		public int DestinationLeft
		{
			get { return _destinationLocation.X; }
			set { _destinationLocation.X = value; }
		}

		public Point DestinationLocation
		{
			get { return _destinationLocation; }
			set { _destinationLocation = value; }
		}

		public Rectangle DestinationRectangle => new Rectangle(DestinationLocation, DestinationSize);

		public int DestinationRight => _destinationLocation.X + ScrollWidth;

		public Size DestinationSize => new Size(ScrollWidth, ScrollHeight);

		public int DestinationTop
		{
			get { return _destinationLocation.Y; }
			set { _destinationLocation.Y = value; }
		}

		public List<DocumentContainer> Frames { get; } = new List<DocumentContainer>();

		public int Id { get; } = _counter++;

		public bool IsHidden => (ClientWidth == 0) || (ClientHeight == 0);

		public string Name { get; private set; }

		public DocumentContainer Parent { get; set; }

		public int ScrollHeight => ScaleY(GetAttributeAsInt("scrollHeight"));

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

		public int ScrollWidth => ScaleX(GetAttributeAsInt("scrollWidth"));

		public int SourceBottom => SourceLocation.Y + ClientHeight;

		public int SourceLeft => SourceLocation.X;

		public Point SourceLocation { get; set; }

		public Rectangle SourceRectangle => new Rectangle(SourceLocation, SourceSize);

		public int SourceRight => SourceLocation.X + ClientWidth;

		public Size SourceSize => new Size(ClientWidth, ClientHeight);

		public int SourceTop => SourceLocation.Y;

		public string Url { get; private set; }

		public Rectangle ViewportRectangle { get; private set; } = Rectangle.Empty;

		/// <summary>
		///     Corrent the frame locations with the information
		/// </summary>
		/// <param name="frameElement"></param>
		private void CorrectFrameLocations(IHTMLElement frameElement)
		{
			long x = 0;
			long y = 0;
			var element = frameElement;
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

			var elementLocation = new Point((int) x, (int) y);
			var element2 = (IHTMLElement2) frameElement;
			var rec = element2.getBoundingClientRect();
			var elementBoundingLocation = new Point(rec.left, rec.top);
			// Release IHTMLRect
			ReleaseCom(rec);
			Log.Debug().WriteLine("Looking for iframe to correct at {0}", elementBoundingLocation);
			foreach (var foundFrame in Frames)
			{
				var frameLocation = foundFrame.SourceLocation;
				if (frameLocation.Equals(elementBoundingLocation))
				{
					// Match found, correcting location
					Log.Debug().WriteLine("Correcting frame from {0} to {1}", frameLocation, elementLocation);
					foundFrame.SourceLocation = elementLocation;
					foundFrame.DestinationLocation = elementLocation;
				}
				else
				{
					Log.Debug().WriteLine("{0} != {1}", frameLocation, elementBoundingLocation);
				}
			}
		}

		/// <summary>
		///     The public accessible Dispose
		///     Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/// <summary>
		///     This Dispose is called from the Dispose and the Destructor.
		///     When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			foreach (var documentContainer in Frames)
			{
				try
				{
					documentContainer.Dispose();
				}
				catch (Exception frameEx)
				{
					Log.Warn().WriteLine("Exception while disposing frame {0}", frameEx.Message);
				}
			}
			try
			{
				ReleaseCom(_document2);
			}
			catch (Exception document2Ex)
			{
				Log.Warn().WriteLine("Exception while disposing _document2 {0}", document2Ex.Message);
			}
			try
			{
				ReleaseCom(_document3);
			}
			catch (Exception document3Ex)
			{
				Log.Warn().WriteLine("Exception while disposing _document3 {0}", document3Ex.Message);
			}
		}

		/// <summary>
		///     Get the attribute from a document
		/// </summary>
		/// <param name="attribute">Attribute to get</param>
		/// <returns>object with the attribute value</returns>
		public object GetAttribute(string attribute)
		{
			var element = !_isDtd ? _document2.body : _document3.documentElement;
			object retVal = element.getAttribute(attribute, 1);
			// Release IHTMLElement com object
			ReleaseCom(element);
			return retVal;
		}

		/// <summary>
		///     Get the attribute as int from a document
		/// </summary>
		public int GetAttributeAsInt(string attribute)
		{
			return (int) GetAttribute(attribute);
		}

		/// <summary>
		///     A "workaround" for Access Denied when dealing with Frames from different domains
		/// </summary>
		/// <param name="htmlWindow">The IHTMLWindow2 to get the document from</param>
		/// <returns>IHTMLDocument2 or null</returns>
		private static IHTMLDocument2 GetDocumentFromWindow(IHTMLWindow2 htmlWindow)
		{
			if (htmlWindow == null)
			{
				Log.Warn().WriteLine("htmlWindow == null");
				return null;
			}

			// First try the usual way to get the document.
			try
			{
				var doc = htmlWindow.document;
				return doc;
			}
			catch (COMException comEx)
			{
				// I think COMException won't be ever fired but just to be sure ...
				if (comEx.ErrorCode != E_ACCESSDENIED)
				{
					Log.Warn().WriteLine(comEx, "comEx.ErrorCode != E_ACCESSDENIED but");
					return null;
				}
			}
			catch (UnauthorizedAccessException)
			{
				// This error is okay, ignoring it
			}
			catch (Exception ex1)
			{
				Log.Warn().WriteLine(ex1, "Some error: ");
				// Any other error.
				return null;
			}

			// At this point the error was E_ACCESSDENIED because the frame contains a document from another domain.
			// IE tries to prevent a cross frame scripting security issue.
			try
			{
				// Convert IHTMLWindow2 to IWebBrowser2 using IServiceProvider.
				// ReSharper disable once SuspiciousTypeConversion.Global
				var serviceProvider = (IServiceProvider) htmlWindow;

				// Use IServiceProvider.QueryService to get IWebBrowser2 object.
				object brws;
				var webBrowserAppGuid = IID_IWebBrowserApp;
				var webBrowser2Guid = IID_IWebBrowser2;
				serviceProvider.QueryService(ref webBrowserAppGuid, ref webBrowser2Guid, out brws);

				// Get the document from IWebBrowser2.
				var browser = (IWebBrowser2) brws;

				return (IHTMLDocument2) browser.Document;
			}
			catch (Exception ex2)
			{
				Log.Warn().WriteLine(ex2, "another error: ");
			}
			return null;
		}

		/// <summary>
		///     Private helper method for the constructors
		/// </summary>
		/// <param name="document2">IHTMLDocument2</param>
		/// <param name="contentWindow">WindowDetails</param>
		private void Init(IHTMLDocument2 document2, WindowDetails contentWindow)
		{
			_document2 = document2;
			ContentWindow = contentWindow;
			_document3 = document2 as IHTMLDocument3;
			// Check what access method is needed for the document
			var document5 = (IHTMLDocument5) document2;

			//compatibility mode affects how height is computed
			_isDtd = false;
			try
			{
				if ((_document3?.documentElement != null) && !document5.compatMode.Equals("BackCompat"))
				{
					_isDtd = true;
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Error checking the compatibility mode:");
			}
			// Do not release IHTMLDocument5 com object, as this also gives problems with the _document2!
			//Marshal.ReleaseComObject(document5);

			var clientRectangle = contentWindow.WindowRectangle;
			try
			{
				var window2 = document2.parentWindow;
				//IHTMLWindow3 window3 = (IHTMLWindow3)_document2.parentWindow;
				var screen = window2.screen;
				var screen2 = (IHTMLScreen2) screen;
				if (Parent != null)
				{
					// Copy _parent values
					_zoomLevelX = Parent._zoomLevelX;
					_zoomLevelY = Parent._zoomLevelY;
					ViewportRectangle = Parent.ViewportRectangle;
				}
				else
				{
					//DisableScrollbars(_document2);

					// Calculate zoom level
					_zoomLevelX = screen2.deviceXDPI/(double) screen2.logicalXDPI;
					_zoomLevelY = screen2.deviceYDPI/(double) screen2.logicalYDPI;


					// Calculate the viewport rectangle, needed if there is a frame around the html window
					Log.Debug().WriteLine("Screen {0}x{1}", ScaleX(screen.width), ScaleY(screen.height));
					//Log.Debug().WriteLine("Screen location {0},{1}", window3.screenLeft, window3.screenTop);
					Log.Debug().WriteLine("Window rectangle {0}", clientRectangle);
					Log.Debug().WriteLine("Client size {0}x{1}", ClientWidth, ClientHeight);
					int diffX = clientRectangle.Width - ClientWidth;
					int diffY = clientRectangle.Height - ClientHeight;
					// If there is a border around the inner window, the diff == 4
					// If there is a border AND a scrollbar the diff == 20
					if (((diffX == 4) || (diffX >= 20)) && ((diffY == 4) || (diffY >= 20)))
					{
						var viewportOffset = new Point(2, 2);
						var viewportSize = new Size(ClientWidth, ClientHeight);
						ViewportRectangle = new Rectangle(viewportOffset, viewportSize);
						Log.Debug().WriteLine("viewportRect {0}", ViewportRectangle);
					}
				}
				Log.Debug().WriteLine("Zoomlevel {0}, {1}", _zoomLevelX, _zoomLevelY);
				// Release com objects
				ReleaseCom(window2);
				ReleaseCom(screen);
				ReleaseCom(screen2);
			}
			catch (Exception e)
			{
				Log.Warn().WriteLine("Can't get certain properties for documents, using default. Due to: ", e);
			}

			try
			{
				Log.Debug().WriteLine("Calculated location {0} for {1}", _startLocation, document2.title);
				if (Name == null)
				{
					Name = document2.title;
				}
			}
			catch (Exception e)
			{
				Log.Warn().WriteLine("Problem while trying to get document title!", e);
			}

			try
			{
				Url = document2.url;
			}
			catch (Exception e)
			{
				Log.Warn().WriteLine("Problem while trying to get document _url!", e);
			}
			SourceLocation = new Point(ScaleX(_startLocation.X), ScaleY(_startLocation.Y));
			_destinationLocation = new Point(ScaleX(_startLocation.X), ScaleY(_startLocation.Y));

			if (Parent != null)
			{
				return;
			}
			try
			{
				var frameCollection = document2.frames;
				for (int frame = 0; frame < frameCollection.length; frame++)
				{
					try
					{
						IHTMLWindow2 frameWindow = frameCollection.item(frame);
						var frameData = new DocumentContainer(frameWindow, contentWindow, this);
						// check if frame is hidden
						if (!frameData.IsHidden)
						{
							Log.Debug().WriteLine("Creating DocumentContainer for Frame {0} found in window with rectangle {1}", frameData.Name, frameData.SourceRectangle);
							Frames.Add(frameData);
						}
						else
						{
							frameData.Dispose();
							Log.Debug().WriteLine("Skipping frame {0}", frameData.Name);
						}
						// Clean up frameWindow
						ReleaseCom(frameWindow);
					}
					catch (Exception e)
					{
						Log.Warn().WriteLine("Problem while trying to get information from a frame, skipping the frame!", e);
					}
				}
				// Clean up collection
				ReleaseCom(frameCollection);
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine(ex, "Problem while trying to get the _frames, skipping!");
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
						Log.Warn().WriteLine(e, "Problem while trying to get information from an iframe, skipping the frame!");
					}
				}
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine(ex, "Problem while trying to get the iframes, skipping!");
			}
		}

		/// <summary>
		///     Helper method to release com objects
		/// </summary>
		/// <param name="comObject"></param>
		private static void ReleaseCom(object comObject)
		{
			if (comObject != null)
			{
				Marshal.ReleaseComObject(comObject);
			}
		}

		private int ScaleX(int physicalValue)
		{
			return (int) Math.Round(physicalValue*_zoomLevelX, MidpointRounding.AwayFromZero);
		}

		private int ScaleY(int physicalValue)
		{
			return (int) Math.Round(physicalValue*_zoomLevelY, MidpointRounding.AwayFromZero);
		}

		/// <summary>
		///     Set/change an int attribute on a document
		/// </summary>
		public void SetAttribute(string attribute, int value)
		{
			SetAttribute(attribute, value.ToString());
		}

		/// <summary>
		///     Set/change an attribute on a document
		/// </summary>
		/// <param name="attribute">Attribute to set</param>
		/// <param name="value">Value to set</param>
		public void SetAttribute(string attribute, string value)
		{
			var element = !_isDtd ? _document2.body : _document3.documentElement;
			element.setAttribute(attribute, value);
			// Release IHTMLElement com object
			ReleaseCom(element);
		}

		private int UnscaleX(int physicalValue)
		{
			return (int) Math.Round(physicalValue/_zoomLevelX, MidpointRounding.AwayFromZero);
		}

		private int UnscaleY(int physicalValue)
		{
			return (int) Math.Round(physicalValue/_zoomLevelY, MidpointRounding.AwayFromZero);
		}
	}
}