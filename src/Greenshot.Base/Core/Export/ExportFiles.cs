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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Base.Core.Export
{
    /// <summary>
    /// The file already exists and may not be overwritten.
    /// </summary>
    public class FileAlreadyExistsException : IOException
    {
        public FileAlreadyExistsException(string fullPath) : base($"File '{fullPath}' already exists.")
        {
            FullPath = fullPath;
        }

        public string FullPath { get; }
    }

    /// <summary>
    /// Writing an export source to files (roadmap 5.5, ImageIO.Save* → SaveAsync): encoded on the pool, written asynchronously
    /// to a temporary file which is renamed at the end, so a cancelled or failed save never leaves half a file.
    /// </summary>
    public static class ExportFiles
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExportFiles));

        /// <summary>
        /// Save the source to the file.
        /// </summary>
        /// <exception cref="FileAlreadyExistsException">The file exists and allowOverwrite is false</exception>
        public static async Task<string> SaveAsync(IExportSource source, string fullPath, bool allowOverwrite, SurfaceOutputSettings outputSettings, CancellationToken cancellationToken)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (outputSettings == null) throw new ArgumentNullException(nameof(outputSettings));
            fullPath = FilenameHelper.MakeFqFilenameSafe(fullPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!allowOverwrite && File.Exists(fullPath))
            {
                throw new FileAlreadyExistsException(fullPath);
            }

            var encoded = await source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
            string tmpPath = Path.Combine(directory ?? Path.GetTempPath(), $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            Log.DebugFormat("Saving {0} bytes to {1}", encoded.Length, fullPath);
            try
            {
                using (var input = encoded.OpenRead())
                using (var output = new FileStream(tmpPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await input.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
                    await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(fullPath))
                {
                    if (!allowOverwrite)
                    {
                        throw new FileAlreadyExistsException(fullPath);
                    }

                    try
                    {
                        File.Replace(tmpPath, fullPath, null);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
                    {
                        // ReplaceFile isn't supported everywhere (some network shares): overwrite the old way
                        Log.Debug($"File.Replace failed for {fullPath}, copying instead", ex);
                        File.Copy(tmpPath, fullPath, true);
                        TryDelete(tmpPath);
                    }
                }
                else
                {
                    File.Move(tmpPath, fullPath);
                }
            }
            catch
            {
                TryDelete(tmpPath);
                throw;
            }

            return fullPath;
        }

        /// <summary>
        /// Save the source to a temporary file named after the configured filename pattern (used e.g. by the e-mail and Office exports),
        /// the file is removed at exit.
        /// </summary>
        public static async Task<string> SaveNamedTmpFileAsync(IExportSource source, ICaptureDetails captureDetails, SurfaceOutputSettings outputSettings, CancellationToken cancellationToken)
        {
            var coreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
            string pattern = coreConfig.OutputFileFilenamePattern;
            if (string.IsNullOrEmpty(pattern?.Trim()))
            {
                pattern = "greenshot ${capturetime}";
            }

            string filename = FilenameHelper.GetFilenameFromPattern(pattern, outputSettings.Format, captureDetails);
            // Prevent problems with "other characters", which causes a problem in e.g. Outlook 2007 or break our HTML
            filename = Regex.Replace(filename, @"[^\d\w\.]", "_");
            // Remove multiple "_"
            filename = Regex.Replace(filename, @"_+", "_");
            string tmpFile = Path.Combine(Path.GetTempPath(), filename);
            Log.Debug("Creating TMP File: " + tmpFile);
            await SaveAsync(source, tmpFile, true, outputSettings, cancellationToken).ConfigureAwait(false);
            ImageIO.RegisterTmpFile(tmpFile);
            return tmpFile;
        }

        /// <summary>
        /// Save the source to a temporary file with a random name (e.g. for external commands), the file is removed at exit.
        /// </summary>
        public static async Task<string> SaveToTmpFileAsync(IExportSource source, SurfaceOutputSettings outputSettings, string destinationPath, CancellationToken cancellationToken)
        {
            string tmpFile = Regex.Replace(Path.GetRandomFileName() + "." + outputSettings.Format, @"[^\d\w\.]", string.Empty);
            string tmpPath = Path.Combine(destinationPath ?? Path.GetTempPath(), tmpFile);
            Log.Debug("Creating TMP File : " + tmpPath);
            await SaveAsync(source, tmpPath, true, outputSettings, cancellationToken).ConfigureAwait(false);
            ImageIO.RegisterTmpFile(tmpPath);
            return tmpPath;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't delete the temporary file {path}", ex);
            }
        }
    }
}
