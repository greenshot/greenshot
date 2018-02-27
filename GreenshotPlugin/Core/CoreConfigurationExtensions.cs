#region Greenshot GNU General License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General License for more details.
// 
// You should have received a copy of the GNU General License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using GreenshotPlugin.Core.Enums;
using System.Windows.Forms;
using Dapplo.Log;

#endregion

namespace GreenshotPlugin.Core
{
    /// <summary>
    ///     Extensions for the ICoreConfiguration.
    /// </summary>
    public static class CoreConfigurationExtensions
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     Validate the OutputFilePath, and if this is not correct it will be set to the default
        ///     Added for BUG-1992, reset the OutputFilePath / OutputFileAsFullpath if they don't exist (e.g. the configuration is
        ///     used on a different PC)
        /// </summary>
        public static void ValidateAndCorrectOutputFilePath(this ICoreConfiguration coreConfiguration)
        {
            if (!Directory.Exists(coreConfiguration.OutputFilePath))
            {
                coreConfiguration.RestoreToDefault("OutputFilePath");
            }
        }

        /// <summary>
        ///     Validate the OutputFileAsFullpath, and if this is not correct it will be set to the default
        ///     Added for BUG-1992, reset the OutputFilePath / OutputFileAsFullpath if they don't exist (e.g. the configuration is
        ///     used on a different PC)
        /// </summary>
        public static void ValidateAndCorrectOutputFileAsFullpath(this ICoreConfiguration coreConfiguration)
        {
            var outputFilePath = Path.GetDirectoryName(coreConfiguration.OutputFileAsFullpath);
            if (outputFilePath == null || !File.Exists(coreConfiguration.OutputFileAsFullpath) && !Directory.Exists(outputFilePath))
            {
                coreConfiguration.RestoreToDefault("OutputFileAsFullpath");
            }
        }

        /// <summary>
        ///     Specifies what THIS build is
        /// </summary>
        public static BuildStates BuildState
        {
            get
            {
                var informationalVersion = Application.ProductVersion;
                if (informationalVersion == null)
                {
                    return BuildStates.RELEASE;
                }

                if (informationalVersion.ToLowerInvariant().Contains("-rc"))
                {
                    return BuildStates.RELEASE_CANDIDATE;
                }
                if (informationalVersion.ToLowerInvariant().Contains("-alpha"))
                {
                    return BuildStates.ALPHA;
                }
                if (informationalVersion.ToLowerInvariant().Contains("-beta"))
                {
                    return BuildStates.BETA;
                }
                return BuildStates.RELEASE;
            }
        }

        /// <summary>
        ///     Supply values we can't put as defaults
        /// </summary>
        /// <param name="property">The property to return a default for</param>
        /// <returns>object with the default value for the supplied property</returns>
        public static object GetDefault(this ICoreConfiguration coreConfiguration, string property)
        {
            switch (property)
            {
                case "OutputFileAsFullpath":
                    if (coreConfiguration.IsPortable)
                    {
                        return Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots\dummy.png");
                    }
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dummy.png");
                case "OutputFilePath":
                    if (!coreConfiguration.IsPortable)
                    {
                        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    }

                    var pafOutputFilePath = Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots");
                    if (!Directory.Exists(pafOutputFilePath))
                    {
                        try
                        {
                            Directory.CreateDirectory(pafOutputFilePath);
                            return pafOutputFilePath;
                        }
                        catch (Exception ex)
                        {
                            Log.Warn().WriteLine(ex);
                            // Problem creating directory, fallback to Desktop
                        }
                    }
                    else
                    {
                        return pafOutputFilePath;
                    }
                    return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                case "DWMBackgroundColor":
                    return Color.Transparent;
                case "ActiveTitleFixes":
                    return new List<string> { "Firefox", "IE", "Chrome" };
                case "TitleFixMatcher":
                    return new Dictionary<string, string> { { "Firefox", " - Mozilla Firefox.*" }, { "IE", " - (Microsoft|Windows) Internet Explorer.*" }, { "Chrome", " - Google Chrome.*" } };
                case "TitleFixReplacer":
                    return new Dictionary<string, string> { { "Firefox", "" }, { "IE", "" }, { "Chrome", "" } };
            }
            return null;
        }

