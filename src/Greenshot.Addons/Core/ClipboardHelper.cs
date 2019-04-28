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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.User32;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Core.Enums;
using Greenshot.Gfx;

namespace Greenshot.Addons.Core
{
	/// <summary>
	///     Description of ClipboardHelper.
	/// </summary>
	public static class ClipboardHelper
	{
		//private static readonly string FORMAT_HTML = "HTML Format";

		// Template for the HTML Text on the clipboard
		// see: http://msdn.microsoft.com/en-us/library/ms649015%28v=vs.85%29.aspx
		// or:  http://msdn.microsoft.com/en-us/library/Aa767917.aspx
		private const string HtmlClipboardString = @"Version:0.9
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
		private const string HtmlClipboardBase64String = @"Version:0.9
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

		private const int BITMAPFILEHEADER_LENGTH = 14;
		private static readonly LogSource Log = new LogSource();
		private static readonly object ClipboardLockObject = new object();

	    /// <summary>
        /// Set from DI via AddonsModule
        /// </summary>
	    internal static ICoreConfiguration CoreConfiguration { get; set; }
        private static readonly string FORMAT_FILECONTENTS = "FileContents";
		private static readonly string FORMAT_PNG = "PNG";
		private static readonly string FORMAT_PNG_OFFICEART = "PNG+Office Art";
		private static readonly string FORMAT_17 = "Format17";
		private static readonly string FORMAT_JPG = "JPG";
		private static readonly string FORMAT_JFIF = "JFIF";
		private static readonly string FORMAT_JFIF_OFFICEART = "JFIF+Office Art";
		private static readonly string FORMAT_GIF = "GIF";
		private static readonly string FORMAT_BITMAP = "System.Drawing.Bitmap";

		/// <summary>
		///     Get the current "ClipboardOwner" but only if it isn't us!
		/// </summary>
		/// <returns>current clipboard owner</returns>
		private static string GetClipboardOwner()
		{
			string owner = null;
			try
			{
				var hWnd = ClipboardNative.CurrentOwner;
				if (hWnd != IntPtr.Zero)
				{
					try
					{
					    User32Api.GetWindowThreadProcessId(hWnd, out var pid);
						using (var me = Process.GetCurrentProcess())
						using (var ownerProcess = Process.GetProcessById(pid))
						{
							// Exclude myself
							if (me.Id != ownerProcess.Id)
							{
								// Get Process Name
								owner = ownerProcess.ProcessName;
								// Try to get the starting Process Filename, this might fail.
								try
								{
									owner = ownerProcess.Modules[0].FileName;
								}
								catch (Exception)
								{
									// Ignore
								}
							}
						}
					}
					catch (Exception e)
					{
						Log.Warn().WriteLine(e, "Non critical error: Couldn't get clipboard process, trying to use the title.");
						owner = User32Api.GetText(hWnd);
					}
				}
			}
			catch (Exception e)
			{
				Log.Warn().WriteLine(e, "Non critical error: Couldn't get clipboard owner.");
			}
			return owner;
		}

		/// <summary>
		///     The SetDataObject will lock/try/catch clipboard operations making it save and not show exceptions.
		///     The bool "copy" is used to decided if the information stays on the clipboard after exit.
		/// </summary>
		/// <param name="ido"></param>
		/// <param name="copy"></param>
		private static void SetDataObject(IDataObject ido, bool copy)
		{
			lock (ClipboardLockObject)
			{
				// Clear first, this seems to solve some issues
				try
				{
					Clipboard.Clear();
				}
				catch (Exception clearException)
				{
					Log.Warn().WriteLine(clearException.Message);
				}
				try
				{
					// For BUG-1935 this was changed from looping ourselfs, or letting MS retry...
					Clipboard.SetDataObject(ido, copy, 15, 200);
				}
				catch (Exception clipboardSetException)
				{
					string messageText;
					var clipboardOwner = GetClipboardOwner();
					if (clipboardOwner != null)
					{
                        messageText = "in use"; // Language.GetFormattedString("clipboard_inuse", clipboardOwner);
					}
					else
					{
                        messageText = "error"; // Language.GetString("Core","clipboard_error");
					}
					Log.Error().WriteLine(clipboardSetException, messageText);
				}
			}
		}

