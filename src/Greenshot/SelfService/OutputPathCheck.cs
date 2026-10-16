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
using System.Linq;
using Greenshot.Base.Core;

namespace Greenshot.SelfService
{
    /// <summary>
    /// What the check of the output folder found
    /// </summary>
    public enum OutputPathFinding
    {
        /// <summary>The folder exists and Greenshot can write to it</summary>
        Ok,
        /// <summary>No folder is configured, Greenshot saves to the default folder</summary>
        NotConfigured,
        /// <summary>The folder contains variables like ${YYYY}: Greenshot checks the folder before filling them in, doesn't find it and switches to the default folder</summary>
        ContainsVariables,
        /// <summary>The folder doesn't exist: at the next save Greenshot switches to the default folder, without a message</summary>
        Missing,
        /// <summary>Greenshot can't write to the folder (permissions, or Windows Defender's controlled folder access)</summary>
        NotWritable,
        /// <summary>A network folder: when it's not reachable (e.g. without VPN), Greenshot switches to the default folder</summary>
        Network,
        /// <summary>A folder synchronized by OneDrive (also the Desktop, when OneDrive backs it up)</summary>
        OneDrive,
        /// <summary>The full path of a file is longer than Windows allows by default</summary>
        TooLong,
        /// <summary>The filename pattern doesn't give a valid filename</summary>
        InvalidFilename,
        /// <summary>The filename pattern creates subfolders</summary>
        PatternCreatesFolders
    }

    /// <summary>
    /// How serious a finding is
    /// </summary>
    public enum OutputPathSeverity
    {
        Ok,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// One finding of the output folder check
    /// </summary>
    public sealed class OutputPathCheckItem
    {
        public OutputPathCheckItem(OutputPathFinding finding, OutputPathSeverity severity, string detail = null)
        {
            Finding = finding;
            Severity = severity;
            Detail = detail;
        }

        public OutputPathFinding Finding { get; }
        public OutputPathSeverity Severity { get; }

        /// <summary>
        /// E.g. the error message of Windows, or null
        /// </summary>
        public string Detail { get; }
    }

    /// <summary>
    /// The result of the output folder check
    /// </summary>
    public sealed class OutputPathCheckResult
    {
        /// <summary>The folder as configured (OutputFilePath)</summary>
        public string ConfiguredPath { get; set; }

        /// <summary>The folder with its variables filled in</summary>
        public string ExpandedPath { get; set; }

        /// <summary>An example of the full path of the next file, null when there is none</summary>
        public string ExampleFile { get; set; }

        public List<OutputPathCheckItem> Items { get; } = new List<OutputPathCheckItem>();

        public OutputPathSeverity Worst
        {
            get
            {
                var worst = OutputPathSeverity.Ok;
                foreach (var item in Items)
                {
                    if (item.Severity > worst)
                    {
                        worst = item.Severity;
                    }
                }
                return worst;
            }
        }
    }

    /// <summary>
    /// Checks where Greenshot saves screenshots, the most common reason why they "disappear": the folder is missing, not writable,
    /// on a network share, contains variables, or the filename pattern gives no valid name. Only reads, changes nothing
    /// (a write test creates a temporary file, which is deleted right away).
    /// </summary>
    public static class OutputPathCheck
    {
        /// <summary>
        /// The longest full path of a file which works everywhere in Windows without long path support
        /// </summary>
        public const int MaxPathLength = 259;

        /// <summary>
        /// Checks the output folder and the filename pattern
        /// </summary>
        /// <param name="configuredPath">OutputFilePath</param>
        /// <param name="filenamePattern">OutputFileFilenamePattern</param>
        /// <param name="fileFormat">OutputFileFormat, e.g. png</param>
        /// <param name="oneDriveFolders">The OneDrive folders, null for the ones of the current user</param>
        public static OutputPathCheckResult Run(string configuredPath, string filenamePattern, string fileFormat, IEnumerable<string> oneDriveFolders = null)
        {
            var result = new OutputPathCheckResult { ConfiguredPath = configuredPath ?? string.Empty };

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.NotConfigured, OutputPathSeverity.Warning));
                return result;
            }

