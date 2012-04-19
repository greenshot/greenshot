/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.UnmanagedHelpers;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of ClipboardHelper.
	/// </summary>
	public static class ClipboardHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ClipboardHelper));
		private static readonly Object clipboardLockObject = new Object();
		private static readonly CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();
		private static IntPtr nextClipboardViewer = IntPtr.Zero;
		// Template for the HTML Text on the clipboard
		// see: http://msdn.microsoft.com/en-us/library/ms649015%28v=vs.85%29.aspx
		// or:  http://msdn.microsoft.com/en-us/library/Aa767917.aspx
		private const string HTML_CLIPBOARD_STRING = @"Version:0.9
StartHTML:<<<<<<<1
EndHTML:<<<<<<<2
StartFragment:<<<<<<<3
EndFragment:<<<<<<<4
StartSelection:<<<<<<<3
EndSelection:<<<<<<<4
<!DOCTYPE>
<HTML>
<HEAD>
<TITLE>Greenshot capture</TITLE>
</HEAD>
<BODY>
<!--StartFragment -->
<img border='0' src='file:///${file}' width='${width}' height='${height}'>
<!--EndFragment -->
</BODY>
</HTML>";
		private const string HTML_CLIPBOARD_BASE64_STRING = @"Version:0.9
StartHTML:<<<<<<<1
EndHTML:<<<<<<<2
StartFragment:<<<<<<<3
EndFragment:<<<<<<<4
StartSelection:<<<<<<<3
EndSelection:<<<<<<<4
<!DOCTYPE>
<HTML>
<HEAD>
<TITLE>Greenshot capture</TITLE>
</HEAD>
<BODY>
<!--StartFragment -->
<img border='0' src='data:image/${format};base64,${data}' width='${width}' height='${height}'>
<!--EndFragment -->
</BODY>
</HTML>";
		
		/// <summary>
		/// Get the current "ClipboardOwner" but only if it isn't us!
		/// </summary>
		/// <returns>current clipboard owner</returns>
		private static string GetClipboardOwner() {
			string owner = null;
			try {
				IntPtr hWnd = User32.GetClipboardOwner();
				if (hWnd != IntPtr.Zero) {
					uint pid = 0;
					uint tid = User32.GetWindowThreadProcessId( hWnd, out pid );
					Process me = Process.GetCurrentProcess();
					Process ownerProcess = Process.GetProcessById( (int)pid );
					// Exclude myself
					if (ownerProcess != null && me.Id != ownerProcess.Id) {
						// Get Process Name
						owner = ownerProcess.ProcessName;
						// Try to get the starting Process Filename, this might fail.
						try {
							owner = ownerProcess.Modules[0].FileName;
						} catch (Exception) {
						}
					}
				}
			} catch (Exception e) {
				LOG.Warn("Non critical error: Couldn't get clipboard owner.", e);
			}
			return owner;
		}

		/// <summary>
		/// Wrapper for the SetDataObject with a bool "copy"
		/// Default is true, the information on the clipboard will stay even if our application quits
		/// For our own Types the other SetDataObject should be called with false.
		/// </summary>
		/// <param name="ido"></param>
		private static void SetDataObject(IDataObject ido) {
			SetDataObject(ido, true);
		}

		/// <summary>
		/// The SetDataObject that will lock/try/catch clipboard operations making it save and not show exceptions.
		/// The bool "copy" is used to decided if the information stays on the clipboard after exit.
		/// </summary>
		/// <param name="ido"></param>
		/// <param name="copy"></param>
		private static void SetDataObject(IDataObject ido, bool copy) {
			lock (clipboardLockObject) {
				int retryCount = 2;
				while (retryCount >= 0) {
					try {
						Clipboard.SetDataObject(ido, copy);
						break;
					} catch (Exception ee) {
						if (retryCount == 0) {
							string messageText = null;
							string clipboardOwner = GetClipboardOwner();
							if (clipboardOwner != null) {
								messageText = Language.GetFormattedString(LangKey.clipboard_inuse, clipboardOwner);
							} else {
								messageText = Language.GetString(LangKey.clipboard_error);
							}
							LOG.Error(messageText, ee);
						} else {
							Thread.Sleep(100);
						}
					} finally {
						--retryCount;
					}
				}
			}
		}
		
		/// <summary>
		/// Safe wrapper for Clipboard.ContainsText
		/// Created for Bug #3432313
		/// </summary>
		/// <returns>boolean if there is text on the clipboard</returns>
		public static bool ContainsText() {
			lock (clipboardLockObject) {
				int retryCount = 2;
				while (retryCount >= 0) {
					try {
						return Clipboard.ContainsText();
					} catch (Exception ee) {
						if (retryCount == 0) {
							string messageText = null;
							string clipboardOwner = GetClipboardOwner();
							if (clipboardOwner != null) {
								messageText = Language.GetFormattedString(LangKey.clipboard_inuse, clipboardOwner);
							} else {
								messageText = Language.GetString(LangKey.clipboard_error);
							}
							LOG.Error(messageText, ee);
						} else {
							Thread.Sleep(100);
						}
					} finally {
						--retryCount;
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// Safe wrapper for Clipboard.ContainsImage
		/// Created for Bug #3432313
		/// </summary>
		/// <returns>boolean if there is an image on the clipboard</returns>
		public static bool ContainsImage() {
			lock (clipboardLockObject) {
				int retryCount = 2;
				while (retryCount >= 0) {
					try {
						return Clipboard.ContainsImage();
					} catch (Exception ee) {
						if (retryCount == 0) {
							string messageText = null;
							string clipboardOwner = GetClipboardOwner();
							if (clipboardOwner != null) {
								messageText = Language.GetFormattedString(LangKey.clipboard_inuse, clipboardOwner);
							} else {
								messageText = Language.GetString(LangKey.clipboard_error);
							}
							LOG.Error(messageText, ee);
						} else {
							Thread.Sleep(100);
						}
					} finally {
						--retryCount;
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// Safe wrapper for Clipboard.GetImage
		/// Created for Bug #3432313
		/// </summary>
		/// <returns>Image if there is an image on the clipboard</returns>
		public static Image GetImage() {
			lock (clipboardLockObject) {
				int retryCount = 2;
				while (retryCount >= 0) {
					try {
						// Try to get the best format, currently we support PNG and "others"
						IList<string> formats = GetFormats();
						if (formats.Contains("PNG")) {
							Object pngObject = GetClipboardData("PNG");
							if (pngObject is MemoryStream) {
								MemoryStream png_stream = pngObject as MemoryStream;
								using (Image tmpImage = Image.FromStream(png_stream)) {
									return ImageHelper.Clone(tmpImage);
								}
							}
						}
						return Clipboard.GetImage();
					} catch (Exception ee) {
						if (retryCount == 0) {
							string messageText = null;
							string clipboardOwner = GetClipboardOwner();
							if (clipboardOwner != null) {
								messageText = Language.GetFormattedString(LangKey.clipboard_inuse, clipboardOwner);
							} else {
								messageText = Language.GetString(LangKey.clipboard_error);
							}
							LOG.Error(messageText, ee);
						} else {
							Thread.Sleep(100);
						}
					} finally {
						--retryCount;
					}
				}
			}
			return null;
		}
		
		/// <summary>
		/// Safe wrapper for Clipboard.GetText
		/// Created for Bug #3432313
		/// </summary>
		/// <returns>string if there is text on the clipboard</returns>
		public static string GetText() {
			lock (clipboardLockObject) {
				int retryCount = 2;
				while (retryCount >= 0) {
					try {
						return Clipboard.GetText();
					} catch (Exception ee) {
						if (retryCount == 0) {
							string messageText = null;
							string clipboardOwner = GetClipboardOwner();
							if (clipboardOwner != null) {
								messageText = Language.GetFormattedString(LangKey.clipboard_inuse, clipboardOwner);
							} else {
								messageText = Language.GetString(LangKey.clipboard_error);
							}
							LOG.Error(messageText, ee);
						} else {
							Thread.Sleep(100);
						}
					} finally {
						--retryCount;
					}
				}
			}
			return null;
		}
		
		/// <summary>
		/// Set text to the clipboard
		/// </summary>
		/// <param name="text"></param>
		public static void SetClipboardData(string text) {
			IDataObject ido = new DataObject();
			ido.SetData(DataFormats.Text, true, text);
			SetDataObject(ido);
		}
		
		private static string getHTMLString(Image image, string filename) {
			string utf8EncodedHTMLString = Encoding.GetEncoding(0).GetString(Encoding.UTF8.GetBytes(HTML_CLIPBOARD_STRING));
			utf8EncodedHTMLString= utf8EncodedHTMLString.Replace("${width}", image.Width.ToString());
			utf8EncodedHTMLString= utf8EncodedHTMLString.Replace("${height}", image.Height.ToString());
			utf8EncodedHTMLString= utf8EncodedHTMLString.Replace("${file}", filename);
			StringBuilder sb=new StringBuilder();
			sb.Append(utf8EncodedHTMLString);
			sb.Replace("<<<<<<<1", (utf8EncodedHTMLString.IndexOf("<HTML>") + "<HTML>".Length).ToString("D8"));
			sb.Replace("<<<<<<<2", (utf8EncodedHTMLString.IndexOf("</HTML>")).ToString("D8"));
			sb.Replace("<<<<<<<3", (utf8EncodedHTMLString.IndexOf("<!--StartFragment -->")+"<!--StartFragment -->".Length).ToString("D8"));
			sb.Replace("<<<<<<<4", (utf8EncodedHTMLString.IndexOf("<!--EndFragment -->")).ToString("D8"));
			return sb.ToString(); 
		}

		private static string getHTMLDataURLString(Image image, MemoryStream pngStream) {
			string utf8EncodedHTMLString = Encoding.GetEncoding(0).GetString(Encoding.UTF8.GetBytes(HTML_CLIPBOARD_BASE64_STRING));
			utf8EncodedHTMLString= utf8EncodedHTMLString.Replace("${width}", image.Width.ToString());
			utf8EncodedHTMLString= utf8EncodedHTMLString.Replace("${height}", image.Height.ToString());
			utf8EncodedHTMLString = utf8EncodedHTMLString.Replace("${format}", "png");
			utf8EncodedHTMLString = utf8EncodedHTMLString.Replace("${data}", Convert.ToBase64String(pngStream.GetBuffer(),0, (int)pngStream.Length));
			StringBuilder sb=new StringBuilder();
			sb.Append(utf8EncodedHTMLString);
			sb.Replace("<<<<<<<1", (utf8EncodedHTMLString.IndexOf("<HTML>") + "<HTML>".Length).ToString("D8"));
			sb.Replace("<<<<<<<2", (utf8EncodedHTMLString.IndexOf("</HTML>")).ToString("D8"));
			sb.Replace("<<<<<<<3", (utf8EncodedHTMLString.IndexOf("<!--StartFragment -->")+"<!--StartFragment -->".Length).ToString("D8"));
			sb.Replace("<<<<<<<4", (utf8EncodedHTMLString.IndexOf("<!--EndFragment -->")).ToString("D8"));
			return sb.ToString(); 
		}

		/// <summary>
		/// Set an Image to the clipboard
		/// This method will place 2 images to the clipboard, one of type Bitmap which
		/// works with pretty much everything and one of type Dib for e.g. OpenOffice
		/// because OpenOffice has a bug http://qa.openoffice.org/issues/show_bug.cgi?id=85661
		/// The Dib (Device Indenpendend Bitmap) in 32bpp actually won't work with Powerpoint 2003!
		/// When pasting a Dib in PP 2003 the Bitmap is somehow shifted left!
		/// For this problem the user should not use the direct paste (=Dib), but select Bitmap
		/// </summary>
		private const int BITMAPFILEHEADER_LENGTH = 14;
		public static void SetClipboardData(Image image) {
			DataObject ido = new DataObject();

			// This will work for Office and most other applications
			//ido.SetData(DataFormats.Bitmap, true, image);
			
			MemoryStream bmpStream = null;
			MemoryStream imageStream = null;
			MemoryStream pngStream = null;
			try {
				// Create PNG stream
				if (config.ClipboardFormats.Contains(ClipboardFormat.PNG) || config.ClipboardFormats.Contains(ClipboardFormat.HTMLDATAURL)) {
					pngStream = new MemoryStream();
					// PNG works for Powerpoint
					image.Save(pngStream, ImageFormat.Png);
					pngStream.Seek(0, SeekOrigin.Begin);
				}

				if (config.ClipboardFormats.Contains(ClipboardFormat.PNG)) {
					// Set the PNG stream
					ido.SetData("PNG", false, pngStream);
				}

				if (config.ClipboardFormats.Contains(ClipboardFormat.DIB)) {
					bmpStream = new MemoryStream();
					// Save image as BMP
					image.Save(bmpStream, ImageFormat.Bmp);
					imageStream = new MemoryStream();
					// Copy the source, but skip the "BITMAPFILEHEADER" which has a size of 14
					imageStream.Write(bmpStream.GetBuffer(), BITMAPFILEHEADER_LENGTH, (int) bmpStream.Length - BITMAPFILEHEADER_LENGTH);

					// Set the DIB to the clipboard DataObject
					ido.SetData(DataFormats.Dib, true, imageStream);
				}
				
				// Set the HTML
				if (config.ClipboardFormats.Contains(ClipboardFormat.HTML)) {
					string tmpFile = ImageOutput.SaveToTmpFile(image, OutputFormat.png, config.OutputFileJpegQuality, config.OutputFileReduceColors);
					string html = getHTMLString(image, tmpFile);
					ido.SetText(html, TextDataFormat.Html);
				} else if (config.ClipboardFormats.Contains(ClipboardFormat.HTMLDATAURL)) {
					string html = getHTMLDataURLString(image, pngStream);
					ido.SetText(html, TextDataFormat.Html);
				}
			} finally {
				// we need to use the SetDataOject before the streams are closed otherwise the buffer will be gone!
				// Place the DataObject to the clipboard
				SetDataObject(ido);
				
				if (pngStream != null) {
					pngStream.Dispose();
					pngStream = null;
				}

				if (bmpStream != null) {
					bmpStream.Dispose();
					bmpStream = null;
				}

				if (imageStream != null) {
					imageStream.Dispose();
					imageStream = null;
				}
			}
		}

		/// <summary>
		/// Set Object with type Type to the clipboard
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="obj">object</param>
		public static void SetClipboardData(Type type, Object obj) {
			DataFormats.Format format = DataFormats.GetFormat(type.FullName);

			//now copy to clipboard
			IDataObject dataObj = new DataObject();
			dataObj.SetData(format.Name, false, obj);
			// Use false to make the object dissapear when the application stops.
			SetDataObject(dataObj, true);
		}
		
		/// <summary>
		/// Retrieve a list of all formats currently on the clipboard
		/// </summary>
		/// <returns>List<string> with the current formats</returns>
		public static List<string> GetFormats() {
			string[] formats = null;

			lock (clipboardLockObject) {
				try {
					IDataObject dataObj = Clipboard.GetDataObject();
					if (dataObj != null) {
						formats = dataObj.GetFormats();
					}
				} catch (Exception e) {
					LOG.Error("Error in GetFormats.", e);
				}
			}
			if (formats != null) {
				return new List<string>(formats);
			}
			return new List<string>();
		}
		
		/// <summary>
		/// Check if there is currently something on the clipboard which has one of the supplied formats
		/// </summary>
		/// <param name="formats">string[] with formats</param>
		/// <returns>true if one of the formats was found</returns>
		public static bool ContainsFormat(string[] formats) {
			bool formatFound = false;
			List<string> currentFormats = GetFormats();
			if (currentFormats == null || currentFormats.Count == 0 ||formats == null || formats.Length == 0) {
				return false;
			}
			foreach (string format in formats) {
				if (currentFormats.Contains(format)) {
					formatFound = true;
					break;
				}
			}
			return formatFound;
		}

		/// <summary>
		/// Get Object of type Type from the clipboard
		/// </summary>
		/// <param name="type">Type to get</param>
		/// <returns>object from clipboard</returns>
		public static Object GetClipboardData(Type type) {
			string format = type.FullName;
			return GetClipboardData(format);
		}

		/// <summary>
		/// Get Object for format from the clipboard
		/// </summary>
		/// <param name="format">format to get</param>
		/// <returns>object from clipboard</returns>
		public static Object GetClipboardData(string format) {
			Object obj = null;

			lock (clipboardLockObject) {
				try {
					IDataObject dataObj = Clipboard.GetDataObject();
					if(dataObj != null && dataObj.GetDataPresent(format)) {
						obj = dataObj.GetData(format);
					}
				} catch (Exception e) {
					LOG.Error("Error in GetClipboardData.", e);
				}
			}
			return obj;
		}
	}
}