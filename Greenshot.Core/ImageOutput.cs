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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using Dapplo.Config.Ini;
using Dapplo.Log;
using Greenshot.Core.Configuration;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;
using Greenshot.Core.Utils;
using Encoder = System.Drawing.Imaging.Encoder;

#endregion

namespace Greenshot.Core
{
	/// <summary>
	///     Description of ImageOutput.
	/// </summary>
	public static class ImageOutput
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IOutputConfiguration OutputConfiguration = IniConfig.Current.GetSubSection<IOutputConfiguration>();
		private static readonly int PROPERTY_TAG_SOFTWARE_USED = 0x0131;
		private static readonly Cache<string, string> TmpFileCache = new Cache<string, string>(10*60*60, RemoveExpiredTmpFile);

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
				Log.Warn().WriteLine("Error creating a PropertyItem: {0}", e.Message);
			}
			return propertyItem;
		}

		/// <summary>
		/// Add a tmp-file, this will automatically be removed "later"
		/// </summary>
		/// <param name="tmpFile">string</param>
		public static void AddTmpFile(string tmpFile)
		{
			TmpFileCache.Add(tmpFile, tmpFile);
		}

		/// <summary>
		///     Remove a tmpfile which was created by SaveNamedTmpFile
		///     Used e.g. by the email export
		/// </summary>
		/// <param name="tmpfile"></param>
		/// <returns>true if it worked</returns>
		public static bool DeleteNamedTmpFile(string tmpfile)
		{
			Log.Debug().WriteLine("Deleting TMP File: {0}", tmpfile);
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
				Log.Warn().WriteLine(ex, "Error deleting tmp file: {0}", tmpfile);
			}
			return false;
		}

		/// <summary>
		///     Cleanup handler for expired tempfiles
		/// </summary>
		/// <param name="filekey"></param>
		/// <param name="filename"></param>
		private static void RemoveExpiredTmpFile(string filekey, object filename)
		{
			string path = filename as string;
			if ((path != null) && File.Exists(path))
			{
				Log.Debug().WriteLine("Removing expired file {0}", path);
				File.Delete(path);
			}
		}

		/// <summary>
		///     Cleanup all created tmpfiles
		/// </summary>
		public static void RemoveTmpFiles()
		{
			foreach (string tmpFile in TmpFileCache.Elements)
			{
				if (File.Exists(tmpFile))
				{
					Log.Debug().WriteLine("Removing old temp file {0}", tmpFile);
					File.Delete(tmpFile);
				}
				TmpFileCache.Remove(tmpFile);
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

			var imageSizes = new List<Size>();
			var encodedImages = new List<MemoryStream>();
			foreach (var image in images)
			{
				// Pick the best fit
				var sizes = new[] {16, 32, 48};
				int size = 256;
				foreach (var possibleSize in sizes)
				{
					if ((image.Width <= possibleSize) && (image.Height <= possibleSize))
					{
						size = possibleSize;
						break;
					}
				}
				var imageStream = new MemoryStream();
				if ((image.Width == size) && (image.Height == size))
				{
					using (var clonedImage = ImageHelper.Clone(image, PixelFormat.Format32bppArgb))
					{
						clonedImage.Save(imageStream, ImageFormat.Png);
						imageSizes.Add(new Size(size, size));
					}
				}
				else
				{
					// Resize to the specified size, first make sure the image is 32bpp
					using (var clonedImage = ImageHelper.Clone(image, PixelFormat.Format32bppArgb))
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

			var offset = iconDirSize + images.Count*iconDirEntrySize;
			for (int i = 0; i < images.Count; i++)
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
		///     Saves image to stream with specified quality
		///     To prevent problems with GDI version of before Windows 7:
		///     the stream is checked if it's seekable and if needed a MemoryStream as "cache" is used.
		/// </summary>
		/// <param name="imageToSave">image to save</param>
		/// <param name="capture">capture for the elements, needed if the greenshot format is used</param>
		/// <param name="stream">Stream to save to</param>
		/// <param name="outputSettings">SurfaceOutputSettings</param>
		public static void SaveToStream(Image imageToSave, ICapture capture, Stream stream, SurfaceOutputSettings outputSettings)
		{
			bool useMemoryStream = false;
			MemoryStream memoryStream = null;
			if ((outputSettings.Format == OutputFormat.greenshot) && (capture == null))
			{
				throw new ArgumentException("Surface needs to be se when using OutputFormat.Greenshot");
			}

			try
			{
				ImageFormat imageFormat;
				switch (outputSettings.Format)
				{
					case OutputFormat.bmp:
						imageFormat = ImageFormat.Bmp;
						break;
					case OutputFormat.gif:
						imageFormat = ImageFormat.Gif;
						break;
					case OutputFormat.jpg:
						imageFormat = ImageFormat.Jpeg;
						break;
					case OutputFormat.tiff:
						imageFormat = ImageFormat.Tiff;
						break;
					case OutputFormat.ico:
						imageFormat = ImageFormat.Icon;
						break;
					default:
						imageFormat = ImageFormat.Png;
						break;
				}
				// Problem with non-seekable streams most likely doesn't happen with Windows 7 (OS Version 6.1 and later)
				// http://stackoverflow.com/questions/8349260/generic-gdi-error-on-one-machine-but-not-the-other
				if (!stream.CanSeek)
				{
					useMemoryStream = true;
					Log.Warn().WriteLine("Using memorystream prevent an issue with saving to a non seekable stream.");
				}

				Log.Debug().WriteLine("Saving image to stream with Format {0} and PixelFormat {1}", imageFormat, imageToSave.PixelFormat);

				// Check if we want to use a memory stream, to prevent a issue which happens with Windows before "7".
				// The save is made to the targetStream, this is directed to either the MemoryStream or the original
				Stream targetStream = stream;
				if (useMemoryStream)
				{
					memoryStream = new MemoryStream();
					targetStream = memoryStream;
				}

				if (Equals(imageFormat, ImageFormat.Jpeg))
				{
					bool foundEncoder = false;
					foreach (ImageCodecInfo imageCodec in ImageCodecInfo.GetImageEncoders())
					{
						if (imageCodec.FormatID == imageFormat.Guid)
						{
							var parameters = new EncoderParameters(1);
							parameters.Param[0] = new EncoderParameter(Encoder.Quality, outputSettings.JPGQuality);
							// Removing transparency if it's not supported in the output
							if (Image.IsAlphaPixelFormat(imageToSave.PixelFormat))
							{
								Image nonAlphaImage = ImageHelper.Clone(imageToSave, PixelFormat.Format24bppRgb);
								AddTag(nonAlphaImage);
								nonAlphaImage.Save(targetStream, imageCodec, parameters);
								nonAlphaImage.Dispose();
								nonAlphaImage = null;
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
					var images = new List<Image>();
					images.Add(imageToSave);
					WriteIcon(stream, images);
				}
				else
				{
					bool needsDispose = false;
					// Removing transparency if it's not supported in the output
					if (!Equals(imageFormat, ImageFormat.Png) && Image.IsAlphaPixelFormat(imageToSave.PixelFormat))
					{
						imageToSave = ImageHelper.Clone(imageToSave, PixelFormat.Format24bppRgb);
						needsDispose = true;
					}
					AddTag(imageToSave);
					// Added for OptiPNG
					bool processed = false;
					if (Equals(imageFormat, ImageFormat.Png) && !string.IsNullOrEmpty(OutputConfiguration.OptimizePNGCommand))
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
						imageToSave = null;
					}
				}

				// If we used a memory stream, we need to stream the memory stream to the original stream.
				if (useMemoryStream)
				{
					memoryStream.WriteTo(stream);
				}

				// Output the surface elements, size and marker to the stream
				if (outputSettings.Format == OutputFormat.greenshot)
				{
					using (MemoryStream tmpStream = new MemoryStream())
					{
						long bytesWritten = capture.SaveElementsToStream(tmpStream);
						using (BinaryWriter writer = new BinaryWriter(tmpStream, Encoding.UTF8, true))
						{
							writer.Write(bytesWritten);
							Version v = Assembly.GetExecutingAssembly().GetName().Version;
							byte[] marker = Encoding.ASCII.GetBytes($"Greenshot{v.Major:00}.{v.Minor:00}");
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
			if (string.IsNullOrEmpty(OutputConfiguration.OptimizePNGCommand))
			{
				return false;
			}
			if (!File.Exists(OutputConfiguration.OptimizePNGCommand))
			{
				Log.Warn().WriteLine("Can't find 'OptimizePNGCommand' {0}", OutputConfiguration.OptimizePNGCommand);
				return false;
			}
			string tmpFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
			try
			{
				using (FileStream tmpStream = File.Create(tmpFileName))
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
					Log.Debug().WriteLine("Starting : {0}", OutputConfiguration.OptimizePNGCommand);
				}

				ProcessStartInfo processStartInfo = new ProcessStartInfo(OutputConfiguration.OptimizePNGCommand)
				{
					Arguments = string.Format(OutputConfiguration.OptimizePNGCommandArguments, tmpFileName),
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false
				};
				using (Process process = Process.Start(processStartInfo))
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
							byte[] processedImage = File.ReadAllBytes(tmpFileName);
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
				Log.Error().WriteLine("Error while processing PNG image: ", e);
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
		///     Add the greenshot property!
		/// </summary>
		/// <param name="imageToSave"></param>
		private static void AddTag(Image imageToSave)
		{
			// Create meta-data
			PropertyItem softwareUsedPropertyItem = CreatePropertyItem(PROPERTY_TAG_SOFTWARE_USED, "Greenshot");
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
		///     Get the OutputFormat for a filename
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
				Log.Warn().WriteLine(ae, "Couldn't parse extension: {0}", extension);
			}
			return format;
		}

		#endregion
	}
}