            string expanded;
            try
            {
                expanded = FilenameHelper.FillVariables(configuredPath, false);
            }
            catch (Exception ex)
            {
                expanded = configuredPath;
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.ContainsVariables, OutputPathSeverity.Error, ex.Message));
            }
            result.ExpandedPath = expanded;

            // ValidateAndCorrectOutputFilePath checks the configured value, before the variables are filled in
            if (configuredPath.Contains("${") && !Directory.Exists(configuredPath))
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.ContainsVariables, OutputPathSeverity.Error));
            }

            bool isNetwork = IsNetworkPath(expanded);
            if (isNetwork)
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.Network, OutputPathSeverity.Warning));
            }
            if (IsInOneDrive(expanded, oneDriveFolders ?? GetOneDriveFolders()))
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.OneDrive, OutputPathSeverity.Info));
            }

            bool exists = SafeDirectoryExists(expanded);
            if (!exists)
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.Missing, OutputPathSeverity.Error));
            }
            else
            {
                string writeError = TryWrite(expanded);
                if (writeError != null)
                {
                    result.Items.Add(new OutputPathCheckItem(OutputPathFinding.NotWritable, OutputPathSeverity.Error, writeError));
                }
            }

            CheckFilename(result, expanded, filenamePattern, fileFormat);

            if (exists && result.Worst <= OutputPathSeverity.Info)
            {
                result.Items.Insert(0, new OutputPathCheckItem(OutputPathFinding.Ok, OutputPathSeverity.Ok));
            }
            return result;
        }

        private static void CheckFilename(OutputPathCheckResult result, string folder, string filenamePattern, string fileFormat)
        {
            string pattern = string.IsNullOrWhiteSpace(filenamePattern) ? "greenshot ${capturetime}" : filenamePattern;
            string filename;
            try
            {
                filename = FilenameHelper.GetFilenameFromPattern(pattern, fileFormat);
            }
            catch (Exception ex)
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.InvalidFilename, OutputPathSeverity.Error, ex.Message));
                return;
            }

            // Literal characters of the pattern stay as they are, e.g. < or |, which Windows doesn't allow
            int invalid = (filename ?? string.Empty).IndexOfAny(Path.GetInvalidPathChars().Concat(new[] { ':', '*', '?' }).ToArray());
            if (invalid >= 0)
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.InvalidFilename, OutputPathSeverity.Error, $"'{filename[invalid]}'"));
                return;
            }
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(filename ?? string.Empty);
            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.InvalidFilename, OutputPathSeverity.Error));
                return;
            }

            if (filename.IndexOf('\\') >= 0 || filename.IndexOf('/') >= 0)
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.PatternCreatesFolders, OutputPathSeverity.Info));
            }

            string fullPath;
            try
            {
                // Like FileDestination: the folder and the filename combined
                fullPath = Path.Combine(folder, filename);
            }
            catch (Exception ex)
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.InvalidFilename, OutputPathSeverity.Error, ex.Message));
                return;
            }
            result.ExampleFile = fullPath;

            if (fullPath.Length > MaxPathLength)
            {
                // Path.GetFullPath would refuse it without long path support
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.TooLong, OutputPathSeverity.Warning, fullPath.Length.ToString()));
                return;
            }
            try
            {
                result.ExampleFile = Path.GetFullPath(fullPath);
            }
            catch (Exception ex)
            {
                result.Items.Add(new OutputPathCheckItem(OutputPathFinding.InvalidFilename, OutputPathSeverity.Error, ex.Message));
            }
        }

        /// <summary>
        /// A UNC path (\\server\share) or a drive which is a network drive
        /// </summary>
        internal static bool IsNetworkPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
            {
                return true;
            }
            try
            {
                string root = Path.GetPathRoot(path);
                return !string.IsNullOrEmpty(root) && new DriveInfo(root).DriveType == DriveType.Network;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsInOneDrive(string path, IEnumerable<string> oneDriveFolders)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            foreach (var folder in oneDriveFolders)
            {
                if (string.IsNullOrEmpty(folder))
                {
                    continue;
                }
                string withSeparator = folder.TrimEnd('\\') + @"\";
                if (path.StartsWith(withSeparator, StringComparison.OrdinalIgnoreCase) || string.Equals(path.TrimEnd('\\'), folder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<string> GetOneDriveFolders()
        {
            foreach (var variable in new[] { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" })
            {
                yield return Environment.GetEnvironmentVariable(variable);
            }
        }

        private static bool SafeDirectoryExists(string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates and deletes a temporary file in the folder, null when that works, otherwise the error message
        /// </summary>
        internal static string TryWrite(string folder)
        {
            string testFile = Path.Combine(folder, $".greenshot-write-test-{Guid.NewGuid():N}.tmp");
            try
            {
                using (new FileStream(testFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
                {
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                try
                {
                    if (File.Exists(testFile))
                    {
                        File.Delete(testFile);
                    }
                }
                catch
                {
                    // ignored, DeleteOnClose removes it
                }
            }
        }
    }
}
