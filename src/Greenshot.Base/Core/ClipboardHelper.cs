/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.UnmanagedHelpers;
using log4net;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Description of ClipboardHelper.
    /// </summary>
    public static class ClipboardHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClipboardHelper));
        private static readonly object ClipboardLockObject = new object();
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        private static readonly string FORMAT_FILECONTENTS = "FileContents";
        private static readonly string FORMAT_HTML = "text/html";
        private static readonly string FORMAT_PNG = "PNG";
        private static readonly string FORMAT_PNG_OFFICEART = "PNG+Office Art";
        private static readonly string FORMAT_17 = "Format17";
        private static readonly string FORMAT_JPG = "JPG";
        private static readonly string FORMAT_JPEG = "JPEG";
        private static readonly string FORMAT_JFIF = "JFIF";
        private static readonly string FORMAT_JFIF_OFFICEART = "JFIF+Office Art";
        private static readonly string FORMAT_GIF = "GIF";

        private static readonly string FORMAT_BITMAP = "System.Drawing.Bitmap";
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

        /// <summary>
        /// Get the current "ClipboardOwner" but only if it isn't us!
        /// </summary>
        /// <returns>current clipboard owner</returns>
        private static string GetClipboardOwner()
        {
            string owner = null;
            try
            {
                IntPtr hWnd = User32.GetClipboardOwner();
                if (hWnd != IntPtr.Zero)
                {
                    try
                    {
                        User32.GetWindowThreadProcessId(hWnd, out var pid);
                        using Process me = Process.GetCurrentProcess();
                        using Process ownerProcess = Process.GetProcessById(pid);
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
                    catch (Exception e)
                    {
                        Log.Warn("Non critical error: Couldn't get clipboard process, trying to use the title.", e);
                        var title = new StringBuilder(260, 260);
                        User32.GetWindowText(hWnd, title, title.Capacity);
                        owner = title.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn("Non critical error: Couldn't get clipboard owner.", e);
            }

            return owner;
        }

        /// <summary>
        /// The SetDataObject will lock/try/catch clipboard operations making it save and not show exceptions.
        /// The bool "copy" is used to decided if the information stays on the clipboard after exit.
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
                    Log.Warn(clearException.Message);
                }

                try
                {
                    // For BUG-1935 this was changed from looping ourselfs, or letting MS retry...
                    Clipboard.SetDataObject(ido, copy, 15, 200);
                }
                catch (Exception clipboardSetException)
                {
                    string messageText;
                    string clipboardOwner = GetClipboardOwner();
                    if (clipboardOwner != null)
                    {
                        messageText = Language.GetFormattedString("clipboard_inuse", clipboardOwner);
                    }
                    else
                    {
                        messageText = Language.GetString("clipboard_error");
                    }

                    Log.Error(messageText, clipboardSetException);
                }
            }
        }

        /// <summary>
        /// The GetDataObject will lock/try/catch clipboard operations making it save and not show exceptions.
        /// </summary>
        public static IDataObject GetDataObject()
        {
            lock (ClipboardLockObject)
            {
                int retryCount = 2;
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
                            string clipboardOwner = GetClipboardOwner();
                            if (clipboardOwner != null)
                            {
                                messageText = Language.GetFormattedString("clipboard_inuse", clipboardOwner);
                            }
                            else
                            {
                                messageText = Language.GetString("clipboard_error");
                            }

                            Log.Error(messageText, ee);
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
        /// Test if the IDataObject contains Text
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns></returns>
        public static bool ContainsText(IDataObject dataObject)
        {
            if (dataObject != null)
            {
                if (dataObject.GetDataPresent(DataFormats.Text) || dataObject.GetDataPresent(DataFormats.UnicodeText))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Wrapper for Clipboard.ContainsImage, specialized for Greenshot, Created for Bug #3432313
        /// </summary>
        /// <returns>boolean if there is an image on the clipboard</returns>
        public static bool ContainsImage()
        {
            IDataObject clipboardData = GetDataObject();
            return ContainsImage(clipboardData);
        }

        /// <summary>
        /// Check if the IDataObject has an image
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>true if an image is there</returns>
        public static bool ContainsImage(IDataObject dataObject)
        {
            if (dataObject == null) return false;

            if (dataObject.GetDataPresent(DataFormats.Bitmap)
                || dataObject.GetDataPresent(DataFormats.Dib)
                || dataObject.GetDataPresent(DataFormats.Tiff)
                || dataObject.GetDataPresent(DataFormats.EnhancedMetafile)
                || dataObject.GetDataPresent(FORMAT_PNG)
                || dataObject.GetDataPresent(FORMAT_17)
                || dataObject.GetDataPresent(FORMAT_JPG)
                || dataObject.GetDataPresent(FORMAT_JFIF)
                || dataObject.GetDataPresent(FORMAT_JPEG)
                || dataObject.GetDataPresent(FORMAT_GIF))
            {
                return true;
            }

            var imageFiles = GetImageFilenames(dataObject);
            if (imageFiles.Any())
            {
                return true;
            }

            var fileDescriptor = (MemoryStream) dataObject.GetData("FileGroupDescriptorW");
            var files = FileDescriptorReader.Read(fileDescriptor);
            var fileIndex = 0;
            foreach (var fileContentFile in files)
            {
                if ((fileContentFile.FileAttributes & FileAttributes.Directory) != 0)
                {
                    //Do something with directories?
                    //Note that directories do not have FileContents
                    //And will throw if we try to read them
                    continue;
                }

                var fileData = FileDescriptorReader.GetFileContents(dataObject, fileIndex);
                try
                {
                    //Do something with the fileContent Stream
                    if (IsValidStream(fileData))
                    {
                        fileData.Position = 0;
                        using (ImageHelper.FromStream(fileData))
                        {
                            // If we get here, there is an image
                            return true;
                        }
                    }
                }
                finally
                {
                    fileData?.Dispose();
                }

                fileIndex++;
            }

            if (dataObject.GetDataPresent(FORMAT_FILECONTENTS))
            {
                try
                {
                    var clipboardContent = dataObject.GetData(FORMAT_FILECONTENTS, true);
                    var imageStream = clipboardContent as MemoryStream;
                    if (IsValidStream(imageStream))
                    {
                        using (ImageHelper.FromStream(imageStream))
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
            }

            // Try to get the image from the HTML code
            var textObject = ContentAsString(dataObject, FORMAT_HTML, Encoding.UTF8);
            if (textObject != null)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(textObject);
                var imgNodes = doc.DocumentNode.SelectNodes("//img");
                if (imgNodes != null)
                {
                    foreach (var imgNode in imgNodes)
                    {
                        var srcAttribute = imgNode.Attributes["src"];
                        var imageUrl = srcAttribute.Value;
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get the specified IDataObject format as a string
        /// </summary>
        /// <param name="dataObject">IDataObject</param>
        /// <param name="format">string</param>
        /// <param name="encoding">Encoding</param>
        /// <returns>sting</returns>
        private static string ContentAsString(IDataObject dataObject, string format, Encoding encoding = null)
        {
            encoding ??= Encoding.Unicode;
            var objectAsFormat = dataObject.GetData(format);
            return objectAsFormat switch
            {
                null => null,
                string text => text,
                MemoryStream ms => encoding.GetString(ms.ToArray()),
                _ => null
            };
        }

        /// <summary>
        /// Simple helper to check the stream
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <returns></returns>
        private static bool IsValidStream(MemoryStream memoryStream)
        {
            return memoryStream != null && memoryStream.Length > 0;
        }

        /// <summary>
        /// Wrapper for Clipboard.GetImage, Created for Bug #3432313
        /// </summary>
        /// <returns>Image if there is an image on the clipboard</returns>
        public static Image GetImage()
        {
            IDataObject clipboardData = GetDataObject();
            // Return the first image
            foreach (Image clipboardImage in GetImages(clipboardData))
            {
                return clipboardImage;
            }

            return null;
        }

        /// <summary>
        /// Get all images (multiple if filenames are available) from the dataObject
        /// Returned images must be disposed by the calling code!
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>IEnumerable of Image</returns>
        public static IEnumerable<Image> GetImages(IDataObject dataObject)
        {
            // Get single image, this takes the "best" match
            Image singleImage = GetImage(dataObject);
            if (singleImage != null)
            {
                Log.InfoFormat("Got image from clipboard with size {0} and format {1}", singleImage.Size, singleImage.PixelFormat);
                yield return singleImage;
            }
            else
            {
                // check if files are supplied
                foreach (string imageFile in GetImageFilenames(dataObject))
                {
                    Image returnImage = null;
                    try
                    {
                        returnImage = ImageHelper.LoadImage(imageFile);
                    }
                    catch (Exception streamImageEx)
                    {
                        Log.Error("Problem retrieving Image from clipboard.", streamImageEx);
                    }

                    if (returnImage != null)
                    {
                        Log.InfoFormat("Got image from clipboard with size {0} and format {1}", returnImage.Size, returnImage.PixelFormat);
                        yield return returnImage;
                    }
                }
            }
        }

        /// <summary>
        /// Get an Image from the IDataObject, don't check for FileDrop
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>Image or null</returns>
        private static Image GetImage(IDataObject dataObject)
        {
            Image returnImage = null;
            if (dataObject != null)
            {
                IList<string> formats = GetFormats(dataObject);
                string[] retrieveFormats;

                // Found a weird bug, where PNG's from Outlook 2010 are clipped
                // So I build some special logik to get the best format:
                if (formats != null && formats.Contains(FORMAT_PNG_OFFICEART) && formats.Contains(DataFormats.Dib))
                {
                    // Outlook ??
                    Log.Info("Most likely the current clipboard contents come from Outlook, as this has a problem with PNG and others we place the DIB format to the front...");
                    retrieveFormats = new[]
                    {
                        DataFormats.Dib, FORMAT_BITMAP, FORMAT_FILECONTENTS, FORMAT_PNG_OFFICEART, FORMAT_PNG, FORMAT_JFIF_OFFICEART, FORMAT_JPG, FORMAT_JPEG, FORMAT_JFIF,
                        DataFormats.Tiff, FORMAT_GIF, FORMAT_HTML
                    };
                }
                else
                {
                    retrieveFormats = new[]
                    {
                        FORMAT_PNG_OFFICEART, FORMAT_PNG, FORMAT_17, FORMAT_JFIF_OFFICEART, FORMAT_JPG, FORMAT_JPEG, FORMAT_JFIF, DataFormats.Tiff, DataFormats.Dib, FORMAT_BITMAP,
                        FORMAT_FILECONTENTS, FORMAT_GIF, FORMAT_HTML
                    };
                }

                foreach (string currentFormat in retrieveFormats)
                {
                    if (formats != null && formats.Contains(currentFormat))
                    {
                        Log.InfoFormat("Found {0}, trying to retrieve.", currentFormat);
                        returnImage = GetImageForFormat(currentFormat, dataObject);
                    }
                    else
                    {
                        Log.DebugFormat("Couldn't find format {0}.", currentFormat);
                    }

                    if (returnImage != null)
                    {
                        return returnImage;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Helper method to try to get an image in the specified format from the dataObject
        /// the DIB reader should solve some issues
        /// It also supports Format17/DibV5, by using the following information: http://stackoverflow.com/a/14335591
        /// </summary>
        /// <param name="format">string with the format</param>
        /// <param name="dataObject">IDataObject</param>
        /// <returns>Image or null</returns>
        private static Image GetImageForFormat(string format, IDataObject dataObject)
        {
            if (format == FORMAT_HTML)
            {
                var textObject = ContentAsString(dataObject, FORMAT_HTML, Encoding.UTF8);
                if (textObject != null)
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(textObject);
                    var imgNodes = doc.DocumentNode.SelectNodes("//img");
                    if (imgNodes != null)
                    {
                        foreach (var imgNode in imgNodes)
                        {
                            var srcAttribute = imgNode.Attributes["src"];
                            var imageUrl = srcAttribute.Value;
                            Log.Debug(imageUrl);
                            var image = NetworkHelper.DownloadImage(imageUrl);
                            if (image != null)
                            {
                                return image;
                            }
                        }
                    }
                }
            }

            object clipboardObject = GetFromDataObject(dataObject, format);
            var imageStream = clipboardObject as MemoryStream;
            if (!IsValidStream(imageStream))
            {
                // TODO: add "HTML Format" support here...
                return clipboardObject as Image;
            }

            if (CoreConfig.EnableSpecialDIBClipboardReader)
            {
                if (format == FORMAT_17 || format == DataFormats.Dib)
                {
                    Log.Info("Found DIB stream, trying to process it.");
                    try
                    {
                        if (imageStream != null)
                        {
                            byte[] dibBuffer = new byte[imageStream.Length];
                            imageStream.Read(dibBuffer, 0, dibBuffer.Length);
                            var infoHeader = BinaryStructHelper.FromByteArray<BITMAPINFOHEADER>(dibBuffer);
                            if (!infoHeader.IsDibV5)
                            {
                                Log.InfoFormat("Using special DIB <v5 format reader with biCompression {0}", infoHeader.biCompression);
                                int fileHeaderSize = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
                                uint infoHeaderSize = infoHeader.biSize;
                                int fileSize = (int) (fileHeaderSize + infoHeader.biSize + infoHeader.biSizeImage);

                                var fileHeader = new BITMAPFILEHEADER
                                {
                                    bfType = BITMAPFILEHEADER.BM,
                                    bfSize = fileSize,
                                    bfReserved1 = 0,
                                    bfReserved2 = 0,
                                    bfOffBits = (int) (fileHeaderSize + infoHeaderSize + infoHeader.biClrUsed * 4)
                                };

                                byte[] fileHeaderBytes = BinaryStructHelper.ToByteArray(fileHeader);

                                using var bitmapStream = new MemoryStream();
                                bitmapStream.Write(fileHeaderBytes, 0, fileHeaderSize);
                                bitmapStream.Write(dibBuffer, 0, dibBuffer.Length);
                                bitmapStream.Seek(0, SeekOrigin.Begin);
                                var image = ImageHelper.FromStream(bitmapStream);
                                if (image != null)
                                {
                                    return image;
                                }
                            }
                            else
                            {
                                Log.Info("Using special DIBV5 / Format17 format reader");
                                // CF_DIBV5
                                IntPtr gcHandle = IntPtr.Zero;
                                try
                                {
                                    GCHandle handle = GCHandle.Alloc(dibBuffer, GCHandleType.Pinned);
                                    gcHandle = GCHandle.ToIntPtr(handle);
                                    return
                                        new Bitmap(infoHeader.biWidth, infoHeader.biHeight,
                                            -(int) (infoHeader.biSizeImage / infoHeader.biHeight),
                                            infoHeader.biBitCount == 32 ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb,
                                            new IntPtr(handle.AddrOfPinnedObject().ToInt32() + infoHeader.OffsetToPixels +
                                                       (infoHeader.biHeight - 1) * (int) (infoHeader.biSizeImage / infoHeader.biHeight))
                                        );
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("Problem retrieving Format17 from clipboard.", ex);
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
                        Log.Error("Problem retrieving DIB from clipboard.", dibEx);
                    }
                }
            }
            else
            {
                Log.Info("Skipping special DIB format reader as it's disabled in the configuration.");
            }

            try
            {
                if (imageStream != null)
                {
                    imageStream.Seek(0, SeekOrigin.Begin);
                    var tmpImage = ImageHelper.FromStream(imageStream);
                    if (tmpImage != null)
                    {
                        Log.InfoFormat("Got image with clipboard format {0} from the clipboard.", format);
                        return tmpImage;
                    }
                }
            }
            catch (Exception streamImageEx)
            {
                Log.Error($"Problem retrieving {format} from clipboard.", streamImageEx);
            }

            return null;
        }

        /// <summary>
        /// Get Text from the DataObject
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
        /// Set text to the clipboard
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
            string utf8EncodedHtmlString = Encoding.GetEncoding(0).GetString(Encoding.UTF8.GetBytes(HtmlClipboardString));
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${width}", surface.Image.Width.ToString());
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${height}", surface.Image.Height.ToString());
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${file}", filename.Replace("\\", "/"));
            StringBuilder sb = new StringBuilder();
            sb.Append(utf8EncodedHtmlString);
            sb.Replace("<<<<<<<1", (utf8EncodedHtmlString.IndexOf("<HTML>", StringComparison.Ordinal) + "<HTML>".Length).ToString("D8"));
            sb.Replace("<<<<<<<2", (utf8EncodedHtmlString.IndexOf("</HTML>", StringComparison.Ordinal)).ToString("D8"));
            sb.Replace("<<<<<<<3", (utf8EncodedHtmlString.IndexOf("<!--StartFragment -->", StringComparison.Ordinal) + "<!--StartFragment -->".Length).ToString("D8"));
            sb.Replace("<<<<<<<4", (utf8EncodedHtmlString.IndexOf("<!--EndFragment -->", StringComparison.Ordinal)).ToString("D8"));
            return sb.ToString();
        }

        private static string GetHtmlDataUrlString(ISurface surface, MemoryStream pngStream)
        {
            string utf8EncodedHtmlString = Encoding.GetEncoding(0).GetString(Encoding.UTF8.GetBytes(HtmlClipboardBase64String));
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${width}", surface.Image.Width.ToString());
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${height}", surface.Image.Height.ToString());
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${format}", "png");
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${data}", Convert.ToBase64String(pngStream.GetBuffer(), 0, (int) pngStream.Length));
            StringBuilder sb = new StringBuilder();
            sb.Append(utf8EncodedHtmlString);
            sb.Replace("<<<<<<<1", (utf8EncodedHtmlString.IndexOf("<HTML>", StringComparison.Ordinal) + "<HTML>".Length).ToString("D8"));
            sb.Replace("<<<<<<<2", (utf8EncodedHtmlString.IndexOf("</HTML>", StringComparison.Ordinal)).ToString("D8"));
            sb.Replace("<<<<<<<3", (utf8EncodedHtmlString.IndexOf("<!--StartFragment -->", StringComparison.Ordinal) + "<!--StartFragment -->".Length).ToString("D8"));
            sb.Replace("<<<<<<<4", (utf8EncodedHtmlString.IndexOf("<!--EndFragment -->", StringComparison.Ordinal)).ToString("D8"));
            return sb.ToString();
        }

        private const int BITMAPFILEHEADER_LENGTH = 14;

        /// <summary>
        /// Set an Image to the clipboard
        /// This method will place images to the clipboard depending on the ClipboardFormats setting.
        /// e.g. Bitmap which works with pretty much everything and type Dib for e.g. OpenOffice
        /// because OpenOffice has a bug http://qa.openoffice.org/issues/show_bug.cgi?id=85661
        /// The Dib (Device Indenpendend Bitmap) in 32bpp actually won't work with Powerpoint 2003!
        /// When pasting a Dib in PP 2003 the Bitmap is somehow shifted left!
        /// For this problem the user should not use the direct paste (=Dib), but select Bitmap
        /// </summary>
        public static void SetClipboardData(ISurface surface)
        {
            DataObject dataObject = new DataObject();

            // This will work for Office and most other applications
            //ido.SetData(DataFormats.Bitmap, true, image);

            MemoryStream dibStream = null;
            MemoryStream dibV5Stream = null;
            MemoryStream pngStream = null;
            Image imageToSave = null;
            bool disposeImage = false;
            try
            {
                SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(OutputFormat.png, 100, false);
                // Create the image which is going to be saved so we don't create it multiple times
                disposeImage = ImageOutput.CreateImageFromSurface(surface, outputSettings, out imageToSave);
                try
                {
                    // Create PNG stream
                    if (CoreConfig.ClipboardFormats.Contains(ClipboardFormat.PNG))
                    {
                        pngStream = new MemoryStream();
                        // PNG works for e.g. Powerpoint
                        SurfaceOutputSettings pngOutputSettings = new SurfaceOutputSettings(OutputFormat.png, 100, false);
                        ImageOutput.SaveToStream(imageToSave, null, pngStream, pngOutputSettings);
                        pngStream.Seek(0, SeekOrigin.Begin);
                        // Set the PNG stream
                        dataObject.SetData(FORMAT_PNG, false, pngStream);
                    }
                }
                catch (Exception pngEx)
                {
                    Log.Error("Error creating PNG for the Clipboard.", pngEx);
                }

                try
                {
                    if (CoreConfig.ClipboardFormats.Contains(ClipboardFormat.DIB))
                    {
                        using (MemoryStream tmpBmpStream = new MemoryStream())
                        {
                            // Save image as BMP
                            SurfaceOutputSettings bmpOutputSettings = new SurfaceOutputSettings(OutputFormat.bmp, 100, false);
                            ImageOutput.SaveToStream(imageToSave, null, tmpBmpStream, bmpOutputSettings);

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
                    Log.Error("Error creating DIB for the Clipboard.", dibEx);
                }

                // CF_DibV5
                try
                {
                    if (CoreConfig.ClipboardFormats.Contains(ClipboardFormat.DIBV5))
                    {
                        // Create the stream for the clipboard
                        dibV5Stream = new MemoryStream();

                        // Create the BITMAPINFOHEADER
                        BITMAPINFOHEADER header = new BITMAPINFOHEADER(imageToSave.Width, imageToSave.Height, 32)
                        {
                            // Make sure we have BI_BITFIELDS, this seems to be normal for Format17?
                            biCompression = BI_COMPRESSION.BI_BITFIELDS
                        };
                        // Create a byte[] to write
                        byte[] headerBytes = BinaryStructHelper.ToByteArray(header);
                        // Write the BITMAPINFOHEADER to the stream
                        dibV5Stream.Write(headerBytes, 0, headerBytes.Length);

                        // As we have specified BI_COMPRESSION.BI_BITFIELDS, the BitfieldColorMask needs to be added
                        BitfieldColorMask colorMask = new BitfieldColorMask();
                        // Make sure the values are set
                        colorMask.InitValues();
                        // Create the byte[] from the struct
                        byte[] colorMaskBytes = BinaryStructHelper.ToByteArray(colorMask);
                        Array.Reverse(colorMaskBytes);
                        // Write to the stream
                        dibV5Stream.Write(colorMaskBytes, 0, colorMaskBytes.Length);

                        // Create the raw bytes for the pixels only
                        byte[] bitmapBytes = BitmapToByteArray((Bitmap) imageToSave);
                        // Write to the stream
                        dibV5Stream.Write(bitmapBytes, 0, bitmapBytes.Length);

                        // Set the DIBv5 to the clipboard DataObject
                        dataObject.SetData(FORMAT_17, true, dibV5Stream);
                    }
                }
                catch (Exception dibEx)
                {
                    Log.Error("Error creating DIB for the Clipboard.", dibEx);
                }

                // Set the HTML
                if (CoreConfig.ClipboardFormats.Contains(ClipboardFormat.HTML))
                {
                    string tmpFile = ImageOutput.SaveToTmpFile(surface, new SurfaceOutputSettings(OutputFormat.png, 100, false), null);
                    string html = GetHtmlString(surface, tmpFile);
                    dataObject.SetText(html, TextDataFormat.Html);
                }
                else if (CoreConfig.ClipboardFormats.Contains(ClipboardFormat.HTMLDATAURL))
                {
                    string html;
                    using (MemoryStream tmpPngStream = new MemoryStream())
                    {
                        SurfaceOutputSettings pngOutputSettings = new SurfaceOutputSettings(OutputFormat.png, 100, false)
                        {
                            // Do not allow to reduce the colors, some applications dislike 256 color images
                            // reported with bug #3594681
                            DisableReduceColors = true
                        };
                        // Check if we can use the previously used image
                        if (imageToSave.PixelFormat != PixelFormat.Format8bppIndexed)
                        {
                            ImageOutput.SaveToStream(imageToSave, surface, tmpPngStream, pngOutputSettings);
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
                if (CoreConfig.ClipboardFormats.Contains(ClipboardFormat.BITMAP))
                {
                    dataObject.SetImage(imageToSave);
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
                    imageToSave?.Dispose();
                }
            }
        }

        /// <summary>
        /// Helper method so get the bitmap bytes
        /// See: http://stackoverflow.com/a/6570155
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <returns>byte[]</returns>
        private static byte[] BitmapToByteArray(Bitmap bitmap)
        {
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

            int absStride = Math.Abs(bmpData.Stride);
            int bytes = absStride * bitmap.Height;
            long ptr = bmpData.Scan0.ToInt32();
            // Declare an array to hold the bytes of the bitmap.
            byte[] rgbValues = new byte[bytes];

            for (int i = 0; i < bitmap.Height; i++)
            {
                IntPtr pointer = new IntPtr(ptr + (bmpData.Stride * i));
                Marshal.Copy(pointer, rgbValues, absStride * (bitmap.Height - i - 1), absStride);
            }

            // Unlock the bits.
            bitmap.UnlockBits(bmpData);

            return rgbValues;
        }

        /// <summary>
        /// Set Object with type Type to the clipboard
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="obj">object</param>
        public static void SetClipboardData(Type type, object obj)
        {
            DataFormats.Format format = DataFormats.GetFormat(type.FullName);

            //now copy to clipboard
            IDataObject dataObj = new DataObject();
            dataObj.SetData(format.Name, false, obj);
            // Use false to make the object disappear when the application stops.
            SetDataObject(dataObj, true);
        }

        /// <summary>
        /// Retrieve a list of all formats currently in the IDataObject
        /// </summary>
        /// <returns>List of string with the current formats</returns>
        public static List<string> GetFormats(IDataObject dataObj)
        {
            string[] formats = null;

            if (dataObj != null)
            {
                formats = dataObj.GetFormats();
            }

            if (formats != null)
            {
                Log.DebugFormat("Got clipboard formats: {0}", string.Join(",", formats));
                return new List<string>(formats);
            }

            return new List<string>();
        }

        /// <summary>
        /// Check if there is currently something on the clipboard which has the supplied format
        /// </summary>
        /// <param name="dataObject">IDataObject</param>
        /// <param name="format">string with format</param>
        /// <returns>true if one the format is found</returns>
        public static bool ContainsFormat(IDataObject dataObject, string format)
        {
            return ContainsFormat(dataObject, new[]
            {
                format
            });
        }

        /// <summary>
        /// Check if there is currently something on the clipboard which has one of the supplied formats
        /// </summary>
        /// <param name="formats">string[] with formats</param>
        /// <returns>true if one of the formats was found</returns>
        public static bool ContainsFormat(string[] formats)
        {
            return ContainsFormat(GetDataObject(), formats);
        }

        /// <summary>
        /// Check if there is currently something on the clipboard which has one of the supplied formats
        /// </summary>
        /// <param name="dataObject">IDataObject</param>
        /// <param name="formats">string[] with formats</param>
        /// <returns>true if one of the formats was found</returns>
        public static bool ContainsFormat(IDataObject dataObject, string[] formats)
        {
            bool formatFound = false;
            List<string> currentFormats = GetFormats(dataObject);
            if (currentFormats == null || currentFormats.Count == 0 || formats == null || formats.Length == 0)
            {
                return false;
            }

            foreach (string format in formats)
            {
                if (currentFormats.Contains(format))
                {
                    formatFound = true;
                    break;
                }
            }

            return formatFound;
        }

        /// <summary>
        /// Get Object for format from IDataObject
        /// </summary>
        /// <param name="dataObj">IDataObject</param>
        /// <param name="type">Type to get</param>
        /// <returns>object from IDataObject</returns>
        public static object GetFromDataObject(IDataObject dataObj, Type type)
        {
            if (type != null)
            {
                return GetFromDataObject(dataObj, type.FullName);
            }

            return null;
        }

        /// <summary>
        /// Get ImageFilenames from the IDataObject
        /// </summary>
        /// <param name="dataObject">IDataObject</param>
        /// <returns></returns>
        public static IEnumerable<string> GetImageFilenames(IDataObject dataObject)
        {
            string[] dropFileNames = (string[]) dataObject.GetData(DataFormats.FileDrop);
            if (dropFileNames != null && dropFileNames.Length > 0)
            {
                return dropFileNames
                    .Where(filename => !string.IsNullOrEmpty(filename))
                    .Where(Path.HasExtension)
                    .Where(filename => ImageHelper.StreamConverters.Keys.Contains(Path.GetExtension(filename).ToLowerInvariant().Substring(1)));
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Get Object for format from IDataObject
        /// </summary>
        /// <param name="dataObj">IDataObject</param>
        /// <param name="format">format to get</param>
        /// <returns>object from IDataObject</returns>
        public static object GetFromDataObject(IDataObject dataObj, string format)
        {
            if (dataObj != null)
            {
                try
                {
                    return dataObj.GetData(format);
                }
                catch (Exception e)
                {
                    Log.Error("Error in GetClipboardData.", e);
                }
            }

            return null;
        }
    }
}