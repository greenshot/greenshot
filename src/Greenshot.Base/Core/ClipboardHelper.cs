/*
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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
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
        // see: https://msdn.microsoft.com/en-us/library/ms649015%28v=v
        // s.85%29.aspx
        // or:  https://msdn.microsoft.com/en-us/library/Aa767917.aspx
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
                IntPtr hWnd = ClipboardNative.CurrentOwner;
                if (hWnd != IntPtr.Zero)
                {
                    try
                    {
                        User32Api.GetWindowThreadProcessId(hWnd, out var pid);
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
                        owner = User32Api.GetText(hWnd);
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
                    // For BUG-1935 this was changed from looping ourselves, or letting MS retry...
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

            IList<string> formats = GetFormats(dataObject);
            Log.DebugFormat("Found formats: {0}", string.Join(",", formats));

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
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var supportedExtensions = fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadDrawableFromStream).ToList();
            foreach (var (stream, filename) in IterateClipboardContent(dataObject))
            {
                try
                {
                    var extension = Path.GetExtension(filename)?.ToLowerInvariant();
                    if (supportedExtensions.Contains(extension))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't read file contents", ex);
                }
                finally
                {
                    stream?.Dispose();
                }
            }

            if (dataObject.GetDataPresent(FORMAT_FILECONTENTS))
            {
                try
                {
                    var clipboardContent = dataObject.GetData(FORMAT_FILECONTENTS, true);
                    var imageStream = clipboardContent as MemoryStream;
                    if (IsValidStream(imageStream))
                    {
                        // TODO: How to check if we support "just a stream"?
                        using (ImageIO.FromStream(imageStream))
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
            if (textObject == null)
            {
                return false;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(textObject);
            var imgNodes = doc.DocumentNode.SelectNodes("//img");

            if (imgNodes == null)
            {
                return false;
            }

            foreach (var imgNode in imgNodes)
            {
                var srcAttribute = imgNode.Attributes["src"];
                var imageUrl = srcAttribute.Value;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Iterate the clipboard content
        /// </summary>
        /// <param name="dataObject">IDataObject</param>
        /// <returns>IEnumerable{(MemoryStream,string)}</returns>
        private static IEnumerable<(MemoryStream stream,string filename)> IterateClipboardContent(IDataObject dataObject)
        {
            if (dataObject == null) yield break;
            var fileDescriptors = AvailableFileDescriptors(dataObject);
            if (fileDescriptors == null) yield break;

            foreach (var fileData in IterateFileDescriptors(fileDescriptors, dataObject))
            {
                yield return fileData;
            }
        }

        /// <summary>
        /// Retrieve the FileDescriptor on the clipboard
        /// </summary>
        /// <param name="dataObject">IDataObject</param>
        /// <returns>IEnumerable{FileDescriptor}</returns>
        private static IEnumerable<FileDescriptor> AvailableFileDescriptors(IDataObject dataObject)
        {
            var fileDescriptor = (MemoryStream) dataObject.GetData("FileGroupDescriptorW");
            if (fileDescriptor != null)
            {
                try
                {
                    return FileDescriptorReader.Read(fileDescriptor);
                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't use FileDescriptorReader.", ex);
                }
            }

            return Enumerable.Empty<FileDescriptor>();
        }

        /// <summary>
        /// Iterate the file descriptors on the clipboard
        /// </summary>
        /// <param name="fileDescriptors">IEnumerable{FileDescriptor}</param>
        /// <param name="dataObject">IDataObject</param>
        /// <returns>IEnumerable{(MemoryStream stream, string filename)}</returns>
        private static IEnumerable<(MemoryStream stream, string filename)> IterateFileDescriptors(IEnumerable<FileDescriptor> fileDescriptors, IDataObject dataObject)
        {
            if (fileDescriptors == null)
            {
                yield break;
            }

            var fileIndex = 0;
            foreach (var fileDescriptor in fileDescriptors)
            {
                if ((fileDescriptor.FileAttributes & FileAttributes.Directory) != 0)
                {
                    //Do something with directories?
                    //Note that directories do not have FileContents
                    //And will throw if we try to read them
                    continue;
                }

                MemoryStream fileData = null;
                try
                {
                    fileData = FileDescriptorReader.GetFileContents(dataObject, fileIndex);
                    //Do something with the fileContent Stream
                }
                catch (Exception ex)
                {
                    Log.Error($"Couldn't read file contents for {fileDescriptor.FileName}.", ex);
                }
                if (fileData?.Length > 0)
                {
                    fileData.Position = 0;
                    yield return (fileData, fileDescriptor.FileName);
                }

                fileIndex++;
            }
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
        /// <returns>true if there is a valid stream</returns>
        private static bool IsValidStream(MemoryStream memoryStream)
        {
            return memoryStream?.Length > 0;
        }

        /// <summary>
        /// Wrapper for Clipboard.GetImage, Created for Bug #3432313
        /// </summary>
        /// <returns>Image if there is an image on the clipboard</returns>
        public static Image GetImage()
        {
            IDataObject clipboardData = GetDataObject();
            if (clipboardData == null)
            {
                return null;
            }
            // Return the first image
            foreach (var clipboardImage in GetImages(clipboardData))
            {
                return clipboardImage;
            }

            return null;
        }

        /// <summary>
        /// Get all images (multiple if file names are available) from the dataObject
        /// Returned images must be disposed by the calling code!
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>IEnumerable of Bitmap</returns>
        public static IEnumerable<Bitmap> GetImages(IDataObject dataObject)
        {
            // Get single image, this takes the "best" match
            Bitmap singleImage = GetImage(dataObject);
            if (singleImage != null)
            {
                Log.Info($"Got {singleImage.GetType()} from clipboard with size {singleImage.Size}");
                yield return singleImage;
                yield break;
            }

            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var supportedExtensions = fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadDrawableFromStream).ToList();

            foreach (var (stream, filename) in IterateClipboardContent(dataObject))
            {
                var extension = Path.GetExtension(filename)?.ToLowerInvariant();
                if (!supportedExtensions.Contains(extension))
                {
                    continue;
                }

                Bitmap bitmap = null;

                try
                {
                    if (!fileFormatHandlers.TryLoadFromStream(stream, extension, out bitmap))
                    {
                        continue;
                    }

                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't read file contents", ex);
                    continue;
                }
                finally
                {
                    stream?.Dispose();
                }
                // If we get here, there is an image
                yield return bitmap;
            }

            // check if files are supplied
            foreach (string imageFile in GetImageFilenames(dataObject))
            {
                var extension = Path.GetExtension(imageFile)?.ToLowerInvariant();
                if (!supportedExtensions.Contains(extension))
                {
                    continue;
                }

                Bitmap bitmap = null;
                using FileStream fileStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    if (!fileFormatHandlers.TryLoadFromStream(fileStream, extension, out bitmap))
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't read file contents", ex);
                    continue;
                }
                // If we get here, there is an image
                yield return bitmap;
            }
        }

        /// <summary>
        /// Get all images (multiple if file names are available) from the dataObject
        /// Returned images must be disposed by the calling code!
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>IEnumerable of IDrawableContainer</returns>
        public static IEnumerable<IDrawableContainer> GetDrawables(IDataObject dataObject)
        {
            // Get single image, this takes the "best" match
            IDrawableContainer singleImage = GetDrawable(dataObject);
            if (singleImage != null)
            {
                Log.InfoFormat($"Got {singleImage.GetType()} from clipboard with size {singleImage.Size}");
                yield return singleImage;
                yield break;
            }
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var supportedExtensions = fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadDrawableFromStream).ToList();

            foreach (var (stream, filename) in IterateClipboardContent(dataObject))
            {
                var extension = Path.GetExtension(filename)?.ToLowerInvariant();
                if (!supportedExtensions.Contains(extension))
                {
                    continue;
                }

                IEnumerable<IDrawableContainer> drawableContainers;
                try
                {
                    drawableContainers = fileFormatHandlers.LoadDrawablesFromStream(stream, extension);
                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't read file contents", ex);
                    continue;
                }
                finally
                {
                    stream?.Dispose();
                }
                // If we get here, there is an image
                foreach (var container in drawableContainers)
                {
                    yield return container;
                }
            }

            // check if files are supplied
            foreach (string imageFile in GetImageFilenames(dataObject))
            {
                var extension = Path.GetExtension(imageFile)?.ToLowerInvariant();
                if (!supportedExtensions.Contains(extension))
                {
                    continue;
                }
                using FileStream fileStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                IEnumerable<IDrawableContainer> drawableContainers;
                try
                {
                    drawableContainers = fileFormatHandlers.LoadDrawablesFromStream(fileStream, extension);
                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't read file contents", ex);
                    continue;
                }
                // If we get here, there is an image
                foreach (var container in drawableContainers)
                {
                    yield return container;
                }
            }
        }

        /// <summary>
        /// Get an Image from the IDataObject, don't check for FileDrop
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>Image or null</returns>
        private static Bitmap GetImage(IDataObject dataObject)
        {
            if (dataObject == null) return null;

            Bitmap returnImage = null;
            IList<string> formats = GetFormats(dataObject);
            string[] retrieveFormats;

            // Found a weird bug, where PNG's from Outlook 2010 are clipped
            // So I build some special logic to get the best format:
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

            return null;
        }

        /// <summary>
        /// Get an IDrawableContainer from the IDataObject, don't check for FileDrop
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns>Image or null</returns>
        private static IDrawableContainer GetDrawable(IDataObject dataObject)
        {
            if (dataObject == null) return null;

            IDrawableContainer returnImage = null;
            IList<string> formats = GetFormats(dataObject);
            string[] retrieveFormats;

            // Found a weird bug, where PNG's from Outlook 2010 are clipped
            // So I build some special logic to get the best format:
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
                    returnImage = GetDrawableForFormat(currentFormat, dataObject);
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

            return null;
        }

        /// <summary>
        /// Helper method to try to get an Bitmap in the specified format from the dataObject
        /// the DIB reader should solve some issues
        /// It also supports Format17/DibV5, by using the following information: https://stackoverflow.com/a/14335591
        /// </summary>
        /// <param name="format">string with the format</param>
        /// <param name="dataObject">IDataObject</param>
        /// <returns>Bitmap or null</returns>
        private static Bitmap GetImageForFormat(string format, IDataObject dataObject)
        {
            Bitmap bitmap = null;

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
                            bitmap = NetworkHelper.DownloadImage(imageUrl);
                            if (bitmap != null)
                            {
                                return bitmap;
                            }
                        }
                    }
                }
            }

            object clipboardObject = GetFromDataObject(dataObject, format);
            var imageStream = clipboardObject as MemoryStream;
            if (!IsValidStream(imageStream))
            {
                return clipboardObject as Bitmap;
            }
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();

            // From here, imageStream is a valid stream
            if (fileFormatHandlers.TryLoadFromStream(imageStream, format, out bitmap))
            {
                return bitmap;
            }
            return null;
        }

        /// <summary>
        /// Helper method to try to get an IDrawableContainer in the specified format from the dataObject
        /// the DIB reader should solve some issues
        /// It also supports Format17/DibV5, by using the following information: https://stackoverflow.com/a/14335591
        /// </summary>
        /// <param name="format">string with the format</param>
        /// <param name="dataObject">IDataObject</param>
        /// <returns>IDrawableContainer or null</returns>
        private static IDrawableContainer GetDrawableForFormat(string format, IDataObject dataObject)
        {
            IDrawableContainer drawableContainer = null;

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
                            drawableContainer = NetworkHelper.DownloadImageAsDrawableContainer(imageUrl);
                            if (drawableContainer != null)
                            {
                                return drawableContainer;
                            }
                        }
                    }
                }
            }

            object clipboardObject = GetFromDataObject(dataObject, format);
            var imageStream = clipboardObject as MemoryStream;
            if (!IsValidStream(imageStream))
            {
                // TODO: add text based, like "HTML Format" support here...
                // TODO: solve the issue that we do not have a factory for the ImageContainer
                /*var image = clipboardObject as Image;
                if (image != null)
                {
                    return new ImageContainer(this)
                    {
                        Image = image,
                        Left = x,
                        Top = y
                    };
                }
                return clipboardObject as Image;
*/
                return null;
            }

            // From here, imageStream is a valid stream
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();

            return fileFormatHandlers.LoadDrawablesFromStream(imageStream, format).FirstOrDefault();
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

        /// <summary>
        /// Set an Image to the clipboard
        /// This method will place images to the clipboard depending on the ClipboardFormats setting.
        /// e.g. Bitmap which works with pretty much everything and type Dib for e.g. OpenOffice
        /// because OpenOffice has a bug https://qa.openoffice.org/issues/show_bug.cgi?id=85661
        /// The Dib (Device Independent Bitmap) in 32bpp actually won't work with Powerpoint 2003!
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
                disposeImage = ImageIO.CreateImageFromSurface(surface, outputSettings, out imageToSave);
                try
                {
                    // Create PNG stream
                    if (CoreConfig.ClipboardFormats.Contains(ClipboardFormat.PNG))
                    {
                        pngStream = new MemoryStream();
                        // PNG works for e.g. Powerpoint
                        SurfaceOutputSettings pngOutputSettings = new SurfaceOutputSettings(OutputFormat.png, 100, false);
                        ImageIO.SaveToStream(imageToSave, null, pngStream, pngOutputSettings);
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
                        // Create the stream for the clipboard
                        dibStream = new MemoryStream();
                        var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();

                        if (!fileFormatHandlers.TrySaveToStream((Bitmap)imageToSave, dibStream, DataFormats.Dib))
                        {
                            dibStream.Dispose();
                            dibStream = null;
                        }
                        else
                        {
                            // Set the DIB to the clipboard DataObject
                            dataObject.SetData(DataFormats.Dib, false, dibStream);
                        }
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
                        var header = BitmapV5Header.Create(imageToSave.Width, imageToSave.Height, 32);
                        // Make sure we have BI_BITFIELDS, this seems to be normal for Format17?
                        header.Compression = BitmapCompressionMethods.BI_BITFIELDS;
                        // Create a byte[] to write
                        byte[] headerBytes = BinaryStructHelper.ToByteArray(header);
                        // Write the BITMAPINFOHEADER to the stream
                        dibV5Stream.Write(headerBytes, 0, headerBytes.Length);

                        // As we have specified BI_COMPRESSION.BI_BITFIELDS, the BitfieldColorMask needs to be added
                        // This also makes sure the default values are set
                        BitfieldColorMask colorMask = new BitfieldColorMask();
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
                    string tmpFile = ImageIO.SaveToTmpFile(surface, new SurfaceOutputSettings(OutputFormat.png, 100, false), null);
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
                            ImageIO.SaveToStream(imageToSave, surface, tmpPngStream, pngOutputSettings);
                        }
                        else
                        {
                            ImageIO.SaveToStream(surface, tmpPngStream, pngOutputSettings);
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
        /// See: https://stackoverflow.com/a/6570155
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <returns>byte[]</returns>
        private static byte[] BitmapToByteArray(Bitmap bitmap)
        {
            // Lock the bitmap's bits.  
            var rect = new NativeRect(0, 0, bitmap.Width, bitmap.Height);
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
            var currentFormats = GetFormats(dataObject);
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
            string[] dropFileNames = (string[])dataObject.GetData(DataFormats.FileDrop);
            if (dropFileNames is not { Length: > 0 }) return Enumerable.Empty<string>();
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();

            var supportedExtensions = fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadFromStream).ToList();
            return dropFileNames
                .Where(filename => !string.IsNullOrEmpty(filename))
                .Where(Path.HasExtension)
                .Where(filename => supportedExtensions.Contains(Path.GetExtension(filename)));

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