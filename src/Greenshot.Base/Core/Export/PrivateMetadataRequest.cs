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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using log4net;

namespace Greenshot.Base.Core.Export
{
    /// <summary>
    /// Asks whether the private details of ExportMetadata (window title, web address, text, barcodes) may go with an image which leaves this computer.
    /// Removing them is the default: Enter, Escape and closing the box remove them.
    /// </summary>
    public sealed class PrivateMetadataRequest : IDialogViewModel<bool>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PrivateMetadataRequest));
        private const int MaxValueLength = 80;

        public PrivateMetadataRequest(ExportImageMetadata details)
        {
            Details = details ?? throw new ArgumentNullException(nameof(details));
        }

        /// <summary>
        /// What would be written
        /// </summary>
        public ExportImageMetadata Details { get; }

        /// <summary>
        /// One line per private detail, long values shortened, the text only counted
        /// </summary>
        public IReadOnlyList<string> DescribeDetails()
        {
            var lines = new List<string>();
            if (Details.Title != null)
            {
                lines.Add(string.Format(Texts.Core.ExportMetadataWindowTitle, Shorten(Details.Title)));
            }
            if (Details.SourceUrl != null)
            {
                lines.Add(string.Format(Texts.Core.ExportMetadataWebAddress, Shorten(Details.SourceUrl)));
            }
            if (Details.Description != null)
            {
                int words = Details.Description.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
                lines.Add(string.Format(Texts.Core.ExportMetadataText, words));
            }
            lines.AddRange(Details.Barcodes.Select(barcode => string.Format(Texts.Core.ExportMetadataBarcodes, Shorten(barcode))));
            return lines;
        }

        private static string Shorten(string value)
        {
            string singleLine = value.Replace("\r", " ").Replace("\n", " ");
            return singleLine.Length <= MaxValueLength ? singleLine : singleLine.Substring(0, MaxValueLength - 1) + "…";
        }

        /// <summary>
        /// The view: a message box with "Remove details" (default) and "Keep details"
        /// </summary>
        /// <returns>true to keep the details</returns>
        public static bool Show(PrivateMetadataRequest request)
        {
            string message = string.Format(Texts.Core.ExportMetadataQuestion, string.Join(Environment.NewLine, request.DescribeDetails()));
            int choice = ThemedMessageBox.ShowChoice(null, Texts.Core.ExportMetadataQuestionTitle, message, MessageBoxImage.Question,
                [Texts.Core.ExportMetadataRemove, Texts.Core.ExportMetadataKeep], defaultIndex: 0, cancelIndex: 0, onTop: true);
            return choice == 1;
        }

        /// <summary>
        /// Ask the user; without a window to ask in (headless, no view registered) the details are removed
        /// </summary>
        /// <returns>true to keep the details</returns>
        public static async Task<bool> AskAsync(IUserInteraction userInteraction, ExportImageMetadata details, CancellationToken cancellationToken)
        {
            if (userInteraction == null || !userInteraction.IsInteractive)
            {
                return false;
            }
            try
            {
                bool keep = await userInteraction.ShowDialogAsync(new PrivateMetadataRequest(details), cancellationToken).ConfigureAwait(false);
                Log.InfoFormat("Private details in the exported image: {0}", keep ? "kept by the user" : "removed");
                return keep;
            }
            catch (InteractionRequiredException)
            {
                return false;
            }
            catch (InvalidOperationException ex)
            {
                // No view registered, e.g. in a tool without the shell
                Log.Warn("Can't ask about the private details, they are removed", ex);
                return false;
            }
        }
    }
}