		/// <summary>
		///     The GetDataObject will lock/try/catch clipboard operations making it save and not show exceptions.
		/// </summary>
		public static IDataObject GetDataObject()
		{
			lock (ClipboardLockObject)
			{
				var retryCount = 2;
				while (retryCount >= 0)
				{
					try
					{
						return Clipboard.GetDataObject();
					}
					catch (Exception ee)
					{
						if (retryCount == 0)
						{
							string messageText;
							var clipboardOwner = GetClipboardOwner();
                            // TODO: Translations
							if (clipboardOwner != null)
							{
                                messageText = "In use"; // Language.GetFormattedString("clipboard_inuse", clipboardOwner);
							}
							else
							{
                                messageText = "Error"; // Language.GetString("Core", "clipboard_error");
							}
							Log.Error().WriteLine(ee, messageText);
						}
						else
						{
							Thread.Sleep(100);
						}
					}
					finally
					{
						--retryCount;
					}
				}
			}
			return null;
		}

		/// <summary>
		///     Wrapper for Clipboard.ContainsText, Created for Bug #3432313
		/// </summary>
		/// <returns>boolean if there is text on the clipboard</returns>
		public static bool ContainsText()
		{
			var clipboardData = GetDataObject();
			return ContainsText(clipboardData);
		}

		/// <summary>
		///     Test if the IDataObject contains Text
		/// </summary>
		/// <param name="dataObject"></param>
		/// <returns></returns>
		public static bool ContainsText(IDataObject dataObject)
		{
		    if (dataObject == null)
		    {
		        return false;
		    }

		    if (dataObject.GetDataPresent(DataFormats.Text) || dataObject.GetDataPresent(DataFormats.UnicodeText))
		    {
		        return true;
		    }
		    return false;
		}

		/// <summary>
		///     Wrapper for Clipboard.ContainsImage, specialized for Greenshot, Created for Bug #3432313
		/// </summary>
		/// <returns>boolean if there is an image on the clipboard</returns>
		public static bool ContainsImage()
		{
			var clipboardData = GetDataObject();
			return ContainsImage(clipboardData);
		}

		/// <summary>
		///     Check if the IDataObject has an image
		/// </summary>
		/// <param name="dataObject"></param>
		/// <returns>true if an image is there</returns>
		public static bool ContainsImage(IDataObject dataObject)
		{
		    if (dataObject == null)
		    {
		        return false;
		    }

		    if (dataObject.GetDataPresent(DataFormats.Bitmap)
		        || dataObject.GetDataPresent(DataFormats.Dib)
		        || dataObject.GetDataPresent(DataFormats.Tiff)
		        || dataObject.GetDataPresent(DataFormats.EnhancedMetafile)
		        || dataObject.GetDataPresent(FORMAT_PNG)
		        || dataObject.GetDataPresent(FORMAT_17)
		        || dataObject.GetDataPresent(FORMAT_JPG)
		        || dataObject.GetDataPresent(FORMAT_GIF))
		    {
		        return true;
		    }
		    var imageFiles = GetImageFilenames(dataObject);
		    if (imageFiles.Any())
		    {
		        return true;
		    }

		    if (!dataObject.GetDataPresent(FORMAT_FILECONTENTS))
		    {
		        return false;
		    }

		    try
		    {
		        var imageStream = dataObject.GetData(FORMAT_FILECONTENTS) as MemoryStream;
		        if (IsValidStream(imageStream))
		        {
		            using (BitmapHelper.FromStream(imageStream))
		            {
		                // If we get here, there is an image
		                return true;
		            }
		        }
		    }
		    catch (Exception)
		    {
		        // Ignore
		    }
		    return false;
		}

		/// <summary>
		///     Simple helper to check the stream
		/// </summary>
		/// <param name="memoryStream"></param>
		/// <returns></returns>
		private static bool IsValidStream(MemoryStream memoryStream)
		{
			return memoryStream != null && memoryStream.Length > 0;
		}

