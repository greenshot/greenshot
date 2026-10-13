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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core.Enums;
using System.Text.RegularExpressions;
using log4net;

namespace Greenshot.Base.Core
{
    public partial class CoreConfigurationImpl : ICoreConfiguration
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CoreConfigurationImpl));

        /// <summary>
        /// This should have been Math.Clamp, but it's not available for .NET Framework 4.8
        /// </summary>
        /// <param name="value">The value to clamp</param>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        /// <returns>The clamped value</returns>
        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Coerce the value to stay between 16 and 256, and to be a multiple of 16, as this is required for the icons to be properly displayed in the Windows shell.
        /// </summary>
        /// <param name="value">NativeSize</param>
        partial void OnIconSizeSet(ref NativeSize value) => value = CoerceIconSize(value);

        /// <summary>
        /// Loading the ini file doesn't go through the setter, so a value like 24 from the file is coerced when it's read
        /// </summary>
        /// <param name="value">NativeSize</param>
        partial void OnIconSizeGet(ref NativeSize value) => value = CoerceIconSize(value);

        private static NativeSize CoerceIconSize(NativeSize value)
        {
            int newWidth = (Clamp(value.Width, 16, 256) / 16) * 16;
            int newHeight = (Clamp(value.Height, 16, 256) / 16) * 16;
            return new NativeSize(newWidth, newHeight);
        }

        partial void OnAutoCropDifferenceSet(ref int value) => value = Clamp(value, 0, 255);

        // Also when read: loading the ini file doesn't go through the setter
        partial void OnBufferPoolLimitSet(ref int value) => value = CoerceBufferPoolLimit(value);
        partial void OnBufferPoolLimitGet(ref int value) => value = CoerceBufferPoolLimit(value);

        /// <summary>
        /// The smallest useful limit of the buffer pools in MB: they work in 128 KB blocks, a limit below some blocks
        /// would make every stream allocate again
        /// </summary>
        public const int MinimumBufferPoolLimit = 4;

        /// <summary>
        /// The largest limit of the buffer pools in MB: a pooled buffer is at most 128 MB, above this there's no practical limit
        /// </summary>
        public const int MaximumBufferPoolLimit = 1024;

        /// <summary>
        /// 0 (or less) is no limit, otherwise the limit stays between <see cref="MinimumBufferPoolLimit"/> and <see cref="MaximumBufferPoolLimit"/>
        /// </summary>
        public static int CoerceBufferPoolLimit(int value) => value <= 0 ? 0 : Clamp(value, MinimumBufferPoolLimit, MaximumBufferPoolLimit);

        partial void OnOutputFileReduceColorsToSet(ref int value) => value = Clamp(value, 2, 256);


        /// <summary>
        /// Coerce the value for the update check interval to stay between 0 and 365, where 0 means "never check for updates". This is to prevent excessive update checking that can occur when the configuration is used on a different PC or when the user accidentally enters an excessively high value. See BUG-1992 for details.
        /// </summary>
        /// <param name="value">Update check interval in days</param>
        partial void OnUpdateCheckIntervalSet(ref int value) => value = Clamp(value, 0, 365);

        /// <summary>
        /// Returns true if the supplied experimental feature is enabled
        /// </summary>
        public bool IsExperimentalFeatureEnabled(string experimentalFeature)
        {
            return ExperimentalFeatures != null && ExperimentalFeatures.Contains(experimentalFeature);
        }

        private string CreateOutputFilePath()
        {
            if (GreenshotEnvironment.IsPortable)
            {
                string pafOutputFilePath = Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots");
                if (!Directory.Exists(pafOutputFilePath))
                {
                    try
                    {
                        Directory.CreateDirectory(pafOutputFilePath);
                        return pafOutputFilePath;
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex);
                    }
                }
                else
                {
                    return pafOutputFilePath;
                }
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        /// <summary>
        /// Normalizes file and directory paths by collapsing redundant backslashes,
        /// while preserving leading double-backslashes for UNC network shares and safely
        /// handling paths containing pattern variables/tokens (e.g. ${capturetime}).
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            path = path.Trim();
            bool isUnc = path.StartsWith(@"\\") || path.StartsWith("//");

            // Replace forward slashes with backslashes
            path = path.Replace('/', '\\');

            // Collapse 2 or more consecutive backslashes into a single backslash
            path = Regex.Replace(path, @"\\{2,}", @"\");

            // Restore leading double-backslash for UNC network paths
            if (isUnc)
            {
                path = @"\" + path;
            }

            // Remove trailing backslash unless it is a drive root like C:\ or UNC root \\server\share\
            if (path.Length > 3 && path.EndsWith(@"\") && !path.EndsWith(@":\"))
            {
                path = path.TrimEnd('\\');
            }

            return path;
        }

        partial void OnOutputFilePathSet(ref string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = NormalizePath(value);
            }
        }

        partial void OnOutputFileAsFullpathSet(ref string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = NormalizePath(value);
            }
        }

        /// <summary>
        /// Validate the OutputFilePath, and if this is not correct it will be set to the default.
        /// Added for BUG-1992, reset the OutputFilePath if it doesn't exist (e.g. the configuration is used on a different PC)
        /// </summary>
        public void ValidateAndCorrectOutputFilePath()
        {
            if (!string.IsNullOrEmpty(OutputFilePath))
            {
                OutputFilePath = NormalizePath(OutputFilePath);
            }
            if (!Directory.Exists(OutputFilePath))
            {
                OutputFilePath = CreateOutputFilePath();
            }
        }

        /// <summary>
        /// Validate the OutputFileAsFullpath, and if this is not correct it will be set to the default.
        /// Added for BUG-1992, reset the OutputFileAsFullpath if it doesn't exist (e.g. the configuration is used on a different PC)
        /// </summary>
        public void ValidateAndCorrectOutputFileAsFullpath()
        {
            if (!string.IsNullOrEmpty(OutputFileAsFullpath))
            {
                OutputFileAsFullpath = NormalizePath(OutputFileAsFullpath);
            }
            var outputFilePath = Path.GetDirectoryName(OutputFileAsFullpath);
            if (outputFilePath == null || (!File.Exists(OutputFileAsFullpath) && !Directory.Exists(outputFilePath)))
            {
                OutputFileAsFullpath = GreenshotEnvironment.IsPortable
                    ? Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots\dummy.png")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dummy.png");
            }
        }

        public void OnAfterLoad()
        {
            // Remember the version the file was saved with, before a save (see OnBeforeSave) replaces it
            LoadedWithVersion = LastSaveWithVersion;

            if (string.IsNullOrEmpty(LastSaveWithVersion))
            {
                try
                {
                    SetRawValue(nameof(LastSaveWithVersion), Assembly.GetEntryAssembly()?.GetName().Version.ToString());
                }
                catch
                {
                    // ignored
                }

                // Disable the AutoReduceColors as it causes issues with Mozilla applications and some others
                OutputFileAutoReduceColors = false;
            }

            bool isUpgradeFrom12 = LastSaveWithVersion?.StartsWith("1.2") ?? false;
            // Fix for excessive feed checking
            if (UpdateCheckInterval != 0 && UpdateCheckInterval <= 7 && isUpgradeFrom12)
            {
                UpdateCheckInterval = 14;
            }

            // Enable OneNote if upgrading from 1.1
            if (ExcludeDestinations != null && ExcludeDestinations.Contains("OneNote"))
            {
                if (LastSaveWithVersion != null && LastSaveWithVersion.StartsWith("1.1"))
                {
                    ExcludeDestinations.Remove("OneNote");
                    // Changed in place, the section doesn't notice that by itself
                    MarkAsDirty();
                }
            }

            // Make sure there is an output!
            OutputDestinations ??= new List<string>();
            if (OutputDestinations.Count == 0)
            {
                OutputDestinations.Add("Editor");
                // The list was changed in place: re-assigning the same instance is a no-op for this INotifyPropertyChanged section
                MarkAsDirty();
            }

            // Prevent both settings at once, bug #3435056
            if (OutputDestinations.Contains("Clipboard") && OutputFileCopyPathToClipboard)
            {
                OutputFileCopyPathToClipboard = false;
            }

            // Make sure we have clipboard formats, otherwise a paste doesn't make sense!
            if (ClipboardFormats == null || ClipboardFormats.Count == 0)
            {
                ClipboardFormats = new List<ClipboardFormat>
                {
                    ClipboardFormat.PNG,
                    ClipboardFormat.HTML,
                    ClipboardFormat.DIB
                };
            }

            // Normalize paths to heal any legacy escaping issues (e.g. duplicated backslashes)
            if (!string.IsNullOrEmpty(OutputFilePath))
            {
                var normalized = NormalizePath(OutputFilePath);
                if (!string.Equals(normalized, OutputFilePath, StringComparison.Ordinal))
                {
                    // The setter marks the section dirty, so the healed path is saved
                    OutputFilePath = normalized;
                }
            }

            if (!string.IsNullOrEmpty(OutputFileAsFullpath))
            {
                var normalized = NormalizePath(OutputFileAsFullpath);
                if (!string.Equals(normalized, OutputFileAsFullpath, StringComparison.Ordinal))
                {
                    OutputFileAsFullpath = normalized;
                }
            }

            // Set defaults for properties that need computed defaults
            if (string.IsNullOrEmpty(OutputFilePath) || !Directory.Exists(OutputFilePath))
            {
                OutputFilePath = CreateOutputFilePath();
            }

            if (string.IsNullOrEmpty(OutputFileAsFullpath))
            {
                OutputFileAsFullpath = GreenshotEnvironment.IsPortable
                    ? Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots\dummy.png")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dummy.png");
            }

            ActiveTitleFixes ??= new List<string> { "Firefox", "Chrome" };
            if (TitleFixMatcher == null || TitleFixMatcher.Count == 0)
            {
                SetRawValue("TitleFixMatcher.Firefox", " - Mozilla Firefox.*");
                SetRawValue("TitleFixMatcher.Chrome", " - Google Chrome.*");
            }
            if (TitleFixReplacer == null || TitleFixReplacer.Count == 0)
            {
                SetRawValue("TitleFixReplacer.Firefox", string.Empty);
                SetRawValue("TitleFixReplacer.Chrome", string.Empty);
            }
            ExcludePlugins ??= new List<string>();
            IncludePlugins ??= new List<string>();
            AllowedUntrustedCertificateHosts ??= new List<string>();
            AllowedCertificateThumbprints ??= new List<string>();
            AiToolsExcludedProcesses ??= new List<string>();
            AiToolsAllowedClients ??= new List<string>();
            AiToolsMcpServerPaths ??= new List<string>();
        }

        public bool OnBeforeSave()
        {
            try
            {
                SetRawValue(nameof(LastSaveWithVersion), Assembly.GetEntryAssembly()?.GetName().Version.ToString());
            }
            catch
            {
                // ignored
            }

            return true;
        }
    }
}
