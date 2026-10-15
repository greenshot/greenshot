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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.SelfService.Imgur;
using log4net;

namespace Greenshot.SelfService.ViewModels
{
    /// <summary>
    /// The images the user uploaded anonymously to Imgur with earlier versions of Greenshot, so they can be deleted.
    /// Greenshot no longer uploads to Imgur, this section is only there while there are uploads.
    /// Used by the Self-Service window and the welcome wizard (through ImgurUploadsView), on the UI thread.
    /// </summary>
    public class ImgurSectionViewModel : SelfServiceSectionViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImgurSectionViewModel));
        private bool _detailsRequested;

        public ImgurSectionViewModel()
        {
            foreach (var upload in Config.ImgurUploadHistory ?? new Dictionary<string, string>())
            {
                Uploads.Add(new ImgurUploadViewModel(upload.Key, upload.Value));
            }
        }

        private static IImgurConfiguration Config => IniConfigHelper.EnsureSection<IImgurConfiguration>(() => new ImgurConfigurationImpl());

        /// <summary>
        /// True when greenshot.ini still has uploads, only then the section (or wizard step) is shown.
        /// </summary>
        public static bool HasUploads => Config.ImgurUploadHistory?.Count > 0;

        public ObservableCollection<ImgurUploadViewModel> Uploads { get; } = new ObservableCollection<ImgurUploadViewModel>();

        public override string Id => "imgur";
        public override string Title => Texts.SelfService.CategoryImgur;
        public override string Subtitle => Texts.SelfService.CategoryImgurSub;
        public override string Icon => "🖼️";

        /// <summary>
        /// Get the titles, dates and thumbnails from Imgur the first time the uploads are shown.
        /// </summary>
        public override void OnNavigatedTo()
        {
            if (_detailsRequested)
            {
                return;
            }

            _detailsRequested = true;
            LoadDetailsAsync().FireAndLog("Load the Imgur upload details", Log);
        }

        private async Task LoadDetailsAsync()
        {
            foreach (var upload in Uploads.ToList())
            {
                try
                {
                    var details = await ImgurApi.GetDetailsAsync(upload.Hash, CancellationToken.None).ConfigureAwait(false);
                    if (details == null)
                    {
                        Log.InfoFormat("Imgur no longer has {0}, removing it from the uploads", upload.Hash);
                        await UiDispatcher.Current.InvokeAsync(() => Forget(upload)).ConfigureAwait(false);
                        continue;
                    }

                    var thumbnail = await ImgurApi.GetThumbnailAsync(upload.Hash, CancellationToken.None).ConfigureAwait(false);
                    await UiDispatcher.Current.InvokeAsync(() =>
                    {
                        upload.Title = details.Value.Title;
                        upload.Uploaded = details.Value.Uploaded;
                        upload.Thumbnail = thumbnail;
                    }).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Offline, or Imgur doesn't accept the client id: the upload stays, with its link
                    Log.Warn($"Couldn't get the details of Imgur upload {upload.Hash}", ex);
                }
            }
        }

        /// <summary>
        /// Delete the uploads on Imgur, each one which is deleted is removed from the list.
        /// </summary>
        public async Task DeleteAsync(IReadOnlyList<ImgurUploadViewModel> uploads)
        {
            foreach (var upload in uploads)
            {
                try
                {
                    await UserInteraction.Current.RunWithProgressAsync("Imgur", (progress, token) => ImgurApi.DeleteAsync(upload.DeleteHash, token), CancellationToken.None);
                    Forget(upload);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.Warn($"Couldn't delete Imgur upload {upload.Hash}", ex);
                }
            }
        }

        /// <summary>
        /// Write the list as a web page, so the user keeps the links (and the delete links) outside of Greenshot.
        /// </summary>
        public void Export(string path)
        {
            string html = ImgurApi.CreateExport(Uploads.Select(upload => (upload.Hash, upload.DeleteHash, upload.Title, upload.Uploaded)),
                Texts.SelfService.ImgurTitle, Texts.SelfService.ImgurSub, Texts.SelfService.ImgurExportDelete);
            File.WriteAllText(path, html, Encoding.UTF8);
        }

        /// <summary>
        /// Remove all uploads from greenshot.ini, Greenshot can't delete them afterwards.
        /// </summary>
        public void ForgetAll()
        {
            foreach (var upload in Uploads.ToList())
            {
                Forget(upload);
            }
        }

        private void Forget(ImgurUploadViewModel upload)
        {
            Uploads.Remove(upload);
            Config.ImgurUploadHistory?.Remove(upload.Hash);
        }
    }
}