        /// <summary>
        ///     Wrapper for Clipboard.GetBitmap, Created for Bug #3432313
        /// </summary>
        /// <returns>Bitmap if there is an bitmap on the clipboard</returns>
        public static IBitmapWithNativeSupport GetBitmap()
		{
			var clipboardData = GetDataObject();
			// Return the first image
		    return GetBitmaps(clipboardData).FirstOrDefault();
		}

        /// <summary>
        ///     Get all Bitmaps (multiple if filenames are available) from the dataObject
        ///     Returned bitmap must be disposed by the calling code!
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>IEnumerable of Bitmap</returns>
        public static IEnumerable<IBitmapWithNativeSupport> GetBitmaps(IDataObject dataObject)
		{
			// Get single image, this takes the "best" match
			var singleImage = GetBitmap(dataObject);
			if (singleImage != null)
			{
				Log.Info().WriteLine("Got image from clipboard with size {0} and format {1}", singleImage.Size, singleImage.PixelFormat);
				yield return singleImage;
			}
			else
			{
				// check if files are supplied
				foreach (var imageFile in GetImageFilenames(dataObject))
				{
                    IBitmapWithNativeSupport returnBitmap = null;
					try
					{
						returnBitmap = BitmapHelper.LoadBitmap(imageFile);
					}
					catch (Exception streamImageEx)
					{
						Log.Error().WriteLine(streamImageEx, "Problem retrieving Bitmap from clipboard.");
					}
				    if (returnBitmap == null)
				    {
				        continue;
				    }
				    Log.Info().WriteLine("Got bitmap from clipboard with size {0} and format {1}", returnBitmap.Size, returnBitmap.PixelFormat);
				    yield return returnBitmap;
				}
			}
		}

        /// <summary>
        ///     Get a Bitmap from the IDataObject, don't check for FileDrop
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>Bitmap or null</returns>
        private static IBitmapWithNativeSupport GetBitmap(IDataObject dataObject)
		{
            IBitmapWithNativeSupport returnBitmap = null;
		    if (dataObject == null)
		    {
		        return null;
		    }
		    IList<string> formats = GetFormats(dataObject);
		    string[] retrieveFormats;

		    // Found a weird bug, where PNG's from Outlook 2010 are clipped
		    // So I build some special logik to get the best format:
		    if (formats != null && formats.Contains(FORMAT_PNG_OFFICEART) && formats.Contains(DataFormats.Dib))
		    {
		        // Outlook ??
		        Log.Info().WriteLine("Most likely the current clipboard contents come from Outlook, as this has a problem with PNG and others we place the DIB format to the front...");
		        retrieveFormats = new[]
		        {
		            DataFormats.Dib, FORMAT_BITMAP, FORMAT_FILECONTENTS, FORMAT_PNG_OFFICEART, FORMAT_PNG, FORMAT_JFIF_OFFICEART, FORMAT_JPG, FORMAT_JFIF, DataFormats.Tiff, FORMAT_GIF
		        };
		    }
		    else
		    {
		        retrieveFormats = new[]
		        {
		            FORMAT_PNG_OFFICEART, FORMAT_PNG, FORMAT_17, FORMAT_JFIF_OFFICEART, FORMAT_JPG, FORMAT_JFIF, DataFormats.Tiff, DataFormats.Dib, FORMAT_BITMAP, FORMAT_FILECONTENTS,
		            FORMAT_GIF
		        };
		    }
		    foreach (var currentFormat in retrieveFormats)
		    {
		        if (formats != null && formats.Contains(currentFormat))
		        {
		            Log.Info().WriteLine("Found {0}, trying to retrieve.", currentFormat);
		            returnBitmap = GetBitmapForFormat(currentFormat, dataObject);
		        }
		        else
		        {
		            Log.Debug().WriteLine("Couldn't find format {0}.", currentFormat);
		        }
		        if (returnBitmap != null)
		        {
		            return returnBitmap;
		        }
		    }
		    return null;
		}

