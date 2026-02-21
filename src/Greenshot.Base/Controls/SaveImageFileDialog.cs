/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Dapplo.Windows.Dialogs;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// Custom dialog for saving images, using the modern FileSaveDialogBuilder.
    /// </summary>
    public class SaveImageFileDialog : IDisposable
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(SaveImageFileDialog));
        private static readonly CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
        private readonly ICaptureDetails _captureDetails;
        private string _selectedPath;
        private string _suggestedFileName;
        private string _initialDirectory;
        private string _defaultExtension;
        private FilterOption[] _filterOptions;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public SaveImageFileDialog(ICaptureDetails captureDetails)
        {
            _captureDetails = captureDetails;
            Init();
        }

        private void Init()
        {
            PrepareFilterOptions();

            // Determine initial directory from the last used full path, then fall back to the configured output folder
            _initialDirectory = null;
            try
            {
                conf.ValidateAndCorrectOutputFileAsFullpath();
                string dir = Path.GetDirectoryName(conf.OutputFileAsFullpath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    _initialDirectory = dir;
                }
            }
            catch
            {
                LOG.WarnFormat("OutputFileAsFullpath was set to {0}, ignoring due to problem in path.", conf.OutputFileAsFullpath);
            }

            if (_initialDirectory == null && Directory.Exists(conf.OutputFilePath))
            {
                _initialDirectory = conf.OutputFilePath;
            }

            // Determine default extension from the configured output format
            _defaultExtension = Enum.GetName(typeof(OutputFormat), conf.OutputFileFormat)?.ToLower();

            // Build suggested filename from the configured pattern
            _suggestedFileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(conf.OutputFileFilenamePattern, _captureDetails);
        }

        private void PrepareFilterOptions()
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var supportedExtensions = fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.SaveToFile).Select(s => s.Substring(1)).ToList();

            _filterOptions = new FilterOption[supportedExtensions.Count];
            for (int i = 0; i < _filterOptions.Length; i++)
            {
                string ext = supportedExtensions[i];
                _filterOptions[i] = new FilterOption
                {
                    Label = ext.ToUpper(),
                    Extension = ext.ToLower()
                };
            }
        }

        /// <summary>
        /// The full selected path. Before <see cref="ShowDialog"/> is called, returns the
        /// suggested full path built from the initial directory and suggested filename.
        /// </summary>
        public string FileName
        {
            get
            {
                if (_selectedPath != null)
                    return _selectedPath;
                string dir = _initialDirectory ?? conf.OutputFilePath ?? string.Empty;
                string name = !string.IsNullOrEmpty(_defaultExtension)
                    ? _suggestedFileName + "." + _defaultExtension
                    : _suggestedFileName;
                return string.IsNullOrEmpty(dir) ? name : Path.Combine(dir, name);
            }
            set => _suggestedFileName = value;
        }

        /// <summary>
        /// Initial directory shown when the dialog opens.
        /// </summary>
        public string InitialDirectory
        {
            get => _initialDirectory;
            set => _initialDirectory = value;
        }

        /// <summary>
        /// Returns the selected path with the correct extension. Before <see cref="ShowDialog"/> is
        /// called, returns the same value as <see cref="FileName"/>.
        /// </summary>
        public string FileNameWithExtension
        {
            get => _selectedPath ?? FileName;
            set
            {
                _suggestedFileName = Path.GetFileNameWithoutExtension(value);
                _defaultExtension = Path.GetExtension(value)?.TrimStart('.');
            }
        }

        /// <summary>
        /// Gets or sets the selected/default file extension (without leading dot).
        /// </summary>
        public string Extension
        {
            get => _selectedPath != null
                ? Path.GetExtension(_selectedPath)?.TrimStart('.')
                : _defaultExtension;
            set => _defaultExtension = value?.TrimStart('.');
        }

        public DialogResult ShowDialog()
        {
            string suggestedName = !string.IsNullOrEmpty(_defaultExtension)
                ? _suggestedFileName + "." + _defaultExtension
                : _suggestedFileName;

            var builder = new FileSaveDialogBuilder()
                .WithSuggestedFileName(suggestedName)
                .WithDefaultExtension(_defaultExtension);

            if (!string.IsNullOrEmpty(_initialDirectory))
            {
                builder = builder.WithInitialDirectory(_initialDirectory);
            }

            string sidebarPath = _initialDirectory ?? conf.OutputFilePath;
            if (!string.IsNullOrEmpty(sidebarPath) && Directory.Exists(sidebarPath))
            {
                builder = builder.AddPlace(sidebarPath, atTop: true);
            }

            foreach (var fo in _filterOptions)
            {
                builder = builder.AddFilter(fo.Label, "*." + fo.Extension);
            }

            var result = builder.ShowDialog();
            if (result.WasCancelled)
            {
                return DialogResult.Cancel;
            }

            _selectedPath = result.SelectedPath;
            return DialogResult.OK;
        }

        private class FilterOption
        {
            public string Label;
            public string Extension;
        }
    }
}