/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Core;
using Greenshot.Drawing;
using Greenshot.UnmanagedHelpers;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of ClipboardHelper.
	/// </summary>
	public class ClipboardHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ClipboardHelper));

		private static readonly Object clipboardLockObject = new Object();

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
							Thread.Sleep(200);
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
			IDataObject ido = new DataObject();
			ido.SetData(DataFormats.Text, true, text);
			SetDataObject(ido);
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
			DataObject ido = new DataObject();

			// This will work for Office and most other applications
			ido.SetData(DataFormats.Bitmap, true, image);
			
			MemoryStream bmpStream = new MemoryStream();
			MemoryStream imageStream = new MemoryStream();
			try {
				// Save image as BMP
				image.Save(bmpStream, ImageFormat.Bmp);

				// Copy the source, but skip the "BITMAPFILEHEADER" which has a size of 14
				imageStream.Write(bmpStream.GetBuffer(), BITMAPFILEHEADER_LENGTH, (int) bmpStream.Length - BITMAPFILEHEADER_LENGTH);
				
				// Set the DIB to the clipboard DataObject
				ido.SetData(DataFormats.Dib, true, imageStream);
			} finally {
				// we need to use the SetDataOject before the streams are closed otherwise the buffer will be gone!
				// Place the DataObject to the clipboard
				SetDataObject(ido);

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