		/// <summary>
		///     Helper method to try to get an image in the specified format from the dataObject
		///     the DIB reader should solve some issues
		///     It also supports Format17/DibV5, by using the following information: http://stackoverflow.com/a/14335591
		/// </summary>
		/// <param name="format">string with the format</param>
		/// <param name="dataObject">IDataObject</param>
		/// <returns>Bitmap or null</returns>
		private static IBitmapWithNativeSupport GetBitmapForFormat(string format, IDataObject dataObject)
		{
			var clipboardObject = GetFromDataObject(dataObject, format);
			var imageStream = clipboardObject as MemoryStream;
			if (!IsValidStream(imageStream))
			{
				// TODO: add "HTML Format" support here...
				return BitmapWrapper.FromBitmap(clipboardObject as Bitmap);
			}
			if (CoreConfiguration.EnableSpecialDIBClipboardReader)
			{
				if (format == FORMAT_17 || format == DataFormats.Dib)
				{
					Log.Info().WriteLine("Found DIB stream, trying to process it.");
					try
					{
						if (imageStream != null)
						{
							var dibBuffer = new byte[imageStream.Length];
							imageStream.Read(dibBuffer, 0, dibBuffer.Length);
							var infoHeader = BinaryStructHelper.FromByteArray<BitmapInfoHeader>(dibBuffer);
							if (!infoHeader.IsDibV5)
							{
								Log.Info().WriteLine("Using special DIB <v5 format reader with biCompression {0}", infoHeader.Compression);
								var fileHeaderSize = Marshal.SizeOf(typeof(BitmapFileHeader));
								var fileHeader = BitmapFileHeader.Create(infoHeader);
								var fileHeaderBytes = BinaryStructHelper.ToByteArray(fileHeader);

								using (var bitmapStream = new MemoryStream())
								{
									bitmapStream.Write(fileHeaderBytes, 0, fileHeaderSize);
									bitmapStream.Write(dibBuffer, 0, dibBuffer.Length);
									bitmapStream.Seek(0, SeekOrigin.Begin);
									var image = BitmapHelper.FromStream(bitmapStream);
									if (image != null)
									{
										return image;
									}
								}
							}
							else
							{
								Log.Info().WriteLine("Using special DIBV5 / Format17 format reader");
								// CF_DIBV5
								var gcHandle = IntPtr.Zero;
								try
								{
									var handle = GCHandle.Alloc(dibBuffer, GCHandleType.Pinned);
									gcHandle = GCHandle.ToIntPtr(handle);
                                    // TODO: Should be easier
									return
                                        BitmapWrapper.FromBitmap(
										new Bitmap(infoHeader.Width, infoHeader.Height,
											-(int) (infoHeader.SizeImage / infoHeader.Height),
											infoHeader.BitCount == 32 ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb,
											new IntPtr(handle.AddrOfPinnedObject().ToInt32() + infoHeader.OffsetToPixels + (infoHeader.Height - 1) * (int) (infoHeader.SizeImage / infoHeader.Height))
										));
								}
								catch (Exception ex)
								{
									Log.Error().WriteLine(ex, "Problem retrieving Format17 from clipboard.");
								}
								finally
								{
									if (gcHandle == IntPtr.Zero)
									{
										GCHandle.FromIntPtr(gcHandle).Free();
									}
								}
							}
						}
					}
					catch (Exception dibEx)
					{
						Log.Error().WriteLine(dibEx, "Problem retrieving DIB from clipboard.");
					}
				}
			}
			else
			{
				Log.Info().WriteLine("Skipping special DIB format reader as it's disabled in the configuration.");
			}
			try
			{
				if (imageStream != null)
				{
					imageStream.Seek(0, SeekOrigin.Begin);
					var tmpImage = BitmapHelper.FromStream(imageStream);
					if (tmpImage != null)
					{
						Log.Info().WriteLine("Got image with clipboard format {0} from the clipboard.", format);
						return tmpImage;
					}
				}
			}
			catch (Exception streamImageEx)
			{
				Log.Error().WriteLine(streamImageEx, $"Problem retrieving {format} from clipboard.");
			}
			return null;
		}

		/// <summary>
		///     Wrapper for Clipboard.GetText created for Bug #3432313
		/// </summary>
		/// <returns>string if there is text on the clipboard</returns>
		public static string GetText()
		{
			return GetText(GetDataObject());
		}

