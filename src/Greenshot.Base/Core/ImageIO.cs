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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Greenshot.Base.Controls;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.IniFile;
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
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
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
                propertyItem = (PropertyItem) ci.Invoke(null);
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
            if (outputSettings.Format == OutputFormat.greenshot && surface == null)
            {
                throw new ArgumentException("Surface needs to be set when using OutputFormat.Greenshot");
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
                    memoryStream = new MemoryStream();
                    targetStream = memoryStream;
                }

                var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
                if (!fileFormatHandlers.TrySaveToStream(imageToSave as Bitmap, targetStream, outputSettings.Format.ToString(), surface, outputSettings))
                {
                    return;
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
            bool disposeImage = false;

            if (outputSettings.Format == OutputFormat.greenshot || outputSettings.SaveBackgroundOnly)
            {
                // We save the image of the surface, this should not be disposed
                imageToSave = surface.Image;
            }
            else
            {
                // We create the export image of the surface to save
                imageToSave = surface.GetImageForExport();
                disposeImage = true;
            }

            // The following block of modifications should be skipped when saving the greenshot format, no effects or otherwise!
            if (outputSettings.Format == OutputFormat.greenshot)
            {
                return disposeImage;
            }

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
                using var quantizer = new WuQuantizer((Bitmap) imageToSave);
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
        /// Load a Greenshot surface
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="returnSurface"></param>
        /// <returns></returns>
        public static ISurface LoadGreenshotSurface(string fullPath, ISurface returnSurface)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            Log.InfoFormat("Loading image from file {0}", fullPath);
            // Fixed lock problem Bug #3431881
            using (Stream surfaceFileStream = File.OpenRead(fullPath))
            {
                returnSurface = LoadGreenshotSurface(surfaceFileStream, returnSurface);
            }

            if (returnSurface != null)
            {
                Log.InfoFormat("Information about file {0}: {1}x{2}-{3} Resolution {4}x{5}", fullPath, returnSurface.Image.Width, returnSurface.Image.Height,
                    returnSurface.Image.PixelFormat, returnSurface.Image.HorizontalResolution, returnSurface.Image.VerticalResolution);
            }

            return returnSurface;
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
        /// Get the OutputFormat for a filename
        /// </summary>
        /// <param name="fullPath">filename (can be a complete path)</param>
        /// <returns>OutputFormat</returns>
        public static OutputFormat FormatForFilename(string fullPath)
        {
            // Fix for bug 2912959
            string extension = fullPath.Substring(fullPath.LastIndexOf(".", StringComparison.Ordinal) + 1);
            OutputFormat format = OutputFormat.png;
            try
            {
                format = (OutputFormat) Enum.Parse(typeof(OutputFormat), extension.ToLower());
            }
            catch (ArgumentException ae)
            {
                Log.Warn("Couldn't parse extension: " + extension, ae);
            }

            return format;
        }

        /// <summary>
        /// Save with showing a dialog
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        /// <returns>Path to filename</returns>
        public static string SaveWithDialog(ISurface surface, ICaptureDetails captureDetails)
        {
            string returnValue = null;
            using (SaveImageFileDialog saveImageFileDialog = new SaveImageFileDialog(captureDetails))
            {
                DialogResult dialogResult = saveImageFileDialog.ShowDialog();
                if (!dialogResult.Equals(DialogResult.OK)) return returnValue;
                try
                {
                    string fileNameWithExtension = saveImageFileDialog.FileNameWithExtension;
                    SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(FormatForFilename(fileNameWithExtension));
                    if (CoreConfig.OutputFilePromptQuality)
                    {
                        QualityDialog qualityDialog = new QualityDialog(outputSettings);
                        qualityDialog.ShowDialog();
                    }

                    // TODO: For now we always overwrite, should be changed
                    Save(surface, fileNameWithExtension, true, outputSettings, CoreConfig.OutputFileCopyPathToClipboard);
                    returnValue = fileNameWithExtension;
                    IniConfig.Save();
                }
                catch (ExternalException)
                {
                    MessageBox.Show(Language.GetFormattedString("error_nowriteaccess", saveImageFileDialog.FileName).Replace(@"\\", @"\"), Language.GetString("error"));
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Create a tmpfile which has the name like in the configured pattern.
        /// Used e.g. by the email export
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        /// <param name="outputSettings"></param>
        /// <returns>Path to image file</returns>
        public static string SaveNamedTmpFile(ISurface surface, ICaptureDetails captureDetails, SurfaceOutputSettings outputSettings)
        {
            string pattern = CoreConfig.OutputFileFilenamePattern;
            if (string.IsNullOrEmpty(pattern?.Trim()))
            {
                pattern = "greenshot ${capturetime}";
            }

            string filename = FilenameHelper.GetFilenameFromPattern(pattern, outputSettings.Format, captureDetails);
            // Prevent problems with "other characters", which causes a problem in e.g. Outlook 2007 or break our HTML
            filename = Regex.Replace(filename, @"[^\d\w\.]", "_");
            // Remove multiple "_"
            filename = Regex.Replace(filename, @"_+", "_");
            string tmpFile = Path.Combine(Path.GetTempPath(), filename);

            Log.Debug("Creating TMP File: " + tmpFile);

            // Catching any exception to prevent that the user can't write in the directory.
            // This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218
            try
            {
                Save(surface, tmpFile, true, outputSettings, false);
                TmpFileCache.Add(tmpFile, tmpFile);
            }
            catch (Exception e)
            {
                // Show the problem
                MessageBox.Show(e.Message, "Error");
                // when save failed we present a SaveWithDialog
                tmpFile = SaveWithDialog(surface, captureDetails);
            }

            return tmpFile;
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
        /// Helper method to create a temp image file
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="outputSettings"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public static string SaveToTmpFile(ISurface surface, SurfaceOutputSettings outputSettings, string destinationPath)
        {
            string tmpFile = Path.GetRandomFileName() + "." + outputSettings.Format;
            // Prevent problems with "other characters", which could cause problems
            tmpFile = Regex.Replace(tmpFile, @"[^\d\w\.]", string.Empty);
            if (destinationPath == null)
            {
                destinationPath = Path.GetTempPath();
            }

            string tmpPath = Path.Combine(destinationPath, tmpFile);
            Log.Debug("Creating TMP File : " + tmpPath);

            try
            {
                Save(surface, tmpPath, true, outputSettings, false);
                TmpFileCache.Add(tmpPath, tmpPath);
            }
            catch (Exception)
            {
                return null;
            }

            return tmpPath;
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

            if (!string.IsNullOrEmpty(extension))
            {
                extension = extension.Replace(".", string.Empty);
            }

            var startingPosition = stream.Position;

            // Make sure we can try multiple times
            if (!stream.CanSeek)
            {
                var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                stream = memoryStream;
                // As we are if a different stream, which starts at 0, change the starting position
                startingPosition = 0;
            }
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            foreach (var fileFormatHandler in fileFormatHandlers
                         .Where(ffh => ffh.Supports(FileFormatHandlerActions.LoadFromStream, extension))
                         .OrderBy(ffh => ffh.PriorityFor(FileFormatHandlerActions.LoadFromStream, extension)))
            {
                stream.Seek(startingPosition, SeekOrigin.Begin);
                if (fileFormatHandler.TryLoadFromStream(stream, extension, out var bitmap))
                {
                    return bitmap;
                }
            }

            return null;
        }

        /// <summary>
        /// Load a Greenshot surface from a stream
        /// </summary>
        /// <param name="surfaceFileStream">Stream</param>
        /// <param name="returnSurface"></param>
        /// <returns>ISurface</returns>
        public static ISurface LoadGreenshotSurface(Stream surfaceFileStream, ISurface returnSurface)
        {
            Image fileImage;
            // Fixed problem that the bitmap stream is disposed... by Cloning the image
            // This also ensures the bitmap is correctly created

            // We create a copy of the bitmap, so everything else can be disposed
            surfaceFileStream.Position = 0;
            using (Image tmpImage = Image.FromStream(surfaceFileStream, true, true))
            {
                Log.DebugFormat("Loaded .greenshot file with Size {0}x{1} and PixelFormat {2}", tmpImage.Width, tmpImage.Height, tmpImage.PixelFormat);
                fileImage = ImageHelper.Clone(tmpImage);
            }

            // Start at -14 read "GreenshotXX.YY" (XX=Major, YY=Minor)
            const int markerSize = 14;
            surfaceFileStream.Seek(-markerSize, SeekOrigin.End);
            using (StreamReader streamReader = new StreamReader(surfaceFileStream))
            {
                var greenshotMarker = streamReader.ReadToEnd();
                if (!greenshotMarker.StartsWith("Greenshot"))
                {
                    throw new ArgumentException("Stream is not a Greenshot file!");
                }

                Log.InfoFormat("Greenshot file format: {0}", greenshotMarker);
                const int filesizeLocation = 8 + markerSize;
                surfaceFileStream.Seek(-filesizeLocation, SeekOrigin.End);
                using BinaryReader reader = new BinaryReader(surfaceFileStream);
                long bytesWritten = reader.ReadInt64();
                surfaceFileStream.Seek(-(bytesWritten + filesizeLocation), SeekOrigin.End);
                returnSurface.LoadElementsFromStream(surfaceFileStream);
            }

            if (fileImage != null)
            {
                returnSurface.Image = fileImage;
                Log.InfoFormat("Information about .greenshot file: {0}x{1}-{2} Resolution {3}x{4}", fileImage.Width, fileImage.Height, fileImage.PixelFormat,
                    fileImage.HorizontalResolution, fileImage.VerticalResolution);
            }

            return returnSurface;
        }
    }
}