#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Encoder = System.Drawing.Imaging.Encoder;

#endregion

namespace GreenshotPlugin.Gfx
{
	/// <summary>
	///     Description of ImageOutput.
	/// </summary>
	public static class ImageOutput
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly int PROPERTY_TAG_SOFTWARE_USED = 0x0131;
		private static readonly Cache<string, string> TmpFileCache = new Cache<string, string>(10 * 60 * 60, RemoveExpiredTmpFile);

		/// <summary>
		///     Creates a PropertyItem (Metadata) to store with the image.
		///     For the possible ID's see:
		///     http://msdn.microsoft.com/de-de/library/system.drawing.imaging.propertyitem.id(v=vs.80).aspx
		///     This code uses Reflection to create a PropertyItem, although it's not adviced it's not as stupid as having a image
		///     in the project so we can read a PropertyItem from that!
		/// </summary>
		/// <param name="id">ID</param>
		/// <param name="text">Text</param>
		/// <returns></returns>
		private static PropertyItem CreatePropertyItem(int id, string text)
		{
			PropertyItem propertyItem = null;
			try
			{
				var ci = typeof(PropertyItem).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, new Type[] {}, null);
				propertyItem = (PropertyItem) ci.Invoke(null);
				// Make sure it's of type string
				propertyItem.Type = 2;
				// Set the ID
				propertyItem.Id = id;
				// Set the text
				var byteString = Encoding.ASCII.GetBytes(text + " ");
				// Set Zero byte for String end.
				byteString[byteString.Length - 1] = 0;
				propertyItem.Value = byteString;
				propertyItem.Len = text.Length + 1;
			}
			catch (Exception e)
			{
				Log.Warn().WriteLine("Error creating a PropertyItem: {0}", e.Message);
			}
			return propertyItem;
		}

		#region save-as

		/// <summary>
		///     Save with showing a dialog
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>Path to filename</returns>
		public static string SaveWithDialog(ISurface surface, ICaptureDetails captureDetails)
		{
			string returnValue = null;
			using (var saveImageFileDialog = new SaveImageFileDialog(captureDetails))
			{
				var dialogResult = saveImageFileDialog.ShowDialog();
				if (dialogResult.Equals(DialogResult.OK))
				{
					try
					{
						var fileNameWithExtension = saveImageFileDialog.FileNameWithExtension;
						var outputSettings = new SurfaceOutputSettings(FormatForFilename(fileNameWithExtension));
						if (CoreConfig.OutputFilePromptQuality)
						{
							var qualityDialog = new QualityDialog(outputSettings);
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
			}
			return returnValue;
		}

		#endregion

		/// <summary>
		///     Create a tmpfile which has the name like in the configured pattern.
		///     Used e.g. by the email export
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <param name="outputSettings"></param>
		/// <returns>Path to image file</returns>
		public static string SaveNamedTmpFile(ISurface surface, ICaptureDetails captureDetails, SurfaceOutputSettings outputSettings)
		{
			var pattern = CoreConfig.OutputFileFilenamePattern;
			if (string.IsNullOrEmpty(pattern?.Trim()))
			{
				pattern = "greenshot ${capturetime}";
			}
			var filename = FilenameHelper.GetFilenameFromPattern(pattern, outputSettings.Format, captureDetails);
			// Prevent problems with "other characters", which causes a problem in e.g. Outlook 2007 or break our HTML
			filename = Regex.Replace(filename, @"[^\d\w\.]", "_");
			// Remove multiple "_"
			filename = Regex.Replace(filename, @"_+", "_");
			var tmpFile = Path.Combine(Path.GetTempPath(), filename);

			Log.Debug().WriteLine("Creating TMP File: " + tmpFile);

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
		///     Remove a tmpfile which was created by SaveNamedTmpFile
		///     Used e.g. by the email export
		/// </summary>
		/// <param name="tmpfile"></param>
		/// <returns>true if it worked</returns>
		public static bool DeleteNamedTmpFile(string tmpfile)
		{
			Log.Debug().WriteLine("Deleting TMP File: " + tmpfile);
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
				Log.Warn().WriteLine(ex, "Error deleting tmp file: ");
			}
			return false;
		}

		/// <summary>
		///     Helper method to create a temp image file
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="outputSettings"></param>
		/// <param name="destinationPath"></param>
		/// <returns></returns>
		public static string SaveToTmpFile(ISurface surface, SurfaceOutputSettings outputSettings, string destinationPath)
		{
			var tmpFile = Path.GetRandomFileName() + "." + outputSettings.Format;
			// Prevent problems with "other characters", which could cause problems
			tmpFile = Regex.Replace(tmpFile, @"[^\d\w\.]", "");
			if (destinationPath == null)
			{
				destinationPath = Path.GetTempPath();
			}
			var tmpPath = Path.Combine(destinationPath, tmpFile);
			Log.Debug().WriteLine("Creating TMP File : " + tmpPath);

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
		///     Cleanup all created tmpfiles
		/// </summary>
		public static void RemoveTmpFiles()
		{
			foreach (var tmpFile in TmpFileCache.Elements)
			{
				if (File.Exists(tmpFile))
				{
					Log.Debug().WriteLine("Removing old temp file {0}", tmpFile);
					File.Delete(tmpFile);
				}
				TmpFileCache.Remove(tmpFile);
			}
		}

		/// <summary>
		///     Cleanup handler for expired tempfiles
		/// </summary>
		/// <param name="filekey"></param>
		/// <param name="filename"></param>
		private static void RemoveExpiredTmpFile(string filekey, object filename)
		{
			var path = filename as string;
			if (path != null && File.Exists(path))
			{
				Log.Debug().WriteLine("Removing expired file {0}", path);
				File.Delete(path);
			}
		}

		#region Icon

		/// <summary>
		///     Write the images to the stream as icon
		///     Every image is resized to 256x256 (but the content maintains the aspect ratio)
		/// </summary>
		/// <param name="stream">Stream to write to</param>
		/// <param name="images">List of images</param>
		public static void WriteIcon(Stream stream, IList<Image> images)
		{
			var binaryWriter = new BinaryWriter(stream);
			//
			// ICONDIR structure
			//
			binaryWriter.Write((short) 0); // reserved
			binaryWriter.Write((short) 1); // image type (icon)
			binaryWriter.Write((short) images.Count); // number of images

			IList<Size> imageSizes = new List<Size>();
			IList<MemoryStream> encodedImages = new List<MemoryStream>();
			foreach (var image in images)
			{
				// Pick the best fit
				var sizes = new[] {16, 32, 48};
				var size = 256;
				foreach (var possibleSize in sizes)
				{
					if (image.Width <= possibleSize && image.Height <= possibleSize)
					{
						size = possibleSize;
						break;
					}
				}
				var imageStream = new MemoryStream();
				if (image.Width == size && image.Height == size)
				{
					using (var clonedImage = image.CloneImage(PixelFormat.Format32bppArgb))
					{
						clonedImage.Save(imageStream, ImageFormat.Png);
						imageSizes.Add(new Size(size, size));
					}
				}
				else
				{
					// Resize to the specified size, first make sure the image is 32bpp
					using (var clonedImage = image.CloneImage(PixelFormat.Format32bppArgb))
					{
						using (var resizedImage = ImageHelper.ResizeImage(clonedImage, true, true, Color.Empty, size, size, null))
						{
							resizedImage.Save(imageStream, ImageFormat.Png);
							imageSizes.Add(resizedImage.Size);
						}
					}
				}

				imageStream.Seek(0, SeekOrigin.Begin);
				encodedImages.Add(imageStream);
			}

			//
			// ICONDIRENTRY structure
			//
			const int iconDirSize = 6;
			const int iconDirEntrySize = 16;

			var offset = iconDirSize + images.Count * iconDirEntrySize;
			for (var i = 0; i < images.Count; i++)
			{
				var imageSize = imageSizes[i];
				// Write the width / height, 0 means 256
				binaryWriter.Write(imageSize.Width == 256 ? (byte) 0 : (byte) imageSize.Width);
				binaryWriter.Write(imageSize.Height == 256 ? (byte) 0 : (byte) imageSize.Height);
				binaryWriter.Write((byte) 0); // no pallete
				binaryWriter.Write((byte) 0); // reserved
				binaryWriter.Write((short) 0); // no color planes
				binaryWriter.Write((short) 32); // 32 bpp
				binaryWriter.Write((int) encodedImages[i].Length); // image data length
				binaryWriter.Write(offset);
				offset += (int) encodedImages[i].Length;
			}

			binaryWriter.Flush();
			//
			// Write image data
			//
			foreach (var encodedImage in encodedImages)
			{
				encodedImage.WriteTo(stream);
				encodedImage.Dispose();
			}
		}

		#endregion

		#region save

		/// <summary>
		///     Saves ISurface to stream with specified output settings
		/// </summary>
		/// <param name="surface">ISurface to save</param>
		/// <param name="stream">Stream to save to</param>
		/// <param name="outputSettings">SurfaceOutputSettings</param>
		public static void SaveToStream(ISurface surface, Stream stream, SurfaceOutputSettings outputSettings)
		{
			Image imageToSave;
			var disposeImage = CreateImageFromSurface(surface, outputSettings, out imageToSave);
			SaveToStream(imageToSave, surface, stream, outputSettings);
			// cleanup if needed
			if (disposeImage)
			{
				imageToSave?.Dispose();
			}
		}

		/// <summary>
		///     Saves image to stream with specified quality
		///     To prevent problems with GDI version of before Windows 7:
		///     the stream is checked if it's seekable and if needed a MemoryStream as "cache" is used.
		/// </summary>
		/// <param name="imageToSave">image to save</param>
		/// <param name="surface">surface for the elements, needed if the greenshot format is used</param>
		/// <param name="stream">Stream to save to</param>
		/// <param name="outputSettings">SurfaceOutputSettings</param>
		public static void SaveToStream(Image imageToSave, ISurface surface, Stream stream, SurfaceOutputSettings outputSettings)
		{
			var useMemoryStream = false;
			MemoryStream memoryStream = null;
			if (outputSettings.Format == OutputFormats.greenshot && surface == null)
			{
				throw new ArgumentException("Surface needs to be set when using OutputFormats.Greenshot");
			}

			try
			{
				ImageFormat imageFormat;
				switch (outputSettings.Format)
				{
					case OutputFormats.bmp:
						imageFormat = ImageFormat.Bmp;
						break;
					case OutputFormats.gif:
						imageFormat = ImageFormat.Gif;
						break;
					case OutputFormats.jpg:
						imageFormat = ImageFormat.Jpeg;
						break;
					case OutputFormats.tiff:
						imageFormat = ImageFormat.Tiff;
						break;
					case OutputFormats.ico:
						imageFormat = ImageFormat.Icon;
						break;
					default:
						// Problem with non-seekable streams most likely doesn't happen with Windows 7 (OS Version 6.1 and later)
						// http://stackoverflow.com/questions/8349260/generic-gdi-error-on-one-machine-but-not-the-other
						if (!stream.CanSeek)
						{
							if (!WindowsVersion.IsWindows7OrLater)
							{
								useMemoryStream = true;
								Log.Warn().WriteLine("Using memorystream prevent an issue with saving to a non seekable stream.");
							}
						}
						imageFormat = ImageFormat.Png;
						break;
				}
				Log.Debug().WriteLine("Saving image to stream with Format {0} and PixelFormat {1}", imageFormat, imageToSave.PixelFormat);

				// Check if we want to use a memory stream, to prevent a issue which happens with Windows before "7".
				// The save is made to the targetStream, this is directed to either the MemoryStream or the original
				var targetStream = stream;
				if (useMemoryStream)
				{
					memoryStream = new MemoryStream();
					targetStream = memoryStream;
				}

				if (Equals(imageFormat, ImageFormat.Jpeg))
				{
					var foundEncoder = false;
					foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
					{
						if (imageCodec.FormatID == imageFormat.Guid)
						{
							var parameters = new EncoderParameters(1)
							{
								Param = {[0] = new EncoderParameter(Encoder.Quality, outputSettings.JPGQuality)}
							};
							// Removing transparency if it's not supported in the output
							if (Image.IsAlphaPixelFormat(imageToSave.PixelFormat))
							{
								var nonAlphaImage = imageToSave.CloneImage(PixelFormat.Format24bppRgb);
								AddTag(nonAlphaImage);
								nonAlphaImage.Save(targetStream, imageCodec, parameters);
								nonAlphaImage.Dispose();
							}
							else
							{
								AddTag(imageToSave);
								imageToSave.Save(targetStream, imageCodec, parameters);
							}
							foundEncoder = true;
							break;
						}
					}
					if (!foundEncoder)
					{
						throw new ApplicationException("No JPG encoder found, this should not happen.");
					}
				}
				else if (Equals(imageFormat, ImageFormat.Icon))
				{
					// FEATURE-916: Added Icon support
					IList<Image> images = new List<Image>();
					images.Add(imageToSave);
					WriteIcon(stream, images);
				}
				else
				{
					var needsDispose = false;
					// Removing transparency if it's not supported in the output
					if (!Equals(imageFormat, ImageFormat.Png) && Image.IsAlphaPixelFormat(imageToSave.PixelFormat))
					{
						imageToSave = imageToSave.CloneImage(PixelFormat.Format24bppRgb);
						needsDispose = true;
					}
					AddTag(imageToSave);
					// Added for OptiPNG
					var processed = false;
					if (Equals(imageFormat, ImageFormat.Png) && !string.IsNullOrEmpty(CoreConfig.OptimizePNGCommand))
					{
						processed = ProcessPngImageExternally(imageToSave, targetStream);
					}
					if (!processed)
					{
						imageToSave.Save(targetStream, imageFormat);
					}
					if (needsDispose)
					{
						imageToSave.Dispose();
					}
				}

				// If we used a memory stream, we need to stream the memory stream to the original stream.
				if (useMemoryStream)
				{
					memoryStream.WriteTo(stream);
				}

				// Output the surface elements, size and marker to the stream
				if (outputSettings.Format == OutputFormats.greenshot)
				{
					using (var tmpStream = new MemoryStream())
					{
						var bytesWritten = surface.SaveElementsToStream(tmpStream);
						using (var writer = new BinaryWriter(tmpStream))
						{
							writer.Write(bytesWritten);
							var v = Assembly.GetExecutingAssembly().GetName().Version;
							var marker = Encoding.ASCII.GetBytes($"Greenshot{v.Major:00}.{v.Minor:00}");
							writer.Write(marker);
							tmpStream.WriteTo(stream);
						}
					}
				}
			}
			finally
			{
				memoryStream?.Dispose();
			}
		}

		/// <summary>
		///     Write the passed Image to a tmp-file and call an external process, than read the file back and write it to the
		///     targetStream
		/// </summary>
		/// <param name="imageToProcess">Image to pass to the external process</param>
		/// <param name="targetStream">stream to write the processed image to</param>
		/// <returns></returns>
		private static bool ProcessPngImageExternally(Image imageToProcess, Stream targetStream)
		{
			if (string.IsNullOrEmpty(CoreConfig.OptimizePNGCommand))
			{
				return false;
			}
			if (!File.Exists(CoreConfig.OptimizePNGCommand))
			{
				Log.Warn().WriteLine("Can't find 'OptimizePNGCommand' {0}", CoreConfig.OptimizePNGCommand);
				return false;
			}
			var tmpFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
			try
			{
				using (var tmpStream = File.Create(tmpFileName))
				{
					Log.Debug().WriteLine("Writing png to tmp file: {0}", tmpFileName);
					imageToProcess.Save(tmpStream, ImageFormat.Png);
					if (Log.IsDebugEnabled())
					{
						Log.Debug().WriteLine("File size before processing {0}", new FileInfo(tmpFileName).Length);
					}
				}
				if (Log.IsDebugEnabled())
				{
					Log.Debug().WriteLine("Starting : {0}", CoreConfig.OptimizePNGCommand);
				}

				var processStartInfo = new ProcessStartInfo(CoreConfig.OptimizePNGCommand)
				{
					Arguments = string.Format(CoreConfig.OptimizePNGCommandArguments, tmpFileName),
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false
				};
				using (var process = Process.Start(processStartInfo))
				{
					if (process != null)
					{
						process.WaitForExit();
						if (process.ExitCode == 0)
						{
							if (Log.IsDebugEnabled())
							{
								Log.Debug().WriteLine("File size after processing {0}", new FileInfo(tmpFileName).Length);
								Log.Debug().WriteLine("Reading back tmp file: {0}", tmpFileName);
							}
							var processedImage = File.ReadAllBytes(tmpFileName);
							targetStream.Write(processedImage, 0, processedImage.Length);
							return true;
						}
						Log.Error().WriteLine("Error while processing PNG image: {0}", process.ExitCode);
						Log.Error().WriteLine("Output: {0}", process.StandardOutput.ReadToEnd());
						Log.Error().WriteLine("Error: {0}", process.StandardError.ReadToEnd());
					}
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Error while processing PNG image: ");
			}
			finally
			{
				if (File.Exists(tmpFileName))
				{
					Log.Debug().WriteLine("Cleaning up tmp file: {0}", tmpFileName);
					File.Delete(tmpFileName);
				}
			}
			return false;
		}

		/// <summary>
		///     Create an image from a surface with the settings from the output settings applied
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="outputSettings"></param>
		/// <param name="imageToSave"></param>
		/// <returns>true if the image must be disposed</returns>
		public static bool CreateImageFromSurface(ISurface surface, SurfaceOutputSettings outputSettings, out Image imageToSave)
		{
			var disposeImage = false;

			if (outputSettings.Format == OutputFormats.greenshot || outputSettings.SaveBackgroundOnly)
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
			if (outputSettings.Format == OutputFormats.greenshot)
			{
				return disposeImage;
			}
			Image tmpImage;
			if (outputSettings.Effects != null && outputSettings.Effects.Count > 0)
			{
				// apply effects, if there are any
				using (var matrix = new Matrix())
				{
					tmpImage = imageToSave.ApplyEffects(outputSettings.Effects, matrix);
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
			if (outputSettings.DisableReduceColors || !CoreConfig.OutputFileAutoReduceColors && !outputSettings.ReduceColors)
			{
				return disposeImage;
			}
			var isAlpha = Image.IsAlphaPixelFormat(imageToSave.PixelFormat);
			if (outputSettings.ReduceColors || !isAlpha && CoreConfig.OutputFileAutoReduceColors)
			{
				using (var quantizer = new WuQuantizer((Bitmap) imageToSave))
				{
					var colorCount = quantizer.GetColorCount();
					Log.Info().WriteLine("Image with format {0} has {1} colors", imageToSave.PixelFormat, colorCount);
					if (!outputSettings.ReduceColors && colorCount >= 256)
					{
						return disposeImage;
					}
					try
					{
						Log.Info().WriteLine("Reducing colors on bitmap to 256.");
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
						Log.Warn().WriteLine(e, "Error occurred while Quantizing the image, ignoring and using original. Error: ");
					}
				}
			}
			else if (isAlpha && !outputSettings.ReduceColors)
			{
				Log.Info().WriteLine("Skipping 'optional' color reduction as the image has alpha");
			}
			return disposeImage;
		}

		/// <summary>
		///     Add the greenshot property!
		/// </summary>
		/// <param name="imageToSave"></param>
		private static void AddTag(Image imageToSave)
		{
			// Create meta-data
			var softwareUsedPropertyItem = CreatePropertyItem(PROPERTY_TAG_SOFTWARE_USED, "Greenshot");
			if (softwareUsedPropertyItem != null)
			{
				try
				{
					imageToSave.SetPropertyItem(softwareUsedPropertyItem);
				}
				catch (Exception)
				{
					Log.Warn().WriteLine("Couldn't set property {0}", softwareUsedPropertyItem.Id);
				}
			}
		}

		/// <summary>
		///     Load a Greenshot surface
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
			Log.Info().WriteLine("Loading image from file {0}", fullPath);
			// Fixed lock problem Bug #3431881
			using (Stream surfaceFileStream = File.OpenRead(fullPath))
			{
				returnSurface = ImageHelper.LoadGreenshotSurface(surfaceFileStream, returnSurface);
			}
			if (returnSurface != null)
			{
				Log.Info().WriteLine("Information about file {0}: {1}x{2}-{3} Resolution {4}x{5}", fullPath, returnSurface.Image.Width, returnSurface.Image.Height,
					returnSurface.Image.PixelFormat, returnSurface.Image.HorizontalResolution, returnSurface.Image.VerticalResolution);
			}
			return returnSurface;
		}

		/// <summary>
		///     Saves image to specific path with specified quality
		/// </summary>
		public static void Save(ISurface surface, string fullPath, bool allowOverwrite, SurfaceOutputSettings outputSettings, bool copyPathToClipboard)
		{
			fullPath = FilenameHelper.MakeFqFilenameSafe(fullPath);
			var path = Path.GetDirectoryName(fullPath);

			// check whether path exists - if not create it
			if (path != null)
			{
				var di = new DirectoryInfo(path);
				if (!di.Exists)
				{
					Directory.CreateDirectory(di.FullName);
				}
			}

			if (!allowOverwrite && File.Exists(fullPath))
			{
				var throwingException = new ArgumentException("File '" + fullPath + "' already exists.");
				throwingException.Data.Add("fullPath", fullPath);
				throw throwingException;
			}
			Log.Debug().WriteLine("Saving surface to {0}", fullPath);
			// Create the stream and call SaveToStream
			using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
			{
				SaveToStream(surface, stream, outputSettings);
			}

			if (copyPathToClipboard)
			{
				ClipboardHelper.SetClipboardData(fullPath);
			}
		}

		/// <summary>
		///     Get the OutputFormats for a filename
		/// </summary>
		/// <param name="fullPath">filename (can be a complete path)</param>
		/// <returns>OutputFormats</returns>
		public static OutputFormats FormatForFilename(string fullPath)
		{
			// Fix for bug 2912959
			var extension = fullPath.Substring(fullPath.LastIndexOf(".", StringComparison.Ordinal) + 1);
			var format = OutputFormats.png;
			try
			{
				format = (OutputFormats) Enum.Parse(typeof(OutputFormats), extension.ToLower());
			}
			catch (ArgumentException ae)
			{
				Log.Warn().WriteLine(ae, "Couldn't parse extension: " + extension);
			}
			return format;
		}

		#endregion
	}
}