		/// <summary>
		///     Get Text from the DataObject
		/// </summary>
		/// <returns>string if there is text on the clipboard</returns>
		public static string GetText(IDataObject dataObject)
		{
			if (ContainsText(dataObject))
			{
				return (string) dataObject.GetData(DataFormats.Text);
			}
			return null;
		}

		/// <summary>
		///     Set text to the clipboard
		/// </summary>
		/// <param name="text"></param>
		public static void SetClipboardData(string text)
		{
			IDataObject ido = new DataObject();
			ido.SetData(DataFormats.Text, true, text);
			SetDataObject(ido, true);
		}

		private static string GetHtmlString(ISurface surface, string filename)
		{
			var utf8EncodedHtmlString = Encoding.GetEncoding(0).GetString(Encoding.UTF8.GetBytes(HtmlClipboardString));
			utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${width}", surface.Screenshot.Width.ToString());
			utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${height}", surface.Screenshot.Height.ToString());
			utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${file}", filename.Replace("\\", "/"));
			var sb = new StringBuilder();
			sb.Append(utf8EncodedHtmlString);
			sb.Replace("<<<<<<<1", (utf8EncodedHtmlString.IndexOf("<HTML>", StringComparison.Ordinal) + "<HTML>".Length).ToString("D8"));
			sb.Replace("<<<<<<<2", utf8EncodedHtmlString.IndexOf("</HTML>", StringComparison.Ordinal).ToString("D8"));
			sb.Replace("<<<<<<<3", (utf8EncodedHtmlString.IndexOf("<!--StartFragment -->", StringComparison.Ordinal) + "<!--StartFragment -->".Length).ToString("D8"));
			sb.Replace("<<<<<<<4", utf8EncodedHtmlString.IndexOf("<!--EndFragment -->", StringComparison.Ordinal).ToString("D8"));
			return sb.ToString();
		}

		private static string GetHtmlDataUrlString(ISurface surface, MemoryStream pngStream)
		{
			var utf8EncodedHtmlString = Encoding.GetEncoding(0).GetString(Encoding.UTF8.GetBytes(HtmlClipboardBase64String));
			utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${width}", surface.Screenshot.Width.ToString());
			utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${height}", surface.Screenshot.Height.ToString());
			utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${format}", "png");
			utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${data}", Convert.ToBase64String(pngStream.GetBuffer(), 0, (int) pngStream.Length));
			var sb = new StringBuilder();
			sb.Append(utf8EncodedHtmlString);
			sb.Replace("<<<<<<<1", (utf8EncodedHtmlString.IndexOf("<HTML>", StringComparison.Ordinal) + "<HTML>".Length).ToString("D8"));
			sb.Replace("<<<<<<<2", utf8EncodedHtmlString.IndexOf("</HTML>", StringComparison.Ordinal).ToString("D8"));
			sb.Replace("<<<<<<<3", (utf8EncodedHtmlString.IndexOf("<!--StartFragment -->", StringComparison.Ordinal) + "<!--StartFragment -->".Length).ToString("D8"));
			sb.Replace("<<<<<<<4", utf8EncodedHtmlString.IndexOf("<!--EndFragment -->", StringComparison.Ordinal).ToString("D8"));
			return sb.ToString();
		}

