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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Expressions;
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
        /// The steps in which the icon size can be changed
        /// </summary>
        public const int IconSizeStep = 4;

        /// <summary>
        /// Coerce the value to stay between 16 and 256, in steps of 4 pixels (the size at 100%, it's scaled with the DPI of the display).
        /// </summary>
        /// <param name="value">NativeSize</param>
        partial void OnIconSizeSet(ref NativeSize value) => value = CoerceIconSize(value);

        /// <summary>
        /// Loading the ini file doesn't go through the setter, so a value from the file is coerced when it's read
        /// </summary>
        /// <param name="value">NativeSize</param>
        partial void OnIconSizeGet(ref NativeSize value) => value = CoerceIconSize(value);

        private static NativeSize CoerceIconSize(NativeSize value)
        {
            int newWidth = (Clamp(value.Width, 16, 256) / IconSizeStep) * IconSizeStep;
            int newHeight = (Clamp(value.Height, 16, 256) / IconSizeStep) * IconSizeStep;
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

        partial void OnOutputFilePngCompressionLevelSet(ref int value) => value = Clamp(value, 1, 9);
        partial void OnOutputFilePngCompressionLevelGet(ref int value) => value = Clamp(value, 1, 9);

        partial void OnClipboardPngCompressionLevelSet(ref int value) => value = Clamp(value, 1, 9);
        partial void OnClipboardPngCompressionLevelGet(ref int value) => value = Clamp(value, 1, 9);


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

        /// <summary>
        /// The default of OutputDirectory as a template (${user.desktop}), so it moves with the folder; portable: the path next to
        /// Greenshot, which stays as it is
        /// </summary>
        private string DefaultOutputDirectory() =>
            GreenshotEnvironment.IsPortable ? CreateOutputFilePath() : OutputTemplates.FolderTemplateOf(CreateOutputFilePath());

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
        /// Sets the default folder when no OutputDirectory is configured. A configured folder stays, also when it doesn't exist right now:
        /// <see cref="GetOutputDirectory"/> creates it, or uses the default folder for one save (e.g. a network drive which isn't reachable).
        /// </summary>
        public void ValidateAndCorrectOutputDirectory()
        {
            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                OutputDirectory = DefaultOutputDirectory();
            }
        }

        /// <summary>
        /// The folder for a new screenshot: the configured folder rendered for the capture and created when it doesn't exist, or the
        /// default folder for this one save when the configured one can't be used (e.g. a network drive which isn't connected).
        /// </summary>
        /// <param name="scope">What the template can use, null for none</param>
        /// <param name="problem">Why the configured folder couldn't be used, null when it is used</param>
        public string GetOutputDirectory(VariableScope scope, out string problem)
        {
            problem = null;
            ValidateAndCorrectOutputDirectory();
            string configured = OutputDirectory;
            string directory = null;
            try
            {
                directory = VariableResolver.Instance.Render(configured, TemplatePurpose.Folder, scope);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Log.InfoFormat("Created the screenshot folder {0}", directory);
                }
                return directory;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException || ex is TemplateException)
            {
                problem = ex.Message;
            }

            string fallback = CreateOutputFilePath();
            Log.WarnFormat("The screenshot folder '{0}' ('{1}') can't be used: {2} Saving to '{3}' this time, the setting stays.", configured, directory, problem, fallback);
            return fallback;
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

            // OutputFileAllowOverwrite (bool) is replaced by OutputFileIfExists: False meant the save dialog
            if (!OutputFileAllowOverwrite)
            {
                OutputFileIfExists = FileExistsAction.Ask;
                OutputFileAllowOverwrite = true;
            }

            // The clicks on the tray icon start recipes now: the actions of LeftClickAction and DoubleClickAction are moved once
            MoveTrayClickAction("LeftClickAction", nameof(TrayClickRecipe), value => TrayClickRecipe = value);
            MoveTrayClickAction("DoubleClickAction", nameof(TrayDoubleClickRecipe), value => TrayDoubleClickRecipe = value);

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

            // The templates of 1.4 are filled once from the patterns of 1.3, which stay for going back to 1.3. A pattern fixed in
            // greenshot-fixed.ini is converted on every start, so it keeps deciding.
            OutputDirectory = FromLegacyPattern(nameof(OutputDirectory), OutputDirectory, nameof(OutputFilePath), OutputFilePath, true);
            OutputFileNameTemplate = FromLegacyPattern(nameof(OutputFileNameTemplate), OutputFileNameTemplate, nameof(OutputFileFilenamePattern), OutputFileFilenamePattern, false);
            OutputPrintFooterTemplate = FromLegacyPattern(nameof(OutputPrintFooterTemplate), OutputPrintFooterTemplate, nameof(OutputPrintFooterPattern), OutputPrintFooterPattern, false);

            // Set defaults for properties that need computed defaults. A configured folder stays, also when it doesn't exist right now
            // (a network drive at the logon, values like ${payload.time:yyyy}), see GetOutputDirectory
            if (string.IsNullOrEmpty(OutputDirectory))
            {
                OutputDirectory = DefaultOutputDirectory();
            }

            if (string.IsNullOrEmpty(OutputFileAsFullpath))
            {
                OutputFileAsFullpath = GreenshotEnvironment.IsPortable
                    ? Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots\dummy.png")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dummy.png");
            }

            AddBuiltInTitleFixes();
            ExcludePlugins ??= new List<string>();
            IncludePlugins ??= new List<string>();
            ExcludePluginNames ??= new List<string>();
            AllowedPluginNames ??= new List<string>();
            BlockedPluginNames ??= new List<string>();
            AllowedDestinations ??= new List<string>();
            BlockedDestinations ??= new List<string>();
            PluginApprovedHashes ??= new List<string>();
            PluginRejectedHashes ??= new List<string>();
            PluginVerifiedHashes ??= new List<string>();
            // The plugin lists of the user use the names now, values in greenshot-fixed.ini are still read where they are
            if (MoveToPluginNames(nameof(ExcludePlugins), ExcludePlugins, nameof(ExcludePluginNames), ExcludePluginNames)
                | MoveToPluginNames(nameof(IncludePlugins), IncludePlugins, nameof(AllowedPluginNames), AllowedPluginNames))
            {
                MarkAsDirty();
            }
            AllowedUntrustedCertificateHosts ??= new List<string>();
            AllowedCertificateThumbprints ??= new List<string>();
            AiToolsExcludedProcesses ??= new List<string>();
            AiToolsAllowedClients ??= new List<string>();
            AiToolsMcpServerPaths ??= new List<string>();
        }

        /// <summary>
        /// The recipes which do what the actions of the former LeftClickAction and DoubleClickAction did
        /// </summary>
        private static readonly Dictionary<string, string> TrayClickActionRecipes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DO_NOTHING"] = string.Empty,
            ["SHOW_CONTEXT_MENU"] = "recipe_tray_menu",
            ["OPEN_LAST_IN_EXPLORER"] = "recipe_open_capture_folder",
            ["OPEN_LAST_IN_EDITOR"] = "recipe_last_capture_in_editor",
            ["OPEN_SETTINGS"] = "recipe_open_settings",
            ["OPEN_EMPTY_EDITOR"] = "recipe_empty_editor",
            ["CAPTURE_REGION"] = "recipe_region",
            ["CAPTURE_SCREEN"] = "recipe_fullscreen",
            ["CAPTURE_WINDOW"] = "recipe_window",
            // The clipboard and file recipes open the editor
            ["CAPTURE_CLIPBOARD"] = "recipe_clipboard",
            ["OPEN_CLIPBOARD_IN_EDITOR"] = "recipe_clipboard",
            ["OPEN_FILE_IN_EDITOR"] = "recipe_file"
        };

        /// <summary>
        /// Moves the action of a former tray click setting to the recipe of the new setting, and removes the old key
        /// </summary>
        /// <param name="oldKey">LeftClickAction or DoubleClickAction</param>
        /// <param name="newKey">The new setting, not changed when it is fixed (greenshot-fixed.ini)</param>
        /// <param name="setRecipe">Sets the new setting</param>
        private void MoveTrayClickAction(string oldKey, string newKey, Action<string> setRecipe)
        {
            string action;
            try
            {
                action = GetRawValue(oldKey);
            }
            catch (Exception ex)
            {
                Log.Debug($"Couldn't read {oldKey}", ex);
                return;
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            if (TrayClickActionRecipes.TryGetValue(action.Trim(), out var recipeId) && !IsConstant(newKey))
            {
                setRecipe(recipeId);
                Log.InfoFormat("Moved the tray icon action {0}={1} to the recipe '{2}'.", oldKey, action, recipeId);
            }

            // Moved only once: an empty value is not moved again
            try
            {
                SetRawValue(oldKey, string.Empty);
                MarkAsDirty();
            }
            catch (Exception ex)
            {
                Log.Debug($"Couldn't clear {oldKey}", ex);
            }
        }

        /// <summary>
        /// The title fixes Greenshot brings, by name: the regular expression and its replacement.
        /// Edge puts an invisible zero width space between "Microsoft" and " Edge", and adds "and 12 more pages" and the profile name.
        /// </summary>
        internal static readonly IReadOnlyList<(string Name, string Matcher, string Replacer)> BuiltInTitleFixes = new[]
        {
            ("Firefox", " - Mozilla Firefox.*", string.Empty),
            ("Chrome", " - Google Chrome.*", string.Empty),
            ("Edge", @"(?: and \d+ more pages?)?(?: - [^-]+)? - Microsoft\u200B? Edge.*", string.Empty),
        };

        /// <summary>
        /// Add each built-in title fix whose matcher isn't in the settings yet, also to a file of an earlier version, and switch it on.
        /// A fix that is taken out of ActiveTitleFixes stays off, as its matcher stays.
        /// </summary>
        private void AddBuiltInTitleFixes()
        {
            ActiveTitleFixes ??= new List<string> { "Firefox", "Chrome" };
            TitleFixMatcher ??= new Dictionary<string, string>();
            TitleFixReplacer ??= new Dictionary<string, string>();
            bool changed = false;
            foreach (var (name, matcher, replacer) in BuiltInTitleFixes)
            {
                if (TitleFixMatcher.ContainsKey(name))
                {
                    continue;
                }
                TitleFixMatcher[name] = matcher;
                if (!TitleFixReplacer.ContainsKey(name))
                {
                    TitleFixReplacer[name] = replacer;
                }
                if (!ActiveTitleFixes.Contains(name))
                {
                    ActiveTitleFixes.Add(name);
                }
                changed = true;
            }
            if (changed)
            {
                // Changed in place, the section doesn't notice that by itself
                MarkAsDirty();
            }
        }

        /// <summary>
        /// Move the plugin identifiers of IncludePlugins or ExcludePlugins, e.g. Box Plugin, to the list with the names, e.g. Box
        /// </summary>
        /// <returns>true when something was moved</returns>
        private bool MoveToPluginNames(string identifiersKey, List<string> identifiers, string namesKey, List<string> names)
        {
            if (IsConstant(identifiersKey) || IsConstant(namesKey) || !ConfigurationRestrictions.Names(identifiers).Any())
            {
                return false;
            }

            foreach (var name in ConfigurationRestrictions.Names(identifiers).Select(ConfigurationRestrictions.PluginNameFromLegacyIdentifier))
            {
                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    names.Add(name);
                }
            }

            identifiers.Clear();
            return true;
        }

        /// <summary>
        /// The value for a template setting: the converted pattern of 1.3 when the template isn't set yet, or when the pattern is
        /// fixed by an administrator and the template isn't
        /// </summary>
        private string FromLegacyPattern(string templateKey, string template, string patternKey, string pattern, bool percentVariables)
        {
            bool patternFixed = IsConstant(patternKey) && !IsConstant(templateKey);
            if (!patternFixed && !string.IsNullOrEmpty(template)) return template;
            if (string.IsNullOrEmpty(pattern)) return template;
            string converted = LegacyTemplateConverter.Convert(pattern, percentVariables);
            if (percentVariables && !GreenshotEnvironment.IsPortable)
            {
                // A folder: the 1.3 path in a known folder is written with its value, e.g. ${user.desktop}
                converted = OutputTemplates.FolderTemplateOf(converted);
            }
            if (!string.Equals(converted, template, StringComparison.Ordinal))
            {
                Log.InfoFormat("{0} '{1}' of Greenshot 1.3 is {2} '{3}' now", patternKey, pattern, templateKey, converted);
            }
            return converted;
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