        /// <summary>
        ///     This method will be called before writing the configuration
        /// </summary>
        public static void BeforeSave(this ICoreConfiguration coreConfiguration)
        {
            try
            {
                // Store version, this can be used later to fix settings after an update
                coreConfiguration.LastSaveWithVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        ///     This method will be called after reading the configuration, so eventually some corrections can be made
        /// </summary>
        public static void AfterLoad(this ICoreConfiguration coreConfiguration)
        {
            // Comment with releases
            // CheckForUnstable = true;

            if (string.IsNullOrEmpty(coreConfiguration.LastSaveWithVersion))
            {
                try
                {
                    // Store version, this can be used later to fix settings after an update
                    coreConfiguration.LastSaveWithVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
                }
                catch
                {
                    // ignored
                }
                // Disable the AutoReduceColors as it causes issues with Mozzila applications and some others
                coreConfiguration.OutputFileAutoReduceColors = false;
            }

            // Fix for excessive feed checking
            if (coreConfiguration.UpdateCheckInterval != 0 && coreConfiguration.UpdateCheckInterval <= 7 && coreConfiguration.LastSaveWithVersion.StartsWith("1.2"))
            {
                coreConfiguration.UpdateCheckInterval = 14;
            }
            if (coreConfiguration.UpdateCheckInterval > 365)
            {
                coreConfiguration.UpdateCheckInterval = 365;
            }

            // Enable OneNote if upgrading from 1.1
            if (coreConfiguration.ExcludeDestinations != null && coreConfiguration.ExcludeDestinations.Contains("OneNote"))
            {
                if (coreConfiguration.LastSaveWithVersion != null && coreConfiguration.LastSaveWithVersion.StartsWith("1.1"))
                {
                    coreConfiguration.ExcludeDestinations.Remove("OneNote");
                }
                else
                {
                    // TODO: Remove with the release
                    coreConfiguration.ExcludeDestinations.Remove("OneNote");
                }
            }

            if (coreConfiguration.OutputDestinations == null)
            {
                coreConfiguration.OutputDestinations = new List<string>();
            }

            // Make sure there is an output!
            if (coreConfiguration.OutputDestinations.Count == 0)
            {
                coreConfiguration.OutputDestinations.Add("Editor");
            }

            // Prevent both settings at once, bug #3435056
            if (coreConfiguration.OutputDestinations.Contains("Clipboard") && coreConfiguration.OutputFileCopyPathToClipboard)
            {
                coreConfiguration.OutputFileCopyPathToClipboard = false;
            }

            // Make sure we have clipboard formats, otherwise a paste doesn't make sense!
            if (coreConfiguration.ClipboardFormats == null || coreConfiguration.ClipboardFormats.Count == 0)
            {
                coreConfiguration.ClipboardFormats = new List<ClipboardFormats> { Enums.ClipboardFormats.PNG, Enums.ClipboardFormats.HTML, Enums.ClipboardFormats.DIB };
            }

            // Make sure the lists are lowercase, to speedup the check
            if (coreConfiguration.NoGDICaptureForProduct != null)
            {
                // Fix error in configuration
                if (coreConfiguration.NoGDICaptureForProduct.Count >= 2)
                {
                    if ("intellij".Equals(coreConfiguration.NoGDICaptureForProduct[0]) && "idea".Equals(coreConfiguration.NoGDICaptureForProduct[1]))
                    {
                        coreConfiguration.NoGDICaptureForProduct = coreConfiguration.NoGDICaptureForProduct.Skip(2).ToList();
                        coreConfiguration.NoGDICaptureForProduct.Add("Intellij Idea");
                    }
                }
                for (var i = 0; i < coreConfiguration.NoGDICaptureForProduct.Count; i++)
                {
                    coreConfiguration.NoGDICaptureForProduct[i] = coreConfiguration.NoGDICaptureForProduct[i].ToLower();
                }
            }
            if (coreConfiguration.NoDWMCaptureForProduct != null)
            {
                // Fix error in configuration
                if (coreConfiguration.NoDWMCaptureForProduct.Count >= 3)
                {
                    if ("citrix".Equals(coreConfiguration.NoDWMCaptureForProduct[0]) && "ica".Equals(coreConfiguration.NoDWMCaptureForProduct[1]) && "client".Equals(coreConfiguration.NoDWMCaptureForProduct[2]))
                    {
                        coreConfiguration.NoDWMCaptureForProduct = coreConfiguration.NoDWMCaptureForProduct.Skip(3).ToList();
                        coreConfiguration.NoDWMCaptureForProduct.Add("Citrix ICA Client");
                    }
                }
                for (var i = 0; i < coreConfiguration.NoDWMCaptureForProduct.Count; i++)
                {
                    coreConfiguration.NoDWMCaptureForProduct[i] = coreConfiguration.NoDWMCaptureForProduct[i].ToLower();
                }
            }

            if (coreConfiguration.AutoCropDifference < 0)
            {
                coreConfiguration.AutoCropDifference = 0;
            }
            if (coreConfiguration.AutoCropDifference > 255)
            {
                coreConfiguration.AutoCropDifference = 255;
            }
            if (coreConfiguration.OutputFileReduceColorsTo < 2)
            {
                coreConfiguration.OutputFileReduceColorsTo = 2;
            }
            if (coreConfiguration.OutputFileReduceColorsTo > 256)
            {
                coreConfiguration.OutputFileReduceColorsTo = 256;
            }

            if (coreConfiguration.WebRequestTimeout <= 10)
            {
                coreConfiguration.WebRequestTimeout = 100;
            }
            if (coreConfiguration.WebRequestReadWriteTimeout < 1)
            {
                coreConfiguration.WebRequestReadWriteTimeout = 100;
            }
        }
    }
}