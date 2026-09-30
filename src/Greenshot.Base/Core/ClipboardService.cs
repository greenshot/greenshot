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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Default <see cref="IClipboardService"/>, on Dapplo.Windows.Clipboard: the clipboard is used on the calling thread,
    /// waiting for a clipboard which is in use happens asynchronously (<see cref="ClipboardAccessOptions"/>).
    /// Encoding happens before the clipboard is opened, decoding after it was closed.
    /// The UI dispatcher is only used to read virtual files (e.g. Outlook attachments), which needs OLE on the UI thread.
    /// </summary>
    public sealed class ClipboardService : IClipboardService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClipboardService));
        private readonly IUiDispatcher _ui;
        private readonly int _maxAttempts;
        private readonly TimeSpan _retryDelay;

        /// <param name="ui">Dispatcher for the UI thread, only used for virtual files on the clipboard</param>
        /// <param name="maxAttempts">Attempts when the clipboard is held by another process, default 15</param>
        /// <param name="retryDelay">Delay between the attempts, default 200 ms</param>
        public ClipboardService(IUiDispatcher ui, int maxAttempts = 15, TimeSpan? retryDelay = null)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _maxAttempts = Math.Max(1, maxAttempts);
            _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(200);
        }

        /// <summary>
        /// The registered clipboard service, or one for the registered UI dispatcher.
        /// </summary>
        public static IClipboardService Current =>
            SimpleServiceProvider.Current?.GetInstance<IClipboardService>(isOptional: true)
            ?? new ClipboardService(SimpleServiceProvider.Current?.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance);

        /// <summary>
        /// The registered clipboard service, or one which uses the supplied dispatcher (e.g. the one of a flow context).
        /// </summary>
        public static IClipboardService For(IUiDispatcher ui) =>
            SimpleServiceProvider.Current?.GetInstance<IClipboardService>(isOptional: true)
            ?? new ClipboardService(ui ?? SimpleServiceProvider.Current?.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance);

        /// <summary>
        /// The options for opening the clipboard: the retries replace the former retry loop
        /// </summary>
        private ClipboardAccessOptions AccessOptions => ClipboardHelper.CreateAccessOptions(_maxAttempts - 1, _retryDelay);

        /// <inheritdoc />
        public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
        {
            var contents = new ClipboardContents().AddUnicodeString(text ?? string.Empty);
            return ClipboardHelper.SetClipboardDataAsync(contents, AccessOptions, cancellationToken);
        }

        /// <inheritdoc />
        public async Task SetImageAsync(Image image, IEnumerable<ClipboardFormat> formats = null, string text = null, CancellationToken cancellationToken = default)
        {
            // Encoding the formats is CPU work: done here, before the clipboard is opened
            using var content = ClipboardHelper.CreateContent(image, formats, text);
            if (!content.HasData)
            {
                return;
            }

            await ClipboardHelper.SetClipboardDataAsync(content.Contents, AccessOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Image> GetImageAsync(CancellationToken cancellationToken = default)
        {
            // Copy only what is needed in one short session, decode afterwards
            var snapshot = await ClipboardHelper.ReadSnapshotAsync(ClipboardHelper.SelectImageReadFormats(), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (snapshot == null)
            {
                return null;
            }

            // Same priority as ClipboardHelper.GetImages: image formats, virtual files, files, HTML
            Image image = ClipboardHelper.GetImageFromFormats(snapshot);
            if (image == null && HasVirtualFiles(snapshot))
            {
                // Virtual files need the OLE data object, which needs the UI (STA) thread
                image = IsStaThread
                    ? ClipboardHelper.GetFirstVirtualFileImage(snapshot)
                    : await _ui.InvokeAsync(() => (Image)ClipboardHelper.GetFirstVirtualFileImage(snapshot), cancellationToken).ConfigureAwait(false);
            }
            image ??= ClipboardHelper.GetFirstFileImage(snapshot);
            if (image != null)
            {
                return image;
            }

            // Only HTML with images: download them
            foreach (var imageUrl in ClipboardHelper.GetHtmlImageUrls(snapshot))
            {
                var downloaded = await NetworkHelper.DownloadImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
                if (downloaded != null)
                {
                    return downloaded;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<bool> ContainsImageAsync(CancellationToken cancellationToken = default)
        {
            if (ClipboardHelper.ContainsVirtualFiles() && !IsStaThread)
            {
                return await _ui.InvokeAsync(ClipboardHelper.ContainsImageExact, cancellationToken).ConfigureAwait(false);
            }

            return ClipboardHelper.ContainsImageExact();
        }

        private static bool IsStaThread => Thread.CurrentThread.GetApartmentState() == ApartmentState.STA;

        private static bool HasVirtualFiles(ClipboardSnapshot snapshot) =>
            snapshot.HasFormat(DataObjectReader.FileGroupDescriptorWFormat) || snapshot.HasFormat(DataObjectReader.FileGroupDescriptorFormat);
    }
}