		/// <summary>
		///     Set an Image to the clipboard
		///     This method will place images to the clipboard depending on the ClipboardFormats setting.
		///     e.g. Bitmap which works with pretty much everything and type Dib for e.g. OpenOffice
		///     because OpenOffice has a bug http://qa.openoffice.org/issues/show_bug.cgi?id=85661
		///     The Dib (Device Indenpendend Bitmap) in 32bpp actually won't work with Powerpoint 2003!
		///     When pasting a Dib in PP 2003 the Bitmap is somehow shifted left!
		///     For this problem the user should not use the direct paste (=Dib), but select Bitmap
		/// </summary>
		public static void SetClipboardData(ISurface surface)
		{
			var dataObject = new DataObject();

			// This will work for Office and most other applications
			//ido.SetData(DataFormats.Bitmap, true, image);

			MemoryStream dibStream = null;
			MemoryStream dibV5Stream = null;
			MemoryStream pngStream = null;
			IBitmapWithNativeSupport bitmapToSave = null;
			var disposeImage = false;
			try
			{
				var outputSettings = new SurfaceOutputSettings(CoreConfiguration, OutputFormats.png, 100, false);
				// Create the image which is going to be saved so we don't create it multiple times
				disposeImage = ImageOutput.CreateBitmapFromSurface(surface, outputSettings, out bitmapToSave);
				try
				{
					// Create PNG stream
					if (CoreConfiguration.ClipboardFormats.Contains(ClipboardFormats.PNG))
					{
						pngStream = new MemoryStream();
						// PNG works for e.g. Powerpoint
						var pngOutputSettings = new SurfaceOutputSettings(CoreConfiguration, OutputFormats.png, 100, false);
						ImageOutput.SaveToStream(bitmapToSave, null, pngStream, pngOutputSettings);
						pngStream.Seek(0, SeekOrigin.Begin);
						// Set the PNG stream
						dataObject.SetData(FORMAT_PNG, false, pngStream);
					}
				}
				catch (Exception pngEx)
				{
					Log.Error().WriteLine(pngEx, "Error creating PNG for the Clipboard.");
				}

				try
				{
					if (CoreConfiguration.ClipboardFormats.Contains(ClipboardFormats.DIB))
					{
						using (var tmpBmpStream = new MemoryStream())
						{
							// Save image as BMP
							var bmpOutputSettings = new SurfaceOutputSettings(CoreConfiguration, OutputFormats.bmp, 100, false);
							ImageOutput.SaveToStream(bitmapToSave, null, tmpBmpStream, bmpOutputSettings);

							dibStream = new MemoryStream();
							// Copy the source, but skip the "BITMAPFILEHEADER" which has a size of 14
							dibStream.Write(tmpBmpStream.GetBuffer(), BITMAPFILEHEADER_LENGTH, (int) tmpBmpStream.Length - BITMAPFILEHEADER_LENGTH);
						}

						// Set the DIB to the clipboard DataObject
						dataObject.SetData(DataFormats.Dib, true, dibStream);
					}
				}
				catch (Exception dibEx)
				{
					Log.Error().WriteLine(dibEx, "Error creating DIB for the Clipboard.");
				}

				// CF_DibV5
				try
				{
					if (CoreConfiguration.ClipboardFormats.Contains(ClipboardFormats.DIBV5))
					{
						// Create the stream for the clipboard
						dibV5Stream = new MemoryStream();

						// Create the BITMAPINFOHEADER
						var header = BitmapInfoHeader.Create(bitmapToSave.Width, bitmapToSave.Height, 32);
						// Make sure we have BI_BITFIELDS, this seems to be normal for Format17?
						header.Compression = BitmapCompressionMethods.BI_BITFIELDS;

						var headerBytes = BinaryStructHelper.ToByteArray(header);
						// Write the BITMAPINFOHEADER to the stream
						dibV5Stream.Write(headerBytes, 0, headerBytes.Length);

						// As we have specified BI_COMPRESSION.BI_BITFIELDS, the BitfieldColorMask needs to be added
						var colorMask = BitfieldColorMask.Create();
						// Create the byte[] from the struct
						var colorMaskBytes = BinaryStructHelper.ToByteArray(colorMask);
						Array.Reverse(colorMaskBytes);
						// Write to the stream
						dibV5Stream.Write(colorMaskBytes, 0, colorMaskBytes.Length);

						// Create the raw bytes for the pixels only
						var bitmapBytes = BitmapToByteArray(bitmapToSave);
						// Write to the stream
						dibV5Stream.Write(bitmapBytes, 0, bitmapBytes.Length);

						// Set the DIBv5 to the clipboard DataObject
						dataObject.SetData(FORMAT_17, true, dibV5Stream);
					}
				}
				catch (Exception dibEx)
				{
					Log.Error().WriteLine(dibEx, "Error creating DIB for the Clipboard.");
				}

				// Set the HTML
				if (CoreConfiguration.ClipboardFormats.Contains(ClipboardFormats.HTML))
				{
					var tmpFile = ImageOutput.SaveToTmpFile(surface, new SurfaceOutputSettings(CoreConfiguration, OutputFormats.png, 100, false), null);
					var html = GetHtmlString(surface, tmpFile);
					dataObject.SetText(html, TextDataFormat.Html);
				}
				else if (CoreConfiguration.ClipboardFormats.Contains(ClipboardFormats.HTMLDATAURL))
				{
					string html;
					using (var tmpPngStream = new MemoryStream())
					{
						var pngOutputSettings = new SurfaceOutputSettings(CoreConfiguration, OutputFormats.png, 100, false)
						{
							// Do not allow to reduce the colors, some applications dislike 256 color images
							// reported with bug #3594681
							DisableReduceColors = true
						};
						// Check if we can use the previously used image
						if (bitmapToSave.PixelFormat != PixelFormat.Format8bppIndexed)
						{
							ImageOutput.SaveToStream(bitmapToSave, surface, tmpPngStream, pngOutputSettings);
						}
						else
						{
							ImageOutput.SaveToStream(surface, tmpPngStream, pngOutputSettings);
						}
						html = GetHtmlDataUrlString(surface, tmpPngStream);
					}
					dataObject.SetText(html, TextDataFormat.Html);
				}
			}
			finally
			{
				// we need to use the SetDataOject before the streams are closed otherwise the buffer will be gone!
				// Check if Bitmap is wanted
				if (CoreConfiguration.ClipboardFormats.Contains(ClipboardFormats.BITMAP))
				{
					dataObject.SetImage(bitmapToSave.NativeBitmap);
					// Place the DataObject to the clipboard
					SetDataObject(dataObject, true);
				}
				else
				{
					// Place the DataObject to the clipboard
					SetDataObject(dataObject, true);
				}

				pngStream?.Dispose();
				dibStream?.Dispose();
				dibV5Stream?.Dispose();
				// cleanup if needed
				if (disposeImage)
				{
					bitmapToSave?.Dispose();
				}
			}
		}

