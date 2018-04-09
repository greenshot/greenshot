#region Greenshot GNU General Public License

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

#endregion

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.Log;
using Greenshot.Addon.Imgur.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Core;

namespace Greenshot.Addon.Imgur.ViewModels
{
    [Export]
    public sealed class ImgurHistoryViewModel : Screen
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        [Import]
        public IImgurConfiguration ImgurConfiguration { get; set; }


        [Import]
        public ImgurDestination ImgurDestination { get; set; }

        [Import]
        public IImgurLanguage ImgurLanguage { get; set; }

        [Import]
        public IGreenshotLanguage GreenshotLanguage { get; set; }

        /// <summary>
        /// The list of imgur items
        /// </summary>
        public ObservableCollection<ImgurImage> ImgurHistory { get; } = new BindableCollection<ImgurImage>();

        protected override void OnActivate()
        {
             // Prepare disposables
            _disposables?.Dispose();
            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                ImgurLanguage.CreateDisplayNameBinding(this, nameof(IImgurLanguage.History))
            };
            LoadHistory();
        }

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
            foreach (string hash in ImgurConfiguration.ImgurUploadHistory.Keys)
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
                    var imgurInfo = await ImgurDestination.RetrieveImgurInfoAsync(hash, ImgurConfiguration.ImgurUploadHistory[hash], cancellationToken).ConfigureAwait(true);
                    if (imgurInfo != null)
                    {
                        await ImgurDestination.RetrieveImgurThumbnailAsync(imgurInfo, cancellationToken).ConfigureAwait(true);
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

        public ImgurImage SelectedImgur { get; private set; }

        public bool CanDelete => true;

        public async Task Delete()
        {
            await ImgurDestination.DeleteImgurImageAsync(SelectedImgur).ConfigureAwait(true);
        }

        public bool CanCopyToClipboard => true;

        public void CopyToClipboard()
        {
            ClipboardHelper.SetClipboardData(SelectedImgur.Data.Link?.AbsoluteUri);
        }

        public void ClearHistory()
        {
            ImgurConfiguration.RuntimeImgurHistory.Clear();
            ImgurConfiguration.ImgurUploadHistory.Clear();
            ImgurHistory.Clear();
        }

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
