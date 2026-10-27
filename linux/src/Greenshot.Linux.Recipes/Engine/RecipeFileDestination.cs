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

#nullable disable

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Naming;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Recipes.Contracts;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// The file formats a recipe can save in (Format of the Destinations and SaveFile steps)
    /// </summary>
    public sealed class SaveableFileFormatIds : IAllowedValuesProvider
    {
        public IReadOnlyList<string> GetAllowedValues() => ImageFormats.Supported.Concat(["jpeg"]).ToList();
    }

    /// <summary>
    /// Save to a file with what the Destinations or SaveFile step set for the flow: SaveDirectory with FilenamePattern,
    /// Format, JpegQuality, AllowOverwrite and CopyPathToClipboard; the rest comes from the settings
    /// </summary>
    public sealed class RecipeFileDestination(CaptureFlowContext context) : IDestination
    {
        public const string FilenameProperty = "Destination.Filename";
        public const string FormatProperty = "Destination.Format";
        public const string JpegQualityProperty = "Destination.JpegQuality";
        public const string AllowOverwriteProperty = "Destination.AllowOverwrite";
        public const string CopyPathProperty = "Destination.CopyPathToClipboard";

        public static bool HasOverrides(CaptureFlowContext context) =>
            new[] { FilenameProperty, FormatProperty, JpegQualityProperty, AllowOverwriteProperty, CopyPathProperty }.Any(context.Properties.ContainsKey);

        public string Designation => WellKnownDestinations.FileDefault;

        public string DisplayName => Greenshot.Linux.Core.Languages.Texts.Editor("save");

        public int Priority => 0;

        public bool IsAvailable => true;

        public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
        {
            var settings = RecipeServices.Settings;
            string format = context.Properties.TryGetValue(FormatProperty, out var f) && f is string fs ? ImageFormats.Normalize(fs) : ImageFormats.Normalize(settings.OutputFileFormat);
            string path = context.Properties.TryGetValue(FilenameProperty, out var p) && p is string ps ? ps : FileDestination.GetDefaultPath(settings, capture.Details);
            if (!path.EndsWith("." + format, StringComparison.OrdinalIgnoreCase))
            {
                path = Path.ChangeExtension(path, format);
            }

            bool allowOverwrite = context.Properties.TryGetValue(AllowOverwriteProperty, out var o) && o is bool ob ? ob : settings.OutputFileAllowOverwrite;
            if (File.Exists(path) && !allowOverwrite)
            {
                // Unattended: a new name instead of the dialog
                string folder = Path.GetDirectoryName(path)!, name = Path.GetFileNameWithoutExtension(path), extension = Path.GetExtension(path);
                for (int i = 1; File.Exists(path); i++)
                {
                    path = Path.Combine(folder, $"{name} ({i}){extension}");
                }
            }

            int quality = context.Properties.TryGetValue(JpegQualityProperty, out var q) && q is int qi ? qi : settings.OutputFileJpegQuality;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllBytesAsync(path, ImageFormats.Encode(capture.Image, format, quality), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ExportResult.Failed(Greenshot.Linux.Core.Languages.Texts.Core("error_nowriteaccess", path) + "\n" + ex.Message);
            }

            bool copyPath = context.Properties.TryGetValue(CopyPathProperty, out var c) && c is bool cb ? cb : settings.OutputFileCopyPathToClipboard;
            if (copyPath && SystemClipboard.IsAvailable)
            {
                await SystemClipboard.SetTextAsync(path).ConfigureAwait(false);
            }

            return ExportResult.Done(path);
        }

        /// <summary>
        /// The path for SaveDirectory with the pattern (variables filled), the format's extension
        /// </summary>
        public static string BuildPath(string directory, string pattern, string format, CaptureDetails details)
        {
            var names = FilenamePattern.ForSettings(RecipeServices.Settings);
            string folder = names.Fill(directory, details, filenameSafe: false);
            if (folder.StartsWith("~/", StringComparison.Ordinal))
            {
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), folder[2..]);
            }

            return Path.Combine(folder, names.GetFilename(pattern, format, details));
        }
    }
}