		/// <summary>
		///     Helper method so get the bitmap bytes
		///     See: http://stackoverflow.com/a/6570155
		/// </summary>
		/// <param name="bitmap">Bitmap</param>
		/// <returns>byte[]</returns>
		private static byte[] BitmapToByteArray(IBitmapWithNativeSupport bitmap)
		{
			// Lock the bitmap's bits.  
			var rect = new NativeRect(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.NativeBitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

			var absStride = Math.Abs(bmpData.Stride);
			var bytes = absStride * bitmap.Height;
			long ptr = bmpData.Scan0.ToInt32();
			// Declare an array to hold the bytes of the bitmap.
			var rgbValues = new byte[bytes];

			for (var i = 0; i < bitmap.Height; i++)
			{
				var pointer = new IntPtr(ptr + bmpData.Stride * i);
				Marshal.Copy(pointer, rgbValues, absStride * (bitmap.Height - i - 1), absStride);
			}

			// Unlock the bits.
			bitmap.NativeBitmap.UnlockBits(bmpData);

			return rgbValues;
		}

		/// <summary>
		///     Set Object with type Type to the clipboard
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="obj">object</param>
		public static void SetClipboardData(Type type, object obj)
		{
			var format = DataFormats.GetFormat(type.FullName);

			//now copy to clipboard
			IDataObject dataObj = new DataObject();
			dataObj.SetData(format.Name, false, obj);
			// Use false to make the object dissapear when the application stops.
			SetDataObject(dataObj, true);
		}

		/// <summary>
		///     Retrieve a list of all formats currently on the clipboard
		/// </summary>
		/// <returns>List of strings with the current formats</returns>
		public static List<string> GetFormats()
		{
			return GetFormats(GetDataObject());
		}

		/// <summary>
		///     Retrieve a list of all formats currently in the IDataObject
		/// </summary>
		/// <returns>List of string with the current formats</returns>
		public static List<string> GetFormats(IDataObject dataObj)
		{
			string[] formats = null;

			if (dataObj != null)
			{
				formats = dataObj.GetFormats();
			}
		    if (formats == null)
		    {
		        return new List<string>();
		    }
		    Log.Debug().WriteLine("Got clipboard formats: {0}", string.Join(",", formats));
		    return new List<string>(formats);
		}

		/// <summary>
		///     Check if there is currently something in the dataObject which has the supplied format
		/// </summary>
		/// <param name="format">string with format</param>
		/// <returns>true if one the format is found</returns>
		public static bool ContainsFormat(string format)
		{
			return ContainsFormat(GetDataObject(), new[] {format});
		}

		/// <summary>
		///     Check if there is currently something on the clipboard which has the supplied format
		/// </summary>
		/// <param name="dataObject">IDataObject</param>
		/// <param name="format">string with format</param>
		/// <returns>true if one the format is found</returns>
		public static bool ContainsFormat(IDataObject dataObject, string format)
		{
			return ContainsFormat(dataObject, new[] {format});
		}

		/// <summary>
		///     Check if there is currently something on the clipboard which has one of the supplied formats
		/// </summary>
		/// <param name="formats">string[] with formats</param>
		/// <returns>true if one of the formats was found</returns>
		public static bool ContainsFormat(string[] formats)
		{
			return ContainsFormat(GetDataObject(), formats);
		}

		/// <summary>
		///     Check if there is currently something on the clipboard which has one of the supplied formats
		/// </summary>
		/// <param name="dataObject">IDataObject</param>
		/// <param name="formats">string[] with formats</param>
		/// <returns>true if one of the formats was found</returns>
		public static bool ContainsFormat(IDataObject dataObject, string[] formats)
		{
		    var currentFormats = GetFormats(dataObject);
			if (currentFormats == null || currentFormats.Count == 0 || formats == null || formats.Length == 0)
			{
				return false;
			}
		    return formats.Any(format => currentFormats.Contains(format));
		}

		/// <summary>
		///     Get Object of type Type from the clipboard
		/// </summary>
		/// <param name="type">Type to get</param>
		/// <returns>object from clipboard</returns>
		public static object GetClipboardData(Type type)
		{
			var format = type.FullName;
			return GetClipboardData(format);
		}

		/// <summary>
		///     Get Object for format from IDataObject
		/// </summary>
		/// <param name="dataObj">IDataObject</param>
		/// <param name="type">Type to get</param>
		/// <returns>object from IDataObject</returns>
		public static object GetFromDataObject(IDataObject dataObj, Type type)
		{
		    return type != null ? GetFromDataObject(dataObj, type.FullName) : null;
		}

		/// <summary>
		///     Get ImageFilenames from the IDataObject
		/// </summary>
		/// <param name="dataObject">IDataObject</param>
		/// <returns></returns>
		public static IEnumerable<string> GetImageFilenames(IDataObject dataObject)
		{
			var dropFileNames = (string[]) dataObject.GetData(DataFormats.FileDrop);
			if (dropFileNames != null && dropFileNames.Length > 0)
			{
				return dropFileNames
					.Where(filename => !string.IsNullOrEmpty(filename))
					.Where(Path.HasExtension)
					.Where(filename => BitmapHelper.StreamConverters.Keys.Contains(Path.GetExtension(filename).ToLowerInvariant().Substring(1)));
			}
			return Enumerable.Empty<string>();
		}

		/// <summary>
		///     Get Object for format from IDataObject
		/// </summary>
		/// <param name="dataObj">IDataObject</param>
		/// <param name="format">format to get</param>
		/// <returns>object from IDataObject</returns>
		public static object GetFromDataObject(IDataObject dataObj, string format)
		{
		    if (dataObj == null)
		    {
		        return null;
		    }
		    try
		    {
		        return dataObj.GetData(format);
		    }
		    catch (Exception e)
		    {
		        Log.Error().WriteLine(e, "Error in GetClipboardData.");
		    }
		    return null;
		}

		/// <summary>
		///     Get Object for format from the clipboard
		/// </summary>
		/// <param name="format">format to get</param>
		/// <returns>object from clipboard</returns>
		public static object GetClipboardData(string format)
		{
			return GetFromDataObject(GetDataObject(), format);
		}
	}
}