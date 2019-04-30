// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.IO;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Core.Enums;

namespace Greenshot.Addons.Controls
{
	/// <summary>
	///     Custom dialog for saving images, wraps SaveFileDialog.
	///     For some reason SFD is sealed :(
	/// </summary>
	public class SaveImageFileDialog : IDisposable
	{
		private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly ICaptureDetails _captureDetails;
		private DirectoryInfo _eagerlyCreatedDirectory;
		private FilterOption[] _filterOptions;
		private SaveFileDialog SaveFileDialog;

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="captureDetails">ICaptureDetails</param>
        public SaveImageFileDialog(ICoreConfiguration coreConfiguration, ICaptureDetails captureDetails = null)
		{
            _coreConfiguration = coreConfiguration;
            _captureDetails = captureDetails;
            Init();
        }

		/// <summary>
		///     filename exactly as typed in the filename field
		/// </summary>
		public string FileName
		{
			get { return SaveFileDialog.FileName; }
			set { SaveFileDialog.FileName = value; }
		}

		/// <summary>
		///     initial directory of the dialog
		/// </summary>
		public string InitialDirectory
		{
			get { return SaveFileDialog.InitialDirectory; }
			set { SaveFileDialog.InitialDirectory = value; }
		}

		/// <summary>
		///     returns filename as typed in the filename field with extension.
		///     if filename field value ends with selected extension, the value is just returned.
		///     otherwise, the selected extension is appended to the filename.
		/// </summary>
		public string FileNameWithExtension
		{
			get
			{
				var fn = SaveFileDialog.FileName;
				// if the filename contains a valid extension, which is the same like the selected filter item's extension, the filename is okay
				if (fn.EndsWith(Extension, StringComparison.CurrentCultureIgnoreCase))
				{
					return fn;
				}
				// otherwise we just add the selected filter item's extension
				return fn + "." + Extension;
			}
			set
			{
				FileName = Path.GetFileNameWithoutExtension(value);
				Extension = Path.GetExtension(value);
			}
		}

		/// <summary>
		///     gets or sets selected extension
		/// </summary>
		public string Extension
		{
			get { return _filterOptions[SaveFileDialog.FilterIndex - 1].Extension; }
			set
			{
				for (var i = 0; i < _filterOptions.Length; i++)
				{
					if (value.Equals(_filterOptions[i].Extension, StringComparison.CurrentCultureIgnoreCase))
					{
						SaveFileDialog.FilterIndex = i + 1;
					}
				}
			}
		}

        /// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
		}

        /// <inheritdoc />
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (SaveFileDialog != null)
				{
					SaveFileDialog.Dispose();
					SaveFileDialog = null;
				}
			}
		}

		private void Init()
		{
			SaveFileDialog = new SaveFileDialog();
			ApplyFilterOptions();
			string initialDirectory = null;
			try
			{
				_coreConfiguration.ValidateAndCorrect();
				initialDirectory = Path.GetDirectoryName(_coreConfiguration.OutputFileAsFullpath);
			}
			catch
			{
				Log.Warn().WriteLine("OutputFileAsFullpath was set to {0}, ignoring due to problem in path.", _coreConfiguration.OutputFileAsFullpath);
			}

			if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
			{
				SaveFileDialog.InitialDirectory = initialDirectory;
			}
			else if (Directory.Exists(_coreConfiguration.OutputFilePath))
			{
				SaveFileDialog.InitialDirectory = _coreConfiguration.OutputFilePath;
			}
			// The following property fixes a problem that the directory where we save is locked (bug #2899790)
			SaveFileDialog.RestoreDirectory = true;
			SaveFileDialog.OverwritePrompt = true;
			SaveFileDialog.CheckPathExists = false;
			SaveFileDialog.AddExtension = true;
			ApplySuggestedValues();
		}

		private void ApplyFilterOptions()
		{
			PrepareFilterOptions();
			var fdf = "";
			var preselect = 0;
			var outputFileFormatAsString = Enum.GetName(typeof(OutputFormats), _coreConfiguration.OutputFileFormat);
			for (var i = 0; i < _filterOptions.Length; i++)
			{
				var fo = _filterOptions[i];
				fdf += fo.Label + "|*." + fo.Extension + "|";
				if (outputFileFormatAsString == fo.Extension)
				{
					preselect = i;
				}
			}
			fdf = fdf.Substring(0, fdf.Length - 1);
			SaveFileDialog.Filter = fdf;
			SaveFileDialog.FilterIndex = preselect + 1;
		}

		private void PrepareFilterOptions()
		{
			var supportedImageFormats = (OutputFormats[]) Enum.GetValues(typeof(OutputFormats));
			_filterOptions = new FilterOption[supportedImageFormats.Length];
			for (var i = 0; i < _filterOptions.Length; i++)
			{
				var ifo = supportedImageFormats[i].ToString();
				if (ifo.ToLower().Equals("jpeg"))
				{
					ifo = "Jpg"; // we dont want no jpeg files, so let the dialog check for jpg
				}
				var fo = new FilterOption
				{
					Label = ifo.ToUpper(),
					Extension = ifo.ToLower()
				};
				_filterOptions.SetValue(fo, i);
			}
		}

        /// <summary>
        /// Show the save file dialog
        /// </summary>
        /// <returns></returns>
		public DialogResult ShowDialog()
		{
			var ret = SaveFileDialog.ShowDialog();
			CleanUp();
			return ret;
		}

		/// <summary>
		///     sets InitialDirectory and FileName property of a SaveFileDialog smartly, considering default pattern and last used
		///     path
		/// </summary>
		private void ApplySuggestedValues()
		{
			// build the full path and set dialog properties
			FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(_coreConfiguration.OutputFileFilenamePattern, _captureDetails);
		}

		private void CleanUp()
		{
			// fix for bug #3379053
			try
			{
				if (_eagerlyCreatedDirectory != null && _eagerlyCreatedDirectory.GetFiles().Length == 0 && _eagerlyCreatedDirectory.GetDirectories().Length == 0)
				{
					_eagerlyCreatedDirectory.Delete();
					_eagerlyCreatedDirectory = null;
				}
			}
			catch (Exception e)
			{
				Log.Warn().WriteLine("Couldn't cleanup directory due to: {0}", e.Message);
				_eagerlyCreatedDirectory = null;
			}
		}

		private class FilterOption
		{
			public string Extension;
			public string Label;
		}
	}
}