// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Diagnostics;
using System.Windows.Media;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Toasts.ViewModels;
using Dapplo.Log;
using Dapplo.Windows.Extensions;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Resources;

namespace Greenshot.Addons.ViewModels
{
    /// <inheritdoc />
    public class ExportNotificationViewModel : ToastBaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly Config<IConfigScreen> _configViewModel;
        private readonly IConfigScreen _configScreen;
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="source">IDestination</param>
        /// <param name="exportInformation">ExportInformation</param>
        /// <param name="exportedSurface">ISurface</param>
        /// <param name="windowManager">IWindowManager</param>
        /// <param name="configViewModel">Config</param>
        /// <param name="configScreen">IConfigScreen</param>
        public ExportNotificationViewModel(
            IDestination source,
            ExportInformation exportInformation,
            ISurface exportedSurface,
            IWindowManager windowManager,
            Config<IConfigScreen> configViewModel,
            IConfigScreen configScreen = null)
        {
            _windowManager = windowManager;
            _configViewModel = configViewModel;
            _configScreen = configScreen;
            Information = exportInformation;
            Source = source;

            using (var bitmap = exportedSurface.GetBitmapForExport())
            {
                ExportBitmapSource = bitmap.NativeBitmap.ToBitmapSource();
            }
        }

        /// <summary>
        /// The greenshot icon
        /// </summary>
        public ImageSource GreenshotIcon => GreenshotResources.Instance.GreenshotIconAsBitmapSource();

        /// <summary>
        /// The export as ImageSource
        /// </summary>
        public ImageSource ExportBitmapSource { get; }

        /// <summary>
        /// Which destination exported this?
        /// </summary>
        public IDestination Source { get; }

        /// <summary>
        /// Information on the export
        /// </summary>
        public ExportInformation Information { get; }

        /// <summary>
        /// Can we configure this?
        /// </summary>
        public bool CanConfigure => _configScreen != null;

        /// <summary>
        /// Trigger the configuration
        /// </summary>
        public void Configure()
        {
            if (!CanConfigure)
            {
                return;
            }

            _configViewModel.CurrentConfigScreen = _configScreen;
            if (!_configViewModel.IsActive)
            {
                _windowManager.ShowDialog(_configViewModel);
            }
        }

        /// <summary>
        /// Handle the click
        /// </summary>
        public void OpenExport()
        {
            try
            {
                if (Information.IsFileExport)
                {
                    ExplorerHelper.OpenInExplorer(Information.Filepath);
                    return;
                }

                if (Information.IsCloudExport)
                {
                    var processStartInfo = new ProcessStartInfo(Information.Uri)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                }
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex,"While opening {0}", Information.Uri);
            }
            finally
            {
                Close();
            }
        }
    }
}
