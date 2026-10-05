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
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// The save dialog for images: the file types Greenshot can write, the configured format preselected,
    /// and the file name and folder suggested by the filename pattern. Wraps the Windows save dialog (Microsoft.Win32).
    /// </summary>
    public sealed class SaveImageFileDialog
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(SaveImageFileDialog));
        private static ICoreConfiguration conf => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
        private readonly SaveFileDialog _saveFileDialog = new SaveFileDialog();
        private FilterOption[] _filterOptions;
        private DirectoryInfo _eagerlyCreatedDirectory;
        private readonly ICaptureDetails _captureDetails;

        public SaveImageFileDialog(ICaptureDetails captureDetails)
        {
            _captureDetails = captureDetails;
            Init();
        }

        private void Init()
        {
            ApplyFilterOptions();
            string initialDirectory = null;
            try
            {
                conf.ValidateAndCorrectOutputFileAsFullpath();
                initialDirectory = Path.GetDirectoryName(conf.OutputFileAsFullpath);
            }
            catch
            {
                LOG.WarnFormat("OutputFileAsFullpath was set to {0}, ignoring due to problem in path.", conf.OutputFileAsFullpath);
            }

            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                _saveFileDialog.InitialDirectory = initialDirectory;
            }
            else if (Directory.Exists(conf.OutputFilePath))
            {
                _saveFileDialog.InitialDirectory = conf.OutputFilePath;
            }

            // The following property fixes a problem that the directory where we save is locked (bug #2899790)
            _saveFileDialog.RestoreDirectory = true;
            _saveFileDialog.OverwritePrompt = true;
            _saveFileDialog.CheckPathExists = false;
            _saveFileDialog.AddExtension = true;
            ApplySuggestedValues();
        }

        private void ApplyFilterOptions()
        {
            PrepareFilterOptions();
            if (_filterOptions.Length == 0)
            {
                _saveFileDialog.Filter = "All files|*.*";
                _saveFileDialog.FilterIndex = 1;
                return;
            }

            string fdf = string.Empty;
            int preselect = 0;
            int pngFilterIndex = -1;
            for (int i = 0; i < _filterOptions.Length; i++)
            {
                FilterOption fo = _filterOptions[i];
                fdf += fo.Label + "|" + string.Join(";", fo.Extensions.Select(extension => "*." + extension)) + "|";
                if (WellKnownFileFormats.IsEqualFormat(WellKnownFileFormats.Png, fo.FormatId))
                {
                    pngFilterIndex = i;
                }

                if (string.Equals(conf.OutputFileFormat, fo.FormatId, StringComparison.OrdinalIgnoreCase))
                {
                    preselect = i;
                }
            }

            if (!string.Equals(conf.OutputFileFormat, _filterOptions[preselect].FormatId, StringComparison.OrdinalIgnoreCase) && pngFilterIndex >= 0)
            {
                preselect = pngFilterIndex;
            }

            fdf = fdf.Substring(0, fdf.Length - 1);
            _saveFileDialog.Filter = fdf;
            _saveFileDialog.FilterIndex = preselect + 1;
        }

        private void PrepareFilterOptions()
        {
            var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            var formats = registry?.GetSaveableFileFormats().ToArray()
                ?? Array.Empty<FileFormatDefinition>();
            _filterOptions = new FilterOption[formats.Length];
            for (int i = 0; i < formats.Length; i++)
            {
                var format = formats[i];
                _filterOptions[i] = new FilterOption
                {
                    FormatId = format.Id,
                    Extensions = format.SaveableExtensions.ToArray(),
                    PreferredExtension = format.PreferredExtension,
                    Label = format.GetDisplayName()
                };
            }
        }

        /// <summary>
        /// filename exactly as typed in the filename field
        /// </summary>
        public string FileName
        {
            get { return _saveFileDialog.FileName; }
            set { _saveFileDialog.FileName = value; }
        }

        /// <summary>
        /// initial directory of the dialog
        /// </summary>
        public string InitialDirectory
        {
            get { return _saveFileDialog.InitialDirectory; }
            set { _saveFileDialog.InitialDirectory = value; }
        }

        /// <summary>
        /// returns filename as typed in the filename field with extension.
        /// if filename field value ends with selected extension, the value is just returned.
        /// otherwise, the selected extension is appended to the filename.
        /// </summary>
        public string FileNameWithExtension
        {
            get
            {
                string fn = _saveFileDialog.FileName;
                if (_filterOptions.Length == 0) return fn;
                // if the filename contains a valid extension, which is the same like the selected filter item's extension, the filename is okay
                if (_filterOptions[_saveFileDialog.FilterIndex - 1].Extensions.Any(extension => fn.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase))) return fn;
                // otherwise we just add the selected filter item's extension
                return fn + "." + _filterOptions[_saveFileDialog.FilterIndex - 1].PreferredExtension;
            }
            set
            {
                FileName = Path.GetFileNameWithoutExtension(value);
                Extension = Path.GetExtension(value);
            }
        }

        /// <summary>
        /// gets or sets selected extension
        /// </summary>
        public string Extension
        {
            get { return _filterOptions.Length == 0 ? null : _filterOptions[_saveFileDialog.FilterIndex - 1].PreferredExtension; }
            set
            {
                string normalized = value?.Trim().TrimStart('.');
                for (int i = 0; i < _filterOptions.Length; i++)
                {
                    if (_filterOptions[i].Extensions.Any(extension => string.Equals(normalized, extension, StringComparison.OrdinalIgnoreCase)))
                    {
                        _saveFileDialog.FilterIndex = i + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Show the dialog (on the UI thread), true when the user chose a file
        /// </summary>
        public bool ShowDialog()
        {
            bool chosen = _saveFileDialog.ShowDialog() == true;
            CleanUp();
            return chosen;
        }

        /// <summary>
        /// sets InitialDirectory and FileName property of a SaveFileDialog smartly, considering default pattern and last used path
        /// </summary>
        private void ApplySuggestedValues()
        {
            string expanded = FilenameHelper.GetFilenameWithoutExtensionFromPattern(conf.OutputFileFilenamePattern, _captureDetails);

            // Pattern may expand to a relative subpath (e.g. "2026-03-02\000002 - title").
            // Split into directory and filename parts so the dialog navigates to the right folder.
            string subDir = Path.GetDirectoryName(expanded);
            string fileName = Path.GetFileName(expanded);

            if (!string.IsNullOrEmpty(subDir))
            {
                string baseDir = !string.IsNullOrEmpty(conf.OutputFilePath)
                    ? conf.OutputFilePath
                    : _saveFileDialog.InitialDirectory;
                string fullDir = Path.Combine(baseDir, subDir);

                if (!Directory.Exists(fullDir))
                {
                    try
                    {
                        Directory.CreateDirectory(fullDir);
                        _eagerlyCreatedDirectory = new DirectoryInfo(fullDir);
                    }
                    catch (Exception e)
                    {
                        LOG.WarnFormat("Couldn't create directory {0} due to: {1}", fullDir, e.Message);
                        fullDir = null;
                    }
                }

                if (fullDir != null)
                {
                    _saveFileDialog.InitialDirectory = fullDir;
                }
            }

            FileName = fileName;
        }

        private class FilterOption
        {
            public string Label;
            public string FormatId;
            public string PreferredExtension;
            public string[] Extensions;
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
                LOG.WarnFormat("Couldn't cleanup directory due to: {0}", e.Message);
                _eagerlyCreatedDirectory = null;
            }
        }
    }
}