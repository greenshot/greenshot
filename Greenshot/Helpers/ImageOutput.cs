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
using System.Collections;
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

namespace Greenshot.Helpers
{
	/// <summary>
	/// Description of ImageOutput.
	/// </summary>
	public class ImageOutput {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImageOutput));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly int PROPERTY_TAG_SOFTWARE_USED = 0x0131;

		private ImageOutput() {
		}
		
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
		public static void SaveToStream(Image imageToSave, Stream stream, OutputFormat extension, int quality) {
			ImageFormat imfo = null;
			bool disposeImage = false;

			switch (extension) {
				case OutputFormat.bmp:
					imfo = ImageFormat.Bmp;
					break;
				case OutputFormat.gif:
					imfo = ImageFormat.Gif;
					break;
				case OutputFormat.jpg:
					imfo = ImageFormat.Jpeg;
					break;
				case OutputFormat.png:
					imfo = ImageFormat.Png;
					break;
				case OutputFormat.tiff:
					imfo = ImageFormat.Tiff;
					break;
				default:
					imfo = ImageFormat.Png;
					break;
			}
			// If Quantizing is enable, overwrite the image to save with a 256 - color version
			if (conf.OutputFileReduceColors) {
				try {
					LOG.Debug("Reducing colors on bitmap.");
					Quantizer quantizer = new OctreeQuantizer(255,8);
					imageToSave = quantizer.Quantize(imageToSave);
					// Make sure the "new" image is disposed
					disposeImage = true;
				} catch(Exception e) {
					LOG.Warn("Error occurred while Quantizing the image, ignoring and using original. Error: ", e);
				}
			}

			try {
				// Create meta-data
				PropertyItem softwareUsedPropertyItem = CreatePropertyItem(PROPERTY_TAG_SOFTWARE_USED, "Greenshot");
				if (softwareUsedPropertyItem != null) {
					try {
						imageToSave.SetPropertyItem(softwareUsedPropertyItem);
					} catch (ArgumentException) {
						LOG.WarnFormat("Image of type {0} do not support property {1}", imfo, softwareUsedPropertyItem.Id);
					}
				}
				LOG.DebugFormat("Saving image to stream with PixelFormat {0}", imageToSave.PixelFormat);
				if (imfo == ImageFormat.Jpeg) {
					EncoderParameters parameters = new EncoderParameters(1);
					parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(Encoder.Quality, quality);
					ImageCodecInfo[] ies = ImageCodecInfo.GetImageEncoders();
					imageToSave.Save(stream, ies[1], parameters);
				} else {
					imageToSave.Save(stream, imfo);
				}
			} finally {
				// cleanup if needed
				if (disposeImage && imageToSave != null) {
					imageToSave.Dispose();
				}
			}
		}
		
		/// <summary>
		/// Helper method to create a temp image file
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		public static string SaveToTmpFile(Image image) {
			string tmpFile = Path.Combine(Path.GetTempPath(),Path.GetRandomFileName() + "." + conf.OutputFileFormat.ToString());
			// Prevent problems with "spaces", which causes a problem in e.g. Outlook 2007
			tmpFile = tmpFile.Replace(" ", "_");
			tmpFile = tmpFile.Replace("%", "_");
			LOG.Debug("Creating TMP File : " + tmpFile);
			
			try {
				ImageOutput.Save(image, tmpFile, conf.OutputFileJpegQuality, false);
			} catch (Exception) {
				return null;
			}
			return tmpFile;
		}

		/// <summary>
		/// Saves image to specific path with specified quality
		/// </summary>
		public static void Save(Image image, string fullPath, int quality, bool copyPathToClipboard) {
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
			LOG.DebugFormat("Saving image to {0}", fullPath);
			// Create the stream and call SaveToStream
			using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write)) {
				SaveToStream(image, stream, format, quality);
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
		public static void Save(Image img, string fullPath) {
			int q;
			
			// Fix for bug 2912959
			string extension = fullPath.Substring(fullPath.LastIndexOf(".") + 1);
			bool isJPG = false;
			if (extension != null) {
				isJPG = "JPG".Equals(extension.ToUpper()) || "JPEG".Equals(extension.ToUpper());
			}
			
			if(isJPG && conf.OutputFilePromptJpegQuality) {
				JpegQualityDialog jqd = new JpegQualityDialog();
				jqd.ShowDialog();
				q = jqd.Quality;
			} else {
				q = conf.OutputFileJpegQuality;
			}
			Save(img, fullPath, q, conf.OutputFileCopyPathToClipboard);
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
			if(dialogResult.Equals(DialogResult.OK)) {
				try {
					string fileNameWithExtension = saveImageFileDialog.FileNameWithExtension;
					ImageOutput.Save(image, fileNameWithExtension);
					returnValue = fileNameWithExtension;
					conf.OutputFileAsFullpath = fileNameWithExtension;
					IniConfig.Save();
				} catch(System.Runtime.InteropServices.ExternalException) {
					MessageBox.Show(Language.GetInstance().GetFormattedString(LangKey.error_nowriteaccess,saveImageFileDialog.FileName).Replace(@"\\",@"\"), Language.GetInstance().GetString(LangKey.error));
				}
			}
			return returnValue;
		}
		#endregion
	}
}
