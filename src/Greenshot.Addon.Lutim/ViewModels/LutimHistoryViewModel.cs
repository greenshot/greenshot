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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.Log;
using Dapplo.Windows.Clipboard;
using Greenshot.Addon.Lutim.Configuration;
using Greenshot.Addon.Lutim.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Extensions;

namespace Greenshot.Addon.Lutim.ViewModels
{
    /// <summary>
    /// The view model for the Lutim history
    /// </summary>
    public sealed class LutimHistoryViewModel : Screen
    {
        private static readonly LogSource Log = new LogSource();
        private readonly LutimApi _lutimApi;

        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provide ILutimConfiguration to the view
        /// </summary>
        public ILutimConfiguration LutimConfiguration { get; set; }

        /// <summary>
        /// Provide ILutimLanguage to the view
        /// </summary>
        public ILutimLanguage LutimLanguage { get; set; }

        /// <summary>
        /// Provide IGreenshotLanguage to the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; set; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="lutimConfiguration">ILutimConfiguration</param>
        /// <param name="lutimLanguage">ILutimLanguage</param>
        /// <param name="lutimApi">LutimApi</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        public LutimHistoryViewModel(
            ILutimConfiguration lutimConfiguration,
            ILutimLanguage lutimLanguage,
            LutimApi lutimApi,
            IGreenshotLanguage greenshotLanguage)
        {
            _lutimApi = lutimApi;
            LutimConfiguration = lutimConfiguration;
            LutimLanguage = lutimLanguage;
            GreenshotLanguage = greenshotLanguage;
        }
        /// <summary>
        /// The list of Lutim items
        /// </summary>
        public IList<LutimInfo> LutimHistory { get; } = new BindableCollection<LutimInfo>();

        /// <inheritdoc />
        protected override void OnActivate()
        {
             // Prepare disposables
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            // automatically update the DisplayName
            var lutimHistoryLanguageBinding = LutimLanguage.CreateDisplayNameBinding(this, nameof(ILutimLanguage.History));

            // Make sure the greenshotLanguageBinding is disposed when this is no longer active
            _disposables.Add(lutimHistoryLanguageBinding);
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        /// <summary>
        ///     Load the complete history of the Lutim uploads, with the corresponding information
        /// </summary>
        private async Task LoadHistory(CancellationToken cancellationToken = default)
        {
            bool saveNeeded = false;

            // Load the ImUr history
            foreach (string hash in LutimConfiguration.LutimUploadHistory.Keys)
            {
                if (LutimConfiguration.RuntimeLutimHistory.ContainsKey(hash))
                {
                    // Already loaded, only add it to the view
                    LutimHistory.Add(LutimConfiguration.RuntimeLutimHistory[hash]);
                    continue;
                }
                try
                {
                    var lutimInfo = await _lutimApi.RetrieveLutimInfoAsync(hash, LutimConfiguration.LutimUploadHistory[hash], cancellationToken);
                    if (lutimInfo != null)
                    {
                        await _lutimApi.RetrieveLutimThumbnailAsync(lutimInfo, cancellationToken);
                        LutimConfiguration.RuntimeLutimHistory.Add(hash, lutimInfo);
                        // Already loaded, only add it to the view
                        LutimHistory.Add(lutimInfo);
                    }
                    else
                    {
                        Log.Debug().WriteLine("Deleting not found Lutim {0} from config.", hash);
                        LutimConfiguration.LutimUploadHistory.Remove(hash);
                        saveNeeded = true;
                    }
                }
                catch (Exception e)
                {
                    Log.Error().WriteLine(e, "Problem loading Lutim history for hash {0}", hash);
                }
            }
            if (saveNeeded)
            {
                // Save needed changes
                // IniConfig.Save();
            }
        }

        /// <summary>
        /// The currently selected Lutim entry
        /// </summary>
        public AddResult SelectedLutim { get; private set; }

        /// <summary>
        /// Is it possible to delete
        /// </summary>
        public bool CanDelete => true;

        /// <summary>
        /// Delete the current selected Lutim
        /// </summary>
        /// <returns>Task</returns>
        public async Task Delete()
        {
            await _lutimApi.DeleteLutimImage(SelectedLutim);
        }

        /// <summary>
        /// Can the current selected Lutim entry be copied to the clipboard
        /// </summary>
        public bool CanCopyToClipboard => true;

        /// <summary>
        /// Copy current selected Lutim entry to the clipboard
        /// </summary>
        public void CopyToClipboard()
        {
            // TODO: Build url
            using (var clipboardAccessToken = ClipboardNative.Access())
            {
                clipboardAccessToken.ClearContents();
                clipboardAccessToken.SetAsUrl(SelectedLutim.LutimInfo.Short);
            }
        }

        /// <summary>
        /// Clear the whole history
        /// </summary>
        public void ClearHistory()
        {
            LutimConfiguration.RuntimeLutimHistory.Clear();
            LutimConfiguration.LutimUploadHistory.Clear();
            LutimHistory.Clear();
        }

        /// <summary>
        /// Show the current selected Lutim entry in the browser
        /// </summary>
        public void Show()
        {
            var link = SelectedLutim?.LutimInfo.Short;
            if (link != null)
            {
                Process.Start(link);
            }
        }
    }
}
