/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Globalization;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using log4net;
using Dapplo.Windows.Dialogs;
using Greenshot.Base.Expressions;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// The save dialog for images: the file types Greenshot can write, the configured format preselected,
    /// and the file name and folder suggested by the filename pattern, or the suggested file (e.g. of a recipe step). Shows the Windows save dialog of Dapplo.Windows.Dialogs.
    /// </summary>
    public sealed class SaveImageFileDialog
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(SaveImageFileDialog));
        private static ICoreConfiguration conf => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
        private string _fileName;
        private string _initialDirectory;
        // The 0-based index of the selected file type in _filterOptions
        private int _filterIndex;
        private FilterOption[] _filterOptions;
        private DirectoryInfo _eagerlyCreatedDirectory;
        private readonly ICaptureDetails _captureDetails;
        private readonly bool _hdrAvailable;
        private readonly string _suggestedPath;

        /// <param name="captureDetails">The details of the capture, for the file name</param>
        /// <param name="hdrAvailable">The capture can be saved as HDR file: JPEG XR is labeled "(HDR)"</param>
        /// <param name="suggestedPath">The file to suggest (folder, name and format) instead of the one of the settings, null for the settings</param>
        public SaveImageFileDialog(ICaptureDetails captureDetails, bool hdrAvailable = false, string suggestedPath = null)
        {
            _captureDetails = captureDetails;
            _hdrAvailable = hdrAvailable;
            _suggestedPath = suggestedPath;
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
                _initialDirectory = initialDirectory;
            }
            else
            {
                string outputDirectory = OutputTemplates.RenderFolder(conf.OutputDirectory, VariableScope.ForCapture(_captureDetails), out _);
                if (Directory.Exists(outputDirectory))
                {
                    _initialDirectory = outputDirectory;
                }
            }

            // The dialog doesn't change the current directory, which locked the folder where we save (bug #2899790), and asks before it overwrites a file
            if (string.IsNullOrWhiteSpace(_suggestedPath))
            {
                ApplySuggestedValues();
            }
            else
            {
                ApplySuggestedPath();
            }
        }

        /// <summary>
        /// The folder, name and format of the suggested file
        /// </summary>
        private void ApplySuggestedPath()
        {
            string directory, fileName, extension;
            try
            {
                directory = Path.GetDirectoryName(_suggestedPath);
                fileName = Path.GetFileNameWithoutExtension(_suggestedPath);
                extension = Path.GetExtension(_suggestedPath);
            }
            catch (ArgumentException)
            {
                // .NET Framework: characters a path can't have
                LOG.WarnFormat("The suggested file {0} isn't a valid path, suggesting the file of the settings.", _suggestedPath);
                ApplySuggestedValues();
                return;
            }

            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                _initialDirectory = directory;
            }

            FileName = fileName;
            if (!string.IsNullOrEmpty(extension))
            {
                Extension = extension;
            }
        }

        private void ApplyFilterOptions()
        {
            PrepareFilterOptions();
            if (_filterOptions.Length == 0)
            {
                _filterIndex = 0;
                return;
            }

            int preselect = 0;
            int pngFilterIndex = -1;
            for (int i = 0; i < _filterOptions.Length; i++)
            {
                FilterOption fo = _filterOptions[i];
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

            _filterIndex = preselect;
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
                    // JPEG XR of a capture with HDR content is the HDR file
                    Label = _hdrAvailable && WellKnownFileFormats.IsEqualFormat(WellKnownFileFormats.Jxr, format.Id) ? format.GetDisplayName() + " (HDR)" : format.GetDisplayName()
                };
            }
        }

        /// <summary>
        /// filename exactly as typed in the filename field
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        /// <summary>
        /// initial directory of the dialog
        /// </summary>
        public string InitialDirectory
        {
            get { return _initialDirectory; }
            set { _initialDirectory = value; }
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
                string fn = _fileName;
                if (_filterOptions.Length == 0) return fn;
                // if the filename contains a valid extension, which is the same like the selected filter item's extension, the filename is okay
                if (_filterOptions[_filterIndex].Extensions.Any(extension => fn.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase))) return fn;
                // otherwise we just add the selected filter item's extension
                return fn + "." + _filterOptions[_filterIndex].PreferredExtension;
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
            get { return _filterOptions.Length == 0 ? null : _filterOptions[_filterIndex].PreferredExtension; }
            set
            {
                string normalized = value?.Trim().TrimStart('.');
                for (int i = 0; i < _filterOptions.Length; i++)
                {
                    if (_filterOptions[i].Extensions.Any(extension => string.Equals(normalized, extension, StringComparison.OrdinalIgnoreCase)))
                    {
                        _filterIndex = i;
                    }
                }
            }
        }

        /// <summary>
        /// Show the dialog (on the UI thread), true when the user chose a file
        /// </summary>
        public bool ShowDialog()
        {
            var builder = new FileSaveDialogBuilder()
                .WithTitle(DialogTitle())
                .WithInitialDirectory(_initialDirectory)
                .WithSuggestedFileName(_fileName);
            if (_filterOptions.Length == 0)
            {
                builder.AddFilter("All files", "*.*");
            }
            else
            {
                foreach (var filterOption in _filterOptions)
                {
                    builder.AddFilter(filterOption.Label, string.Join(";", filterOption.Extensions.Select(extension => "*." + extension)));
                }

                // The dialog adds the extension of the chosen file type to a name without one
                builder.WithSelectedFilter(_filterIndex).WithDefaultExtension(_filterOptions[_filterIndex].PreferredExtension);
            }

            FileDialogResult result;
            try
            {
                result = builder.ShowDialog();
            }
            finally
            {
                CleanUp();
            }

            if (result.WasCancelled)
            {
                return false;
            }

            _fileName = result.SelectedPath;
            if (_filterOptions.Length > 0 && result.SelectedFilterIndex >= 0 && result.SelectedFilterIndex < _filterOptions.Length)
            {
                // The file type the user chose
                _filterIndex = result.SelectedFilterIndex;
            }

            return true;
        }

        /// <summary>
        /// The title bar says which capture is saved, e.g. "Save as: Inbox - Outlook (08:41:12)", so several open save dialogs can be told apart
        /// </summary>
        /// <returns>the title, null for the default of Windows when there are no capture details</returns>
        private string DialogTitle()
        {
            if (_captureDetails == null)
            {
                return null;
            }

            string title = string.IsNullOrWhiteSpace(_captureDetails.Title) ? "Greenshot" : _captureDetails.Title.Trim();
            string time = _captureDetails.DateTime.ToString("T", CultureInfo.CurrentCulture);
            string format = Texts.Core.SaveAsCaptureTitle;
            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, title, time);
            }
            catch (FormatException)
            {
                // A language pack with a broken placeholder
                LOG.WarnFormat("The text save_as_capture_title {0} can't be formatted.", format);
                return title;
            }
        }

        /// <summary>
        /// sets InitialDirectory and FileName property of a SaveFileDialog smartly, considering default pattern and last used path
        /// </summary>
        private void ApplySuggestedValues()
        {
            string outputDirectory = OutputTemplates.RenderFolder(conf.OutputDirectory, VariableScope.ForCapture(_captureDetails), out _);
            string baseDir = !string.IsNullOrEmpty(outputDirectory) ? outputDirectory : _initialDirectory;
            // A suggestion: the counters show their next number and stay
            string expanded = OutputTemplates.PreviewFileName(_captureDetails, baseDir);
            if (!string.IsNullOrEmpty(baseDir) && expanded.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                expanded = expanded.Substring(baseDir.Length).TrimStart(Path.DirectorySeparatorChar);
            }

            // The template may hold a sub folder (e.g. "2026-03-02\002 - title").
            // Split into directory and filename parts so the dialog navigates to the right folder.
            string subDir = Path.GetDirectoryName(expanded);
            string fileName = Path.GetFileName(expanded);

            if (!string.IsNullOrEmpty(subDir))
            {
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
                    _initialDirectory = fullDir;
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