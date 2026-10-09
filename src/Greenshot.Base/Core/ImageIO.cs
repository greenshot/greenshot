/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Greenshot.Base.Core.FileFormatHandlers;
using Dapplo.Ini;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// This contains all io related logic for image
    /// </summary>
    public static class ImageIO
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageIO));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private static readonly int PROPERTY_TAG_SOFTWARE_USED = 0x0131;
        private static readonly Cache<string, string> TmpFileCache = new Cache<string, string>(10 * 60 * 60, RemoveExpiredTmpFile);

        /// <summary>
        /// Creates a PropertyItem (Metadata) to store with the image.
        /// For the possible ID's see: https://msdn.microsoft.com/de-de/library/system.drawing.imaging.propertyitem.id(v=vs.80).aspx
        /// This code uses Reflection to create a PropertyItem, although it's not advised it's not as stupid as having a image in the project so we can read a PropertyItem from that!
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="text">Text</param>
        /// <returns></returns>
        private static PropertyItem CreatePropertyItem(int id, string text)
        {
            PropertyItem propertyItem = null;
            try
            {
                ConstructorInfo ci = typeof(PropertyItem).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, new Type[]
                {
                }, null);
                propertyItem = (PropertyItem)ci.Invoke(null);
                // Make sure it's of type string
                propertyItem.Type = 2;
                // Set the ID
                propertyItem.Id = id;
                // Set the text
                byte[] byteString = Encoding.ASCII.GetBytes(text + " ");
                // Set Zero byte for String end.
                byteString[byteString.Length - 1] = 0;
                propertyItem.Value = byteString;
                propertyItem.Len = text.Length + 1;
            }
            catch (Exception e)
            {
                Log.WarnFormat("Error creating a PropertyItem: {0}", e.Message);
            }

            return propertyItem;
        }

        /// <summary>
        /// Saves ISurface to stream with specified output settings
        /// </summary>
        /// <param name="surface">ISurface to save</param>
        /// <param name="stream">Stream to save to</param>
        /// <param name="outputSettings">SurfaceOutputSettings</param>
        public static void SaveToStream(ISurface surface, Stream stream, SurfaceOutputSettings outputSettings)
        {
            bool disposeImage = CreateImageFromSurface(surface, outputSettings, out var imageToSave);
            SaveToStream(imageToSave, surface, stream, outputSettings);
            // cleanup if needed
            if (disposeImage)
            {
                imageToSave?.Dispose();
            }
        }

        /// <summary>
        /// Saves image to stream with specified quality
        /// To prevent problems with GDI version of before Windows 7:
        /// the stream is checked if it's seekable and if needed a MemoryStream as "cache" is used.
        /// </summary>
        /// <param name="imageToSave">image to save</param>
        /// <param name="surface">surface for the elements, needed if the greenshot format is used</param>
        /// <param name="stream">Stream to save to</param>
        /// <param name="outputSettings">SurfaceOutputSettings</param>
        public static void SaveToStream(Image imageToSave, ISurface surface, Stream stream, SurfaceOutputSettings outputSettings)
        {
            bool useMemoryStream = false;
            MemoryStream memoryStream = null;
            if (WellKnownFileFormats.IsGreenshotFormat(outputSettings.Format) && surface == null)
            {
                throw new ArgumentException("Surface needs to be set when using OutputFormat .greenshot");
            }

            try
            {
                // Check if we want to use a memory stream, to prevent issues with non seekable streams
                // The save is made to the targetStream, this is directed to either the MemoryStream or the original
                Stream targetStream = stream;
                if (!stream.CanSeek)
                {
                    useMemoryStream = true;
                    Log.Warn("Using a memory stream prevent an issue with saving to a non seekable stream.");
                    memoryStream = RecyclableMemoryStreamFactory.GetStream("ImageIO.SaveToStream");
                    targetStream = memoryStream;
                }

                var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
                if (!fileFormatHandlers.TrySaveToStream(imageToSave as Bitmap, targetStream, FileFormatRegistry.GetPreferredExtension(outputSettings.Format), surface, outputSettings))
                {
                    throw new InvalidOperationException($"No file format handler could save an image using format '{outputSettings.Format}'.");
                }

                // If we used a memory stream, we need to stream the memory stream to the original stream.
                if (useMemoryStream)
                {
                    memoryStream.WriteTo(stream);
                }
            }
            finally
            {
                memoryStream?.Dispose();
            }
        }

        /// <summary>
        /// Create an image from a surface with the settings from the output settings applied
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="outputSettings"></param>
        /// <param name="imageToSave"></param>
        /// <returns>true if the image must be disposed</returns>
        public static bool CreateImageFromSurface(ISurface surface, SurfaceOutputSettings outputSettings, out Image imageToSave)
        {
            if (WellKnownFileFormats.IsGreenshotFormat(outputSettings.Format) || outputSettings.SaveBackgroundOnly)
            {
                // We save the image of the surface, this should not be disposed
                imageToSave = surface.Image;
                // The following block of modifications should be skipped when saving the greenshot format, no effects or otherwise!
                if (WellKnownFileFormats.IsGreenshotFormat(outputSettings.Format))
                {
                    return false;
                }

                return CreateImageForOutput(imageToSave, false, outputSettings, out imageToSave);
            }

            // We create the export image of the surface to save
            return CreateImageForOutput(surface.GetImageForExport(), true, outputSettings, out imageToSave);
        }

        /// <summary>
        /// Apply the output settings (effects, color reduction) to an already rendered image, without the surface: this can run on any thread
        /// which owns (or borrows, and doesn't share) the source image.
        /// </summary>
        /// <param name="sourceImage">The rendered capture</param>
        /// <param name="ownsSourceImage">True when the source image may be disposed here once it is replaced</param>
        /// <param name="outputSettings">SurfaceOutputSettings</param>
        /// <param name="imageToSave">The result, can be the source image</param>
        /// <returns>true if the result is a new image (or the owned source image) which the caller must dispose</returns>
        public static bool CreateImageForOutput(Image sourceImage, bool ownsSourceImage, SurfaceOutputSettings outputSettings, out Image imageToSave)
        {
            imageToSave = sourceImage;
            bool disposeImage = ownsSourceImage;

            Image tmpImage;
            if (outputSettings.Effects != null && outputSettings.Effects.Count > 0)
            {
                // apply effects, if there are any
                using (Matrix matrix = new Matrix())
                {
                    tmpImage = ImageHelper.ApplyEffects(imageToSave, outputSettings.Effects, matrix);
                }

                if (tmpImage != null)
                {
                    if (disposeImage)
                    {
                        imageToSave.Dispose();
                    }

                    imageToSave = tmpImage;
                    disposeImage = true;
                }
            }

            // check for color reduction, forced or automatically, only when the DisableReduceColors is false 
            if (outputSettings.DisableReduceColors || (!CoreConfig.OutputFileAutoReduceColors && !outputSettings.ReduceColors))
            {
                return disposeImage;
            }

            bool isAlpha = Image.IsAlphaPixelFormat(imageToSave.PixelFormat);
            if (outputSettings.ReduceColors || (!isAlpha && CoreConfig.OutputFileAutoReduceColors))
            {
                using var quantizer = new WuQuantizer((Bitmap)imageToSave);
                int colorCount = quantizer.GetColorCount();
                Log.InfoFormat("Image with format {0} has {1} colors", imageToSave.PixelFormat, colorCount);
                if (!outputSettings.ReduceColors && colorCount >= 256)
                {
                    return disposeImage;
                }

                try
                {
                    Log.Info("Reducing colors on bitmap to 256.");
                    tmpImage = quantizer.GetQuantizedImage(CoreConfig.OutputFileReduceColorsTo);
                    if (disposeImage)
                    {
                        imageToSave.Dispose();
                    }

                    imageToSave = tmpImage;
                    // Make sure the "new" image is disposed
                    disposeImage = true;
                }
                catch (Exception e)
                {
                    Log.Warn("Error occurred while Quantizing the image, ignoring and using original. Error: ", e);
                }
            }
            else if (isAlpha && !outputSettings.ReduceColors)
            {
                Log.Info("Skipping 'optional' color reduction as the image has alpha");
            }

            return disposeImage;
        }

        /// <summary>
        /// Add the greenshot property!
        /// </summary>
        /// <param name="imageToSave"></param>
        public static void AddTag(this Image imageToSave)
        {
            // Create meta-data
            PropertyItem softwareUsedPropertyItem = CreatePropertyItem(PROPERTY_TAG_SOFTWARE_USED, "Greenshot");
            if (softwareUsedPropertyItem == null) return;
            try
            {
                imageToSave.SetPropertyItem(softwareUsedPropertyItem);
            }
            catch (Exception)
            {
                Log.WarnFormat("Couldn't set property {0}", softwareUsedPropertyItem.Id);
            }
        }

        /// <summary>
        /// Saves image to specific path with specified quality
        /// </summary>
        public static void Save(ISurface surface, string fullPath, bool allowOverwrite, SurfaceOutputSettings outputSettings, bool copyPathToClipboard)
        {
            fullPath = FilenameHelper.MakeFqFilenameSafe(fullPath);
            string path = Path.GetDirectoryName(fullPath);

            // check whether path exists - if not create it
            if (path != null)
            {
                DirectoryInfo di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    Directory.CreateDirectory(di.FullName);
                }
            }

            if (!allowOverwrite && File.Exists(fullPath))
            {
                ArgumentException throwingException = new ArgumentException("File '" + fullPath + "' already exists.");
                throwingException.Data.Add("fullPath", fullPath);
                throw throwingException;
            }

            Log.DebugFormat("Saving surface to {0}", fullPath);
            // Create the stream and call SaveToStream
            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                SaveToStream(surface, stream, outputSettings);
            }

            if (copyPathToClipboard)
            {
                ClipboardHelper.SetClipboardData(fullPath);
            }
        }

        /// <summary>
        /// Saves a pre-rendered bitmap to disk. Unlike <see cref="Save"/>, this method does not call
        /// <see cref="CreateImageFromSurface"/> — the caller is responsible for rendering the bitmap on the
        /// UI thread before calling this method, allowing the encode+write work to run on a background thread.
        /// If <paramref name="copyPathToClipboard"/> is true the path is placed on the clipboard, which works on any thread.
        /// </summary>
        /// <param name="uiContext">Not used anymore, the clipboard doesn't need the UI thread</param>
        public static void SaveRenderedImage(Image renderedBitmap, string fullPath, bool allowOverwrite,
            SurfaceOutputSettings outputSettings, bool copyPathToClipboard, SynchronizationContext uiContext = null)
        {
            // Check before the file is created, otherwise an empty file is left behind
            if (WellKnownFileFormats.IsGreenshotFormat(outputSettings.Format))
            {
                throw new NotSupportedException($"The greenshot format needs the surface, use {nameof(Save)} instead.");
            }

            fullPath = FilenameHelper.MakeFqFilenameSafe(fullPath);
            string path = Path.GetDirectoryName(fullPath);

            if (path != null)
            {
                DirectoryInfo di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    Directory.CreateDirectory(di.FullName);
                }
            }

            if (!allowOverwrite && File.Exists(fullPath))
            {
                ArgumentException throwingException = new ArgumentException("File '" + fullPath + "' already exists.");
                throwingException.Data.Add("fullPath", fullPath);
                throw throwingException;
            }

            Log.DebugFormat("Saving pre-rendered bitmap to {0}", fullPath);
            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                SaveToStream(renderedBitmap, null, stream, outputSettings);
            }

            if (copyPathToClipboard)
            {
                // The clipboard works on any thread
                ClipboardHelper.SetClipboardData(fullPath);
            }
        }

        /// <summary>
        /// Get the registered file format ID for a filename
        /// </summary>
        /// <param name="fullPath">filename (can be a complete path)</param>
        /// <returns>File format ID</returns>
        public static string FormatForFilename(string fullPath)
        {
            string extension = Path.GetExtension(fullPath)?.TrimStart('.');
            var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            var fallbackExtension = WellKnownFileFormats.Png;
            if (registry is null)
            {
                Log.WarnFormat("File format registry is not available, defaulting to ({0})", fallbackExtension);
                return fallbackExtension;
            }
            var formatId =  registry.GetByExtension(extension)?.Id;
            if (string.IsNullOrEmpty(formatId))
            {
                Log.WarnFormat("No file format registered for extension {0}, defaulting to ({1})", extension, fallbackExtension);
                return fallbackExtension;
            } 
            return formatId;
        }

        /// <summary>
        /// Remove a tmpfile which was created by SaveNamedTmpFile
        /// Used e.g. by the email export
        /// </summary>
        /// <param name="tmpfile"></param>
        /// <returns>true if it worked</returns>
        public static bool DeleteNamedTmpFile(string tmpfile)
        {
            Log.Debug("Deleting TMP File: " + tmpfile);
            try
            {
                if (File.Exists(tmpfile))
                {
                    File.Delete(tmpfile);
                    TmpFileCache.Remove(tmpfile);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Error deleting tmp file: ", ex);
            }

            return false;
        }

        /// <summary>
        /// Writes already encoded image bytes to a temp file, so an image which was encoded once doesn't need to be encoded again.
        /// The file is removed later, see RegisterTmpFile.
        /// </summary>
        /// <param name="encoded">MemoryStream with the encoded image, the position isn't changed</param>
        /// <param name="format">the format of the encoded bytes, used for the extension</param>
        /// <param name="destinationPath">directory, null for the temp directory</param>
        /// <returns>the path of the temp file, null when it couldn't be written</returns>
        public static string SaveEncodedToTmpFile(MemoryStream encoded, string format, string destinationPath)
        {
            string tmpPath = CreateTmpFilePath(format, destinationPath);
            try
            {
                using (var stream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write))
                {
                    encoded.WriteTo(stream);
                }
                RegisterTmpFile(tmpPath);
            }
            catch (Exception)
            {
                return null;
            }

            return tmpPath;
        }

        /// <summary>
        /// A path for a temp file with a random name and the extension of the format
        /// </summary>
        /// <param name="format">the file format, used for the extension</param>
        /// <param name="destinationPath">directory, null for the temp directory</param>
        /// <returns>the path, the file isn't created</returns>
        public static string CreateTmpFilePath(string format, string destinationPath)
        {
            string tmpFile = Path.GetRandomFileName() + FileFormatRegistry.GetPreferredExtensionWithDot(format);
            // Prevent problems with "other characters", which could cause problems
            tmpFile = Regex.Replace(tmpFile, @"[^\d\w\.]", string.Empty);
            string tmpPath = Path.Combine(destinationPath ?? Path.GetTempPath(), tmpFile);
            Log.Debug("Creating TMP File : " + tmpPath);
            return tmpPath;
        }

        /// <summary>
        /// Remember a temporary file, it is removed by RemoveTmpFiles (e.g. at exit).
        /// </summary>
        public static void RegisterTmpFile(string tmpFile)
        {
            if (!string.IsNullOrEmpty(tmpFile))
            {
                TmpFileCache.Add(tmpFile, tmpFile);
            }
        }

        /// <summary>
        /// Cleanup all created tmpfiles
        /// </summary>	
        public static void RemoveTmpFiles()
        {
            foreach (string tmpFile in TmpFileCache.Elements)
            {
                if (File.Exists(tmpFile))
                {
                    Log.DebugFormat("Removing old temp file {0}", tmpFile);
                    File.Delete(tmpFile);
                }

                TmpFileCache.Remove(tmpFile);
            }
        }

        /// <summary>
        /// Cleanup handler for expired tempfiles
        /// </summary>
        /// <param name="filekey"></param>
        /// <param name="filename"></param>
        private static void RemoveExpiredTmpFile(string filekey, object filename)
        {
            if (filename is string path && File.Exists(path))
            {
                Log.DebugFormat("Removing expired file {0}", path);
                File.Delete(path);
            }
        }

        /// <summary>
        /// Load an image from file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Image LoadImage(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            if (!File.Exists(filename))
            {
                return null;
            }

            Image fileImage;
            Log.InfoFormat("Loading image from file {0}", filename);
            // Fixed lock problem Bug #3431881
            using (Stream imageFileStream = File.OpenRead(filename))
            {
                fileImage = FromStream(imageFileStream, Path.GetExtension(filename));
            }

            if (fileImage != null)
            {
                Log.InfoFormat("Information about file {0}: {1}x{2}-{3} Resolution {4}x{5}", filename, fileImage.Width, fileImage.Height, fileImage.PixelFormat,
                    fileImage.HorizontalResolution, fileImage.VerticalResolution);
            }

            return fileImage;
        }

        /// <summary>
        /// Create an image from a stream, if an extension is supplied more formats are supported.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="extension"></param>
        /// <returns>Image</returns>
        public static Image FromStream(Stream stream, string extension = null)
        {
            if (stream == null)
            {
                return null;
            }

            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var startingPosition = stream.Position;
            // Make sure we can try multiple times
            if (!stream.CanSeek)
            {
                var memoryStream = RecyclableMemoryStreamFactory.GetStream("ImageIO.LoadImageFromStream");
                stream.CopyTo(memoryStream);
                stream = memoryStream;
                // As we are if a different stream, which starts at 0, change the starting position
                startingPosition = 0;
            }

            try
            {
                if (fileFormatHandlers.TryLoadFromStream(stream, extension, out var bitmap))
                {
                    return bitmap;
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

            return null;
        }

        /// <summary>
        /// Convert an image to a byte array and get the extension for the image format.
        /// An unknown format will be converted as PNG.
        /// </summary>
        /// <param name="myImage">The image to convert.</param>
        /// <returns>A tuple containing the byte array of the image and its file extension.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the image is null.</exception>
        public static (byte[] Data, string Extension) GetImageDataAndExtension(Image myImage)
        {
            if (myImage == null)
            {
                throw new ArgumentNullException(nameof(myImage));
            }

            // maps known formats to their extension, if the format is not known we default to png
            var (format, extension) = myImage.RawFormat switch
            {
                var f when f.Equals(ImageFormat.Jpeg) => (ImageFormat.Jpeg, "jpg"),
                var f when f.Equals(ImageFormat.Gif) => (ImageFormat.Gif, "gif"),
                var f when f.Equals(ImageFormat.Bmp) => (ImageFormat.Bmp, "bmp"),
                var f when f.Equals(ImageFormat.Tiff) => (ImageFormat.Tiff, "tiff"),
                var f when f.Equals(ImageFormat.Png) => (ImageFormat.Png, "png"),
                _ => (ImageFormat.Png, "png")
            };

            using var ms = new MemoryStream();
            myImage.Save(ms, format);
            return (ms.ToArray(), extension);
        }

        /// <summary>
        /// Converts the specified <see cref="Image"/> to a byte array in PNG format.
        /// </summary>
        public static byte[] ImageToPngByteArray(Image image)
        {
            if (image == null) return null;

            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, ImageFormat.Png);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Convert an icon to a byte array and get the extension for the icon format.
        /// </summary>
        /// <param name="icon">The icon to convert.</param>
        /// <returns>A tuple containing the byte array of the icon and its file extension.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the icon is null.</exception>
        public static (byte[] Data, string Extension) GetIconDataAndExtension(Icon icon)
        {
            if (icon == null)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            using var ms = new MemoryStream();
            icon.Save(ms);
            return (ms.ToArray(), "ico");
        }

        /// <summary>
        /// Converts a byte array into an <see cref="Image"/> object.
        /// </summary>
        public static Image ByteArrayToImage(byte[] byteArrayIn)
        {
            if (byteArrayIn == null || byteArrayIn.Length == 0) return null;
            using var ms = new MemoryStream(byteArrayIn);
            return Image.FromStream(ms);
        }

        /// <summary>
        /// Converts a byte array into an <see cref="Icon"/> object.
        /// </summary>
        public static Icon ByteArrayToIcon(byte[] byteArrayIn)
        {
            if (byteArrayIn == null || byteArrayIn.Length == 0) return null;
            using var ms = new MemoryStream(byteArrayIn);
            return new Icon(ms);
        }

    }
}