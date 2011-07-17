/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Drawing;
using Greenshot.UnmanagedHelpers;
using GreenshotPlugin.Core;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of ClipboardHelper.
	/// </summary>
	public class ClipboardHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ClipboardHelper));
		private static readonly Object clipboardLockObject = new Object();
		private static string previousTmpFile = null;
		private static IntPtr nextClipboardViewer = IntPtr.Zero;
		// Template for the HTML Text on the clipboard, see: http://msdn.microsoft.com/en-us/library/ms649015%28v=vs.85%29.aspx
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

		/// <summary>
		/// Register the window to monitor the clipboard
		/// </summary>
		/// <param name="handle">Handle for the window</param>
		public static void RegisterClipboardViewer(IntPtr handle) {
			LOG.Debug("RegisterClipboardViewer called");
			nextClipboardViewer = User32.SetClipboardViewer(handle);
		}
		/// <summary>
		/// Deregister the window to monitor the clipboard
		/// </summary>
		/// <param name="handle">Handle for the window</param>
		public static void DeregisterClipboardViewer(IntPtr handle) {
			LOG.Debug("DeregisterClipboardViewer called");
			User32.ChangeClipboardChain(handle, nextClipboardViewer);
			CleanupTmpFile();
		}
		
		/// <summary>
		/// Handle WndProc messages for the clipboard
		/// </summary>
		/// <param name="m">Messag</param>
		/// <returns>true if the message is handled</returns>
		public static bool HandleClipboardMessages(ref Message m) {
			switch (m.Msg) {
				case (int)WindowsMessages.WM_DRAWCLIPBOARD:
					// Check if there is a format "greenshot" on the clipboard, than don't delete
					List<string> currentFormats = GetFormats();
					if (currentFormats != null && !currentFormats.Contains("greenshot")) {
						CleanupTmpFile();
					}
					// Make sure the next clipboard viewer gets the message
					User32.SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
					return true;
				case (int)WindowsMessages.WM_CHANGECBCHAIN:
					if (m.WParam == nextClipboardViewer) {
						nextClipboardViewer = m.LParam;
					} else {
						User32.SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
					}
					return true;
			}
			return false;
		}
		
		/**
		 * Get the current "ClipboardOwner" but only if it isn't us!
		 */
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

		/**
		 * Wrapper for the SetDataObject with a bool "copy"
		 * 
		 * Default is true, the information on the clipboard will stay even if our application quits
		 * For our own Types the other SetDataObject should be called with false.
		 */
		private static void SetDataObject(IDataObject ido) {
			SetDataObject(ido, true);
		}

		/**
		 * The SetDataObject that will lock/try/catch clipboard operations
		 * making it save and not show exceptions.
		 * 
		 * The bool "copy" is used to decided if the information stays on the clipboard after exit.
		 */
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
							ILanguage lang = Language.GetInstance();
							if (clipboardOwner != null) {
								messageText = String.Format(lang.GetString(LangKey.clipboard_inuse), clipboardOwner);
						    } else {
								messageText = lang.GetString(LangKey.clipboard_error);
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
		
		/**
		 * Set text to the clipboard
		 */
		public static void SetClipboardData(string text) {
			CleanupTmpFile();
			IDataObject ido = new DataObject();
			ido.SetData(DataFormats.Text, true, text);
			SetDataObject(ido);
		}
		
		private static string getClipboardString(Image image, string filename) {
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

		/// <summary>
		/// Cleanup previously created tmp file
		/// </summary>
		private static void CleanupTmpFile() {
			if (previousTmpFile != null) {
				try {
					LOG.DebugFormat("Deleting previous tmp file: {0}", previousTmpFile);
					File.Delete(previousTmpFile);
				} catch (Exception e) {
					LOG.Warn("Error deleting " + previousTmpFile, e);
				} finally {
					previousTmpFile = null;
				}
			}
		}
		
		/**
		 * Set an Image to the clipboard
		 * 
		 * This method will place 2 images to the clipboard, one of type Bitmap which
		 * works with pretty much everything and one of type Dib for e.g. OpenOffice
		 * because OpenOffice has a bug http://qa.openoffice.org/issues/show_bug.cgi?id=85661
		 * 
		 * The Dib (Device Indenpendend Bitmap) in 32bpp actually won't work with Powerpoint 2003!
		 * When pasting a Dib in PP 2003 the Bitmap is somehow shifted left!
		 * For this problem the user should not use the direct paste (=Dib), but select Bitmap
		 */
		private const int BITMAPFILEHEADER_LENGTH = 14;
		public static void SetClipboardData(Image image) {
			CleanupTmpFile();
			DataObject ido = new DataObject();

			// This will work for Office and most other applications
			//ido.SetData(DataFormats.Bitmap, true, image);
			
			MemoryStream bmpStream = new MemoryStream();
			MemoryStream imageStream = new MemoryStream();
			MemoryStream pngStream = new MemoryStream();
			try {
				// PNG works for Powerpoint
				image.Save(pngStream, ImageFormat.Png);

				// Save image as BMP
				image.Save(bmpStream, ImageFormat.Bmp);

				// Copy the source, but skip the "BITMAPFILEHEADER" which has a size of 14
				imageStream.Write(bmpStream.GetBuffer(), BITMAPFILEHEADER_LENGTH, (int) bmpStream.Length - BITMAPFILEHEADER_LENGTH);

				// Mark the clipboard for us as "do not touch"
				ido.SetData("greenshot", false, "was here!");
				// Set the PNG stream
				ido.SetData("PNG", false, pngStream);
				// Set the DIB to the clipboard DataObject
				ido.SetData(DataFormats.Dib, true, imageStream);
				// Set the HTML
				previousTmpFile = ImageOutput.SaveToTmpFile(image);
				string html = getClipboardString(image, previousTmpFile);
				ido.SetText(html, TextDataFormat.Html);
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

		/**
		 * Set Object with type Type to the clipboard
		 */
		public static void SetClipboardData(Type type, Object obj) {
			CleanupTmpFile();
			DataFormats.Format format = DataFormats.GetFormat(type.FullName);

			//now copy to clipboard
			IDataObject dataObj = new DataObject();
			dataObj.SetData(format.Name, false, obj);
			// Use false to make the object dissapear when the application stops.
			SetDataObject(dataObj, true);
		}
		
		/**
		 * Retrieve a list of all formats currently on the clipboard
		 */
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
		
		/**
		 * Check if there is currently something on the clipboard which has one of the supplied formats
		 */
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

		/**
		 * Get Object of type Type from the clipboard
		 */
		public static Object GetClipboardData(Type type) {
			string format = type.FullName;
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