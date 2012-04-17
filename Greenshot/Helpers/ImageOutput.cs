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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of ImageOutput.
	/// </summary>
	public static class ImageOutput {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImageOutput));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly int PROPERTY_TAG_SOFTWARE_USED = 0x0131;
		private static CacheHelper<string> tmpFileCache = new CacheHelper<string>("tmpfile", 60*60*10, new CacheObjectExpired(RemoveExpiredTmpFile));
		
		/// <summary>
		/// Creates a PropertyItem (Metadata) to store with the image.
		/// For the possible ID's see: http://msdn.microsoft.com/de-de/library/system.drawing.imaging.propertyitem.id(v=vs.80).aspx
		/// This code uses Reflection to create a PropertyItem, although it's not adviced it's not as stupid as having a image in the project so we can read a PropertyItem from that!
		/// </summary>
		/// <param name="id">ID</param>
		/// <param name="text">Text</param>
		/// <returns></returns>
		private static PropertyItem CreatePropertyItem(int id, string text) {
			PropertyItem propertyItem = null;
			try {
				ConstructorInfo ci = typeof(PropertyItem).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public , null, new Type[] { }, null);
				propertyItem = (PropertyItem)ci.Invoke(null);
				// Make sure it's of type string
				propertyItem.Type =2;
				// Set the ID
				propertyItem.Id = id;
				// Set the text
				byte [] byteString = System.Text.ASCIIEncoding.ASCII.GetBytes(text + " ");
				// Set Zero byte for String end.
				byteString[byteString.Length-1] = 0;
				propertyItem.Value = byteString;
				propertyItem.Len = text.Length + 1;
			} catch (Exception e) {
				LOG.WarnFormat("Error creating a PropertyItem: {0}", e.Message);
			}
			return propertyItem;
		}
		#region save

		/// <summary>
		/// Saves image to stream with specified quality
		/// </summary>
		public static void SaveToStream(Image imageToSave, Stream stream, OutputFormat extension, int quality, bool reduceColors) {
			ImageFormat imageFormat = null;
			bool disposeImage = false;

			switch (extension) {
				case OutputFormat.bmp:
					imageFormat = ImageFormat.Bmp;
					break;
				case OutputFormat.gif:
					imageFormat = ImageFormat.Gif;
					break;
				case OutputFormat.jpg:
					imageFormat = ImageFormat.Jpeg;
					break;
				case OutputFormat.png:
					imageFormat = ImageFormat.Png;
					break;
				case OutputFormat.tiff:
					imageFormat = ImageFormat.Tiff;
					break;
				default:
					imageFormat = ImageFormat.Png;
					break;
			}

			// Fix for Bug #3468436, force quantizing when output format is gif as this has only 256 colors!
			if (ImageFormat.Gif.Equals(imageFormat)) {
				reduceColors = true;
			}

			// Removing transparency if it's not supported
			if (imageFormat != ImageFormat.Png) {
				imageToSave = ImageHelper.Clone(imageToSave, PixelFormat.Format24bppRgb);
				disposeImage = true;
			}

			// check for color reduction, forced or automatically
			if (conf.OutputFileAutoReduceColors || reduceColors) {
				WuQuantizer quantizer = new WuQuantizer((Bitmap)imageToSave);
				int colorCount = quantizer.GetColorCount();
				LOG.InfoFormat("Image with format {0} has {1} colors", imageToSave.PixelFormat, colorCount);
				if (reduceColors || colorCount < 256) {
					try {
						LOG.Info("Reducing colors on bitmap to 255.");
						Image tmpImage = quantizer.GetQuantizedImage(255);
						if (disposeImage) {
							imageToSave.Dispose();
						}
						imageToSave = tmpImage;
						// Make sure the "new" image is disposed
						disposeImage = true;
					} catch (Exception e) {
						LOG.Warn("Error occurred while Quantizing the image, ignoring and using original. Error: ", e);
					}
				}
			} else {
				LOG.Info("Skipping color reduction test, OutputFileAutoReduceColors is set to false.");
			}

			try {
				// Create meta-data
				PropertyItem softwareUsedPropertyItem = CreatePropertyItem(PROPERTY_TAG_SOFTWARE_USED, "Greenshot");
				if (softwareUsedPropertyItem != null) {
					try {
						imageToSave.SetPropertyItem(softwareUsedPropertyItem);
					} catch (ArgumentException) {
						LOG.WarnFormat("Image of type {0} do not support property {1}", imageFormat, softwareUsedPropertyItem.Id);
					}
				}
				LOG.DebugFormat("Saving image to stream with Format {0} and PixelFormat {1}", imageFormat, imageToSave.PixelFormat);
				if (imageFormat == ImageFormat.Jpeg) {
					EncoderParameters parameters = new EncoderParameters(1);
					parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(Encoder.Quality, quality);
					ImageCodecInfo[] ies = ImageCodecInfo.GetImageEncoders();
					imageToSave.Save(stream, ies[1], parameters);
				} else if (imageFormat != ImageFormat.Png && Image.IsAlphaPixelFormat(imageToSave.PixelFormat)) {
					// No transparency in target format
					using (Bitmap tmpBitmap = ImageHelper.Clone(imageToSave, PixelFormat.Format24bppRgb)) {
						tmpBitmap.Save(stream, imageFormat);
					}
				} else {
					imageToSave.Save(stream, imageFormat);
				}
			} finally {
				// cleanup if needed
				if (disposeImage && imageToSave != null) {
					imageToSave.Dispose();
				}
			}
		}
		
		/// <summary>
		/// Saves image to specific path with specified quality
		/// </summary>
		public static void Save(Image image, string fullPath, bool allowOverwrite, int quality, bool reduceColors, bool copyPathToClipboard) {
			fullPath = FilenameHelper.MakeFQFilenameSafe(fullPath);
			string path = Path.GetDirectoryName(fullPath);

			// check whether path exists - if not create it
			DirectoryInfo di = new DirectoryInfo(path);
			if (!di.Exists) {
				Directory.CreateDirectory(di.FullName);
			}
			string extension = Path.GetExtension(fullPath);
			if (extension != null && extension.StartsWith(".")) {
				extension = extension.Substring(1);
			}
			OutputFormat format = OutputFormat.png;
			try {
				if (extension != null) {
					format = (OutputFormat)Enum.Parse(typeof(OutputFormat), extension.ToLower());
				}
			} catch(ArgumentException ae) {
				LOG.Warn("Couldn't parse extension: " + extension, ae);
			}
			if (!allowOverwrite && File.Exists(fullPath)) {
				ArgumentException throwingException = new ArgumentException("File '" + fullPath + "' already exists.");
				throwingException.Data.Add("fullPath", fullPath);
				throw throwingException;
			}
			LOG.DebugFormat("Saving image to {0}", fullPath);
			// Create the stream and call SaveToStream
			using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write)) {
				SaveToStream(image, stream, format, quality, reduceColors);
			}

			if (copyPathToClipboard) {
				ClipboardHelper.SetClipboardData(fullPath);
			}
		}

		/// <summary>
		/// saves img to fullpath
		/// </summary>
		/// <param name="img">the image to save</param>
		/// <param name="fullPath">the absolute destination path including file name</param>
		/// <param name="allowOverwrite">true if overwrite is allowed, false if not</param>
		public static void Save(Image img, string fullPath, bool allowOverwrite) {
			int quality;
			bool reduceColors = false;
			
			// Fix for bug 2912959
			string extension = fullPath.Substring(fullPath.LastIndexOf(".") + 1);
			bool isJPG = false;
			if (extension != null) {
				isJPG = "JPG".Equals(extension.ToUpper()) || "JPEG".Equals(extension.ToUpper());
			}
			
			if(conf.OutputFilePromptQuality) {
				QualityDialog qualityDialog = new QualityDialog(isJPG);
				qualityDialog.ShowDialog();
				quality = qualityDialog.Quality;
				reduceColors = qualityDialog.ReduceColors;
			} else {
				quality = conf.OutputFileJpegQuality;
				reduceColors = conf.OutputFileReduceColors;
			}
			Save(img, fullPath, allowOverwrite, quality, reduceColors, conf.OutputFileCopyPathToClipboard);
		}
		#endregion
		
		#region save-as
		public static string SaveWithDialog(Image image) {
			return SaveWithDialog(image, null);
		}

		public static string SaveWithDialog(Image image, ICaptureDetails captureDetails) {
			string returnValue = null;
			SaveImageFileDialog saveImageFileDialog = new SaveImageFileDialog(captureDetails);
			DialogResult dialogResult = saveImageFileDialog.ShowDialog();
			if (dialogResult.Equals(DialogResult.OK)) {
				try {
					string fileNameWithExtension = saveImageFileDialog.FileNameWithExtension;
					// TODO: For now we overwrite, should be changed
					ImageOutput.Save(image, fileNameWithExtension, true);
					returnValue = fileNameWithExtension;
					conf.OutputFileAsFullpath = fileNameWithExtension;
					IniConfig.Save();
				} catch(System.Runtime.InteropServices.ExternalException) {
					MessageBox.Show(Language.GetFormattedString(LangKey.error_nowriteaccess,saveImageFileDialog.FileName).Replace(@"\\",@"\"), Language.GetString(LangKey.error));
				}
			}
			return returnValue;
		}
		#endregion

		public static string SaveNamedTmpFile(Image image, ICaptureDetails captureDetails, OutputFormat outputFormat, int quality, bool reduceColors) {
			string pattern = conf.OutputFileFilenamePattern;
			if (pattern == null || string.IsNullOrEmpty(pattern.Trim())) {
				pattern = "greenshot ${capturetime}";
			}
			string filename = FilenameHelper.GetFilenameFromPattern(pattern, outputFormat, captureDetails);
			// Prevent problems with "other characters", which causes a problem in e.g. Outlook 2007 or break our HTML
			filename = Regex.Replace(filename, @"[^\d\w\.]", "_");
			// Remove multiple "_"
			filename = Regex.Replace(filename, @"_+", "_");
			string tmpFile = Path.Combine(Path.GetTempPath(),filename);

			LOG.Debug("Creating TMP File: " + tmpFile);
			
			// Catching any exception to prevent that the user can't write in the directory.
			// This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218
			try {
				ImageOutput.Save(image, tmpFile, true, quality, reduceColors, false);
				tmpFileCache.Add(tmpFile, tmpFile);
			} catch (Exception e) {
				// Show the problem
				MessageBox.Show(e.Message, "Error");
				// when save failed we present a SaveWithDialog
				tmpFile = ImageOutput.SaveWithDialog(image, captureDetails);
			}
			return tmpFile;
		}
		
				/// <summary>
		/// Helper method to create a temp image file
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		public static string SaveToTmpFile(Image image, OutputFormat outputFormat, int quality, bool reduceColors) {
			string tmpFile = Path.GetRandomFileName() + "." + outputFormat.ToString();
			// Prevent problems with "other characters", which could cause problems
			tmpFile = Regex.Replace(tmpFile, @"[^\d\w\.]", "");
			string tmpPath = Path.Combine(Path.GetTempPath(), tmpFile);
			LOG.Debug("Creating TMP File : " + tmpPath);
			
			try {
				ImageOutput.Save(image, tmpPath, true, quality, reduceColors, false);
				tmpFileCache.Add(tmpPath, tmpPath);
			} catch (Exception) {
				return null;
			}
			return tmpPath;
		}

		/// <summary>
		/// Cleanup all created tmpfiles
		/// </summary>	
		public static void RemoveTmpFiles() {
			foreach(string tmpFile in tmpFileCache.GetElements()) {
				if (File.Exists(tmpFile)) {
					LOG.DebugFormat("Removing old temp file {0}", tmpFile);
					File.Delete(tmpFile);
				}
			}
		}
		
		/// <summary>
		/// Cleanup handler for expired tempfiles
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="alsoTheFilename"></param>
		private static void RemoveExpiredTmpFile(string filekey, object filename) {
			string path = filename as string;
			if (path != null && File.Exists(path)) {
				LOG.DebugFormat("Removing expired file {0}", path);
				File.Delete(path);
			}
		}
	}
}
