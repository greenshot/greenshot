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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Default <see cref="IClipboardService"/>: every clipboard call runs on the UI thread, retries wait with Task.Delay in between.
    /// </summary>
    public sealed class ClipboardService : IClipboardService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClipboardService));
        private readonly IUiDispatcher _ui;
        private readonly int _maxAttempts;
        private readonly TimeSpan _retryDelay;

        /// <param name="ui">Dispatcher for the UI thread</param>
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

        public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
        {
            var dataObject = new DataObject();
            dataObject.SetData(DataFormats.Text, true, text ?? string.Empty);
            return SetDataObjectAsync(dataObject, cancellationToken);
        }

        public async Task SetImageAsync(Image image, IEnumerable<ClipboardFormat> formats = null, string text = null, CancellationToken cancellationToken = default)
        {
            // Encoding the formats is CPU work: done here, on the calling (pool) thread
            using var content = ClipboardHelper.CreateContent(image, formats, text);
            if (!content.HasData)
            {
                return;
            }

            await SetDataObjectAsync(content.DataObject, cancellationToken).ConfigureAwait(false);
        }

        private async Task SetDataObjectAsync(IDataObject dataObject, CancellationToken cancellationToken)
        {
            string lastError = null;
            for (int attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                var (success, error) = await _ui.InvokeAsync(() =>
                {
                    bool ok = ClipboardHelper.TrySetDataObjectOnce(dataObject, true, out var message);
                    return (ok, message);
                }, cancellationToken).ConfigureAwait(false);
                if (success)
                {
                    return;
                }

                lastError = error;
                if (attempt < _maxAttempts)
                {
                    await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
                }
            }

            string owner = await _ui.InvokeAsync(() => ClipboardHelper.CurrentClipboardOwner, cancellationToken).ConfigureAwait(false);
            Log.Error($"Couldn't place data on the clipboard: {lastError}");
            throw new ClipboardException(lastError, owner);
        }

        public async Task<Image> GetImageAsync(CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= Math.Min(_maxAttempts, 3); attempt++)
            {
                var (available, image, imageUrls) = await _ui.InvokeAsync(() =>
                {
                    var dataObject = ClipboardHelper.GetDataObject();
                    if (dataObject == null)
                    {
                        return (false, (Image)null, (IList<string>)null);
                    }

                    foreach (var clipboardImage in ClipboardHelper.GetImages(dataObject))
                    {
                        return (true, (Image)clipboardImage, (IList<string>)null);
                    }

                    return (true, (Image)null, ClipboardHelper.GetHtmlImageUrls(dataObject));
                }, cancellationToken).ConfigureAwait(false);
                if (!available)
                {
                    await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (image != null || imageUrls == null)
                {
                    return image;
                }

                // Only HTML with images: download them, off the UI thread
                foreach (var imageUrl in imageUrls)
                {
                    var downloaded = await NetworkHelper.DownloadImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
                    if (downloaded != null)
                    {
                        return downloaded;
                    }
                }

                return null;
            }

            return null;
        }

        public Task<bool> ContainsImageAsync(CancellationToken cancellationToken = default)
        {
            return _ui.InvokeAsync(ClipboardHelper.ContainsImage, cancellationToken);
        }
    }
}
