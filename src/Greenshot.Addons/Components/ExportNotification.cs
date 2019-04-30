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
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.Log;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addons.Components
{
    /// <summary>
    /// This is to notify the user of exports
    /// </summary>
    public class ExportNotification
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly IEventAggregator _eventAggregator;
        private readonly Func<IDestination, ExportInformation, ISurface, IConfigScreen, Owned<ExportNotificationViewModel>> _toastFactory;

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        /// <param name="toastFactory">Func to create toasts</param>
        public ExportNotification(
            ICoreConfiguration coreConfiguration,
            IEventAggregator eventAggregator,
            Func<IDestination, ExportInformation, ISurface, IConfigScreen, Owned<ExportNotificationViewModel>> toastFactory)
        {
            _coreConfiguration = coreConfiguration;
            _eventAggregator = eventAggregator;
            _toastFactory = toastFactory;
        }

        /// <summary>
        /// This takes care of creating the toast view model, publishing it, and disposing afterwards
        /// </summary>
        /// <param name="source">IDestination</param>
        /// <param name="exportInformation">ExportInformation</param>
        /// <param name="exportedSurface">ISurface</param>
        /// <param name="configScreen">IConfigScreen option to specify which IConfigScreen belongs to the destination</param>
        public void NotifyOfExport(IDestination source, ExportInformation exportInformation, ISurface exportedSurface, IConfigScreen configScreen = null)
        {
            if (exportInformation == null || !_coreConfiguration.ShowTrayNotification)
            {
                Log.Info().WriteLine("No notification due to ShowTrayNotification = {0} - or export made = {1}", _coreConfiguration.ShowTrayNotification);
                return;
            }

            if (!exportInformation.ExportMade)
            {
                if (exportInformation.IsError)
                {
                    Log.Warn().WriteLine("{0}", exportInformation.ErrorMessage);
                }
                else
                {
                    Log.Debug().WriteLine("Export to {0} cancelled.", exportInformation.DestinationDesignation);
                }

                return;
            }
            // Create the ViewModel "part"
            var message = _toastFactory(source, exportInformation, exportedSurface, configScreen);
            // Prepare to dispose the view model parts automatically if it's finished
            void DisposeHandler(object sender, DeactivationEventArgs args)
            {
                message.Value.Deactivated -= DisposeHandler;
                message.Dispose();
            }

            message.Value.Deactivated += DisposeHandler;

            // Show the ViewModel as toast 
            _eventAggregator.PublishOnCurrentThread(message.Value);
        }
    }
}
