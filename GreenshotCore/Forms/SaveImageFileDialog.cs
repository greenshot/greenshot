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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Core;
using Greenshot.Helpers;
using Greenshot.Plugin;

namespace Greenshot.Forms {
	/// <summary>
	/// Custom dialog for saving images, wraps SaveFileDialog.
	/// For some reason SFD is sealed :(
	/// </summary>
	public class SaveImageFileDialog {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SaveImageFileDialog));
		protected SaveFileDialog saveFileDialog;
		private FilterOption[] filterOptions;
		private DirectoryInfo eagerlyCreatedDirectory;
		private ICaptureDetails captureDetails = null;
		private CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

		public SaveImageFileDialog() {
			init();
		}

		public SaveImageFileDialog(ICaptureDetails captureDetails) {
			this.captureDetails = captureDetails;
			init();
		}

		private void init() {
			saveFileDialog = new SaveFileDialog();
			applyFilterOptions();
			saveFileDialog.InitialDirectory = Path.GetDirectoryName(conf.OutputFileAsFullpath);
			// The following property fixes a problem that the directory where we save is locked (bug #2899790)
			saveFileDialog.RestoreDirectory = true;

			saveFileDialog.CheckPathExists = false;
			saveFileDialog.AddExtension = false;
			ApplySuggestedValues();
		}

		private void applyFilterOptions() {
			prepareFilterOptions();
			string fdf = "";
			int preselect = 0;
			for(int i=0; i<filterOptions.Length; i++){
				FilterOption fo = filterOptions[i];
				fdf +=  fo.Label + "|*." + fo.Extension + "|";
				if(conf.OutputFileAsFullpath.EndsWith(fo.Extension, StringComparison.CurrentCultureIgnoreCase)) preselect = i;
			}
			fdf = fdf.Substring(0, fdf.Length-1);
			saveFileDialog.Filter = fdf;
			saveFileDialog.FilterIndex = preselect + 1;
		}
		
		private void prepareFilterOptions() {
			filterOptions = new FilterOption[RuntimeConfig.SupportedImageFormats.Length];
			for(int i=0; i<filterOptions.Length; i++){
				string ifo = RuntimeConfig.SupportedImageFormats[i];
				if (ifo.ToLower().Equals("jpeg")) ifo = "Jpg"; // we dont want no jpeg files, so let the dialog check for jpg
				FilterOption fo = new FilterOption();
				fo.Label = ifo.ToUpper();
				fo.Extension = ifo.ToLower();
				filterOptions.SetValue(fo, i);
			}
		}
		
		/// <summary>
		/// filename exactly as typed in the filename field
		/// </summary>
		public string FileName {
			get {return saveFileDialog.FileName;}
			set {saveFileDialog.FileName = value;}
		}
		
		/// <summary>
		/// initial directory of the dialog
		/// </summary>
		public string InitialDirectory {
			get {return saveFileDialog.InitialDirectory;}
			set {saveFileDialog.InitialDirectory = value;}
		}
		
		/// <summary>
		/// returns filename as typed in the filename field with extension.
		/// if filename field value ends with selected extension, the value is just returned.
		/// otherwise, the selected extension is appended to the filename.
		/// </summary>
		public string FileNameWithExtension {
			get {
				string fn = saveFileDialog.FileName;
				// if the filename contains a valid extension, which is the same like the selected filter item's extension, the filename is okay
				if(fn.EndsWith(Extension,System.StringComparison.CurrentCultureIgnoreCase)) return fn;
				// otherwise we just add the selected filter item's extension
				else return fn + "." + Extension;
			}
			set {
				FileName = Path.GetFileNameWithoutExtension(value);
				Extension = Path.GetExtension(value);
			}
		}
		
		/// <summary>
		/// gets or sets selected extension
		/// </summary>
		public string Extension {
			get {
				return filterOptions[saveFileDialog.FilterIndex-1].Extension;
			}
			set {
				for(int i=0; i<filterOptions.Length; i++) {
					if(value.Equals(filterOptions[i].Extension, System.StringComparison.CurrentCultureIgnoreCase)) {
						saveFileDialog.FilterIndex = i + 1;
					}
				}
			}
		}
		
		public DialogResult ShowDialog() {
			DialogResult ret = saveFileDialog.ShowDialog();
			CleanUp();
			return ret;
		}
		
		/// <summary>
		/// sets InitialDirectory and FileName property of a SaveFileDialog smartly, considering default pattern and last used path
		/// </summary>
		/// <param name="sfd">a SaveFileDialog instance</param>
		private void ApplySuggestedValues() {
			string rootDir = GetRootDirFromConfig();
			// build the full path and set dialog properties
			string filenameFromPattern = FilenameHelper.GetFilenameWithoutExtensionFromPattern(conf.OutputFileFilenamePattern, captureDetails);
			string fullpath = Path.Combine(rootDir, filenameFromPattern);
			string dir = CreateDirectoryIfNotExists(fullpath);
			
			FileName = Path.GetFileNameWithoutExtension(fullpath);
			Extension = Path.GetExtension(fullpath);
			InitialDirectory = dir;
			
			
			
			/*string path = conf.Output_FileAs_Fullpath;
			if(path.Length == 0) {
				// first save -> apply default storage location and pattern
				saveFileDialog.InitialDirectory = conf.Output_File_Path;
				saveFileDialog.FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(conf.Output_File_FilenamePattern);
			} else {
				// check whether last used path matches default pattern
				string patternStr = Path.Combine(conf.Output_File_Path, conf.Output_File_FilenamePattern);
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
					 di = Directory.CreateDirectory(recommendedDir);
					 eagerlyCreatedDirectory = di;
				}
				InitialDirectory = recommendedDir;
				FileName = fileName;
				Extension = Path.GetExtension(fileName);
			}*/
		}
		
		private string GetRootDirFromConfig() {
			string rootDir =conf.OutputFilePath;
			// the idea was to let the user choose whether to suggest the dir
			// configured in the settings dialog or just remember the latest path.
			// however, we'd need an extra option for this, making the settings dialog
			// less comprehensive -> perhaps later....
			/*if(rootDir.Length == 0) { // <-- no, wouldn't work just like that
				// no default storage location, use latest path or desktop
				rootDir = conf.Output_FileAs_Fullpath;
				if(rootDir.Length == 0) {
					rootDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				} else {
					rootDir = Path.GetDirectoryName(rootDir);
				}
			} else {
				// storage location is configured
				rootDir = conf.Output_File_Path;
			}*/
			return rootDir;
		}
		
		private class FilterOption {
			public string Label;
			public string Extension;
		}
		
		private void CleanUp() {
			if(eagerlyCreatedDirectory!=null
			   && eagerlyCreatedDirectory.GetFiles().Length==0 
			   && eagerlyCreatedDirectory.GetDirectories().Length==0) {
				eagerlyCreatedDirectory.Delete();
				eagerlyCreatedDirectory = null;
			}
		}
		
		private string CreateDirectoryIfNotExists(string fullPath) {
			string dirName = null;
			try {
				dirName = Path.GetDirectoryName(fullPath);
				DirectoryInfo di = new DirectoryInfo(dirName);
				if(!di.Exists) {
					 di = Directory.CreateDirectory(dirName);
					 eagerlyCreatedDirectory = di;
				}
			} catch (Exception e) {
				LOG.Error("Error in CreateDirectoryIfNotExists",e);
			}
			return dirName;
		}
	}
}
