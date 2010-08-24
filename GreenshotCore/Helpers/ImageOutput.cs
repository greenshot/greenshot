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
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Core;
using Greenshot.Forms;
using Greenshot.Plugin;

namespace Greenshot.Helpers
{
	/// <summary>
	/// Description of ImageOutput.
	/// </summary>
	public class ImageOutput {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImageOutput));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

		private ImageOutput() {
		}
		#region save

		/// <summary>
		/// Saves image to stream with specified quality
		/// </summary>
		public static void SaveToStream(Image img, Stream stream, OutputFormat extension, int quality) {
			ImageFormat imfo = null;
			
			switch (extension) {
				case OutputFormat.Bmp:
					imfo = ImageFormat.Bmp;
					break;
				case OutputFormat.Gif:
					imfo = ImageFormat.Gif;
					break;
				case OutputFormat.Jpeg:
					imfo = ImageFormat.Jpeg;
					break;
				case OutputFormat.Png:
					imfo = ImageFormat.Png;
					break;
				default:
					imfo = ImageFormat.Png;
					break;
			}
			PropertyItem pit = PropertyItemProvider.GetPropertyItem(0x0131, "Greenshot");
			img.SetPropertyItem(pit);
			if (imfo == ImageFormat.Jpeg) {
				EncoderParameters parameters = new EncoderParameters(1);
				parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(Encoder.Quality, quality);
				ImageCodecInfo[] ies = ImageCodecInfo.GetImageEncoders();
				img.Save(stream, ies[1], parameters);
			} else {
				img.Save(stream, imfo);
			}
        }

		/// <summary>
		/// Saves image to specific path with specified quality
		/// </summary>
		public static void Save(Image image, string fullPath, int quality, ICaptureDetails captureDetails) {
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
			OutputFormat format = OutputFormat.Png;
			try {
				format = (OutputFormat)Enum.Parse(typeof(OutputFormat), extension);
			} catch(ArgumentException ae) {
				LOG.Warn("Couldn't parse extension: " + extension, ae);
			}
			// Create the stream and call SaveToStream
			using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write)) {
				SaveToStream(image, stream, format, quality);
			}
			
			// Inform all registered code (e.g. Plugins) of file output
			PluginHelper.instance.CreateImageOutputEvent(fullPath, image, captureDetails);
			
			if (conf.OutputFileCopyPathToClipboard) {
				ClipboardHelper.SetClipboardData(fullPath);
			}
		}

		/// <summary>
		/// saves img to fullpath
		/// </summary>
		/// <param name="img">the image to save</param>
		/// <param name="fullPath">the absolute destination path including file name</param>
		public static void Save(Image img, string fullPath, ICaptureDetails captureDetails) {
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
			Save(img, fullPath, q, captureDetails);
		}
		#endregion
		
		#region save-as
		//private static string eagerlyCreatedDir = null;
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
					ImageOutput.Save(image, fileNameWithExtension, captureDetails);
					returnValue = fileNameWithExtension;
					conf.OutputFileAsFullpath = fileNameWithExtension;
					IniConfig.Save();
				} catch(System.Runtime.InteropServices.ExternalException) {
					MessageBox.Show(Language.GetInstance().GetFormattedString(LangKey.error_nowriteaccess,saveImageFileDialog.FileName).Replace(@"\\",@"\"), Language.GetInstance().GetString(LangKey.error));
					//eagerlyCreatedDir = null;
				}
			}
			// clean up dir we have created when creating SaveFileDialog (if any)
			/*if(eagerlyCreatedDir != null) {
				DirectoryInfo di = new DirectoryInfo(eagerlyCreatedDir);
				if(di.Exists && di.GetFiles().Length == 0) {
					di.Delete();
				}
				eagerlyCreatedDir = null;
			}*/
			return returnValue;
		}
		
		/*
		/// <summary>
		/// sets InitialDirectory and FileName property of a SaveFileDialog smartly, considering default pattern and last used path
		/// </summary>
		/// <param name="sfd">a SaveFileDialog instance</param>
		private static void applyPresetValues(SaveFileDialog sfd) {
			
			AppConfig conf = AppConfig.GetInstance();
			string path = conf.Output_FileAs_Fullpath;
			if(path.Length == 0) {
				// first save -> apply default storage location and pattern
				sfd.InitialDirectory = conf.Output_File_Path;
				sfd.FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(conf.Output_File_FilenamePattern);
			} else {
				// check whether last used path matches default pattern
				string patternStr = conf.Output_File_FilenamePattern;
				patternStr = patternStr.Replace(@"\",@"\\"); // escape backslashes for regex
				IDictionaryEnumerator en = FilenameHelper.Placeholders.GetEnumerator();
				while(en.MoveNext()) {
					patternStr = patternStr.Replace(en.Key.ToString(),en.Value.ToString());
				}
				Regex rg = new Regex(patternStr);
				Match m = rg.Match(path);
				// rootDir serves as root for pattern based saving
				String rootDir = (m.Success) ? path.Substring(0, m.Index) : Path.GetDirectoryName(path);
				String fileNameFromPattern = FilenameHelper.GetFilenameWithoutExtensionFromPattern(conf.Output_File_FilenamePattern);
				String appendDir = Path.GetDirectoryName(fileNameFromPattern);
				String fileName = Path.GetFileName(fileNameFromPattern);
				
				String recommendedDir = Path.Combine(rootDir, appendDir);
				
				// due to weird behavior of SaveFileDialog, we cannot use a path in the FileName property (causes InitialDirectory to be ignored)
				// thus we create the recommended dir eagerly, assign it to InitialDirectory, and clean up afterwards, if it has not been used
				DirectoryInfo di = new DirectoryInfo(recommendedDir);
				if(!di.Exists) {
					 Directory.CreateDirectory(recommendedDir);
					 eagerlyCreatedDir = recommendedDir;
				}
				sfd.InitialDirectory = recommendedDir;
				sfd.FileName = fileName;
			}
		}
		*/
		#endregion
	}
}
