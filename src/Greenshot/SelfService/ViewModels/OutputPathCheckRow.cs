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

using Greenshot.Base.Languages;

namespace Greenshot.SelfService.ViewModels
{
    /// <summary>
    /// One line of the output folder check in the Files section
    /// </summary>
    public sealed class OutputPathCheckRow
    {
        public OutputPathCheckRow(OutputPathCheckItem item)
        {
            Icon = item.Severity switch
            {
                OutputPathSeverity.Ok => "✅",
                OutputPathSeverity.Info => "ℹ️",
                OutputPathSeverity.Warning => "⚠️",
                _ => "❌"
            };
            Text = CreateText(item);
        }

        public string Icon { get; }
        public string Text { get; }

        private static string CreateText(OutputPathCheckItem item)
        {
            var texts = Texts.SelfService;
            string detail = item.Detail ?? string.Empty;
            string text = item.Finding switch
            {
                OutputPathFinding.Ok => texts.FilesOutputOk,
                OutputPathFinding.NotConfigured => texts.FilesOutputNotConfigured,
                OutputPathFinding.ContainsVariables => texts.FilesOutputContainsVariables,
                OutputPathFinding.Missing => texts.FilesOutputMissing,
                OutputPathFinding.NotWritable => string.Format(texts.FilesOutputNotWritable, detail),
                OutputPathFinding.Network => texts.FilesOutputNetwork,
                OutputPathFinding.OneDrive => texts.FilesOutputOnedrive,
                OutputPathFinding.TooLong => string.Format(texts.FilesOutputTooLong, detail, OutputPathCheck.MaxPathLength),
                OutputPathFinding.InvalidFilename => string.Format(texts.FilesOutputInvalidFilename, detail),
                OutputPathFinding.PatternCreatesFolders => texts.FilesOutputPatternFolders,
                _ => item.Finding.ToString()
            };
            return string.IsNullOrEmpty(text) ? item.Finding.ToString() : text;
        }
    }
}
