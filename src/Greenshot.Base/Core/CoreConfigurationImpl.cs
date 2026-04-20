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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core.Enums;
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
        partial void OnIconSizeSet(ref NativeSize value)
        {
            int newWidth = (Clamp(value.Width, 16, 256) /16) * 16;
            int newHeight = (Clamp(value.Height, 16, 256) / 16) * 16;
            value = new NativeSize(newWidth, newHeight);
        }

        partial void OnAutoCropDifferenceSet(ref int value) => value = Clamp(value, 0, 255);
        partial void OnOutputFileReduceColorsToSet(ref int value) => value = Clamp(value, 2, 256);
        partial void OnWebRequestTimeoutSet(ref int value) => value = Clamp(value, 1, 100);
        partial void OnWebRequestReadWriteTimeoutSet(ref int value) => value = Clamp(value, 1, 100);


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
        /// Validate the OutputFilePath, and if this is not correct it will be set to the default.
        /// Added for BUG-1992, reset the OutputFilePath if it doesn't exist (e.g. the configuration is used on a different PC)
        /// </summary>
        public void ValidateAndCorrectOutputFilePath()
        {
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
                }
            }

            // Make sure there is an output!
            if (OutputDestinations.Count == 0)
            {
                OutputDestinations.Add("Editor");
                // Re-assign to trigger SetRawValue dirty tracking for the in-place Add
                OutputDestinations = OutputDestinations;
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

            if (NoGDICaptureForProduct != null)
            {
                // Fix error in configuration
                if (NoGDICaptureForProduct.Count >= 2)
                {
                    if ("intellij".Equals(NoGDICaptureForProduct[0]) && "idea".Equals(NoGDICaptureForProduct[1]))
                    {
                        NoGDICaptureForProduct.RemoveRange(0, 2);
                        NoGDICaptureForProduct.Add("Intellij Idea");
                    }
                }

                for (int i = 0; i < NoGDICaptureForProduct.Count; i++)
                {
                    NoGDICaptureForProduct[i] = NoGDICaptureForProduct[i].ToLower();
                }

                MarkAsDirty();
            }

            if (NoDWMCaptureForProduct != null)
            {
                // Fix error in configuration
                if (NoDWMCaptureForProduct.Count >= 3)
                {
                    if ("citrix".Equals(NoDWMCaptureForProduct[0]) && "ica".Equals(NoDWMCaptureForProduct[1]) && "client".Equals(NoDWMCaptureForProduct[2]))
                    {
                        NoDWMCaptureForProduct.RemoveRange(0, 3);
                        NoDWMCaptureForProduct.Add("Citrix ICA Client");
                    }
                }

                for (int i = 0; i < NoDWMCaptureForProduct.Count; i++)
                {
                    NoDWMCaptureForProduct[i] = NoDWMCaptureForProduct[i].ToLower();
                }

                MarkAsDirty();
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

            if (DWMBackgroundColor == default)
            {
                DWMBackgroundColor = Color.Transparent;
            }

            ActiveTitleFixes ??= new List<string> { "Firefox", "Chrome" };
            TitleFixMatcher ??= new Dictionary<string, string>
            {
                { "Firefox", " - Mozilla Firefox.*" },
                { "Chrome", " - Google Chrome.*" }
            };
            TitleFixReplacer ??= new Dictionary<string, string>
            {
                { "Firefox", string.Empty },
                { "Chrome", string.Empty }
            };
            ExcludePlugins ??= new List<string>();
            IncludePlugins ??= new List<string>();
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
