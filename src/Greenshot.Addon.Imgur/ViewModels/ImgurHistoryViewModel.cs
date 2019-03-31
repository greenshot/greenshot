// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.Log;
using Dapplo.Windows.Clipboard;
using Greenshot.Addon.Imgur.Configuration;
using Greenshot.Addon.Imgur.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Extensions;

namespace Greenshot.Addon.Imgur.ViewModels
{
    /// <summary>
    /// This is the view model for the history
    /// </summary>
    public sealed class ImgurHistoryViewModel : Screen
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ImgurApi _imgurApi;

        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// The configuration used in the view
        /// </summary>
        public IImgurConfiguration ImgurConfiguration { get; }


        /// <summary>
        /// The translations used in the view
        /// </summary>
        public IImgurLanguage ImgurLanguage { get; }

        /// <summary>
        /// Used from the View
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// Constructor which accepts the dependencies for this class
        /// </summary>
        /// <param name="imgurConfiguration">IImgurConfiguration</param>
        /// <param name="imgurApi">ImgurApi</param>
        /// <param name="imgurLanguage">IImgurLanguage</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        public ImgurHistoryViewModel(
            IImgurConfiguration imgurConfiguration,
            ImgurApi imgurApi,
            IImgurLanguage imgurLanguage,
            IGreenshotLanguage greenshotLanguage
            )
        {
            ImgurConfiguration = imgurConfiguration;
            _imgurApi = imgurApi;
            ImgurLanguage = imgurLanguage;
            GreenshotLanguage = greenshotLanguage;
        }
        /// <summary>
        /// The list of imgur items
        /// </summary>
        public ObservableCollection<ImgurImage> ImgurHistory { get; } = new BindableCollection<ImgurImage>();

        /// <inheritdoc />
        protected override void OnActivate()
        {
             // Prepare disposables
            _disposables?.Dispose();
            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                ImgurLanguage.CreateDisplayNameBinding(this, nameof(IImgurLanguage.History))
            };
            _ = LoadHistory();
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        /// <summary>
        ///     Load the complete history of the imgur uploads, with the corresponding information
        /// </summary>
        private async Task LoadHistory(CancellationToken cancellationToken = default)
        {
            // Load the ImUr history
            foreach (var hash in ImgurConfiguration.ImgurUploadHistory.Keys)
            {
                if (ImgurHistory.Any(imgurInfo => imgurInfo.Data.Id == hash))
                {
                    continue;
                }
                if (ImgurConfiguration.RuntimeImgurHistory.ContainsKey(hash))
                {
                    // Already loaded, only add it to the view
                    ImgurHistory.Add(ImgurConfiguration.RuntimeImgurHistory[hash]);
                    continue;
                }
                try
                {
                    var imgurInfo = await _imgurApi.RetrieveImgurInfoAsync(hash, ImgurConfiguration.ImgurUploadHistory[hash], cancellationToken).ConfigureAwait(true);
                    if (imgurInfo != null)
                    {
                        await _imgurApi.RetrieveImgurThumbnailAsync(imgurInfo, cancellationToken).ConfigureAwait(true);
                        ImgurConfiguration.RuntimeImgurHistory.Add(hash, imgurInfo);
                        // Already loaded, only add it to the view
                        ImgurHistory.Add(imgurInfo);
                    }
                    else
                    {
                        Log.Debug().WriteLine("Deleting not found ImgUr {0} from config.", hash);
                        ImgurConfiguration.ImgurUploadHistory.Remove(hash);
                    }
                }
                catch (Exception e)
                {
                    Log.Error().WriteLine(e, "Problem loading ImgUr history for hash {0}", hash);
                }
            }
        }

        /// <summary>
        /// The selected Imgur entry
        /// </summary>
        public ImgurImage SelectedImgur { get; private set; }

        /// <summary>
        /// Used from the View
        /// </summary>
        public bool CanDelete => true;

        /// <summary>
        /// Used from the View
        /// </summary>
        public async Task Delete()
        {
            await _imgurApi.DeleteImgurImageAsync(SelectedImgur).ConfigureAwait(true);
        }

        /// <summary>
        /// Used from the View
        /// </summary>
        public bool CanCopyToClipboard => true;

        /// <summary>
        /// Used from the View
        /// </summary>
        public void CopyToClipboard()
        {
            using (var clipboardAccessToken = ClipboardNative.Access())
            {
                clipboardAccessToken.ClearContents();
                clipboardAccessToken.SetAsUrl(SelectedImgur.Data.Link?.AbsoluteUri);
            }
        }

        /// <summary>
        /// Used from the View
        /// </summary>
        public void ClearHistory()
        {
            ImgurConfiguration.RuntimeImgurHistory.Clear();
            ImgurConfiguration.ImgurUploadHistory.Clear();
            ImgurHistory.Clear();
        }

        /// <summary>
        /// Used from the View
        /// </summary>
        public void Show()
        {
            var link = SelectedImgur.Data.Link?.AbsoluteUri;
            if (link != null)
            {
                Process.Start(link);
            }
        }
    }
}
