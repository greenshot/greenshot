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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Dapplo.CaliburnMicro;
using Dapplo.Log;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

namespace Greenshot.Components
{
    /// <summary>
    /// This startup loads / initialized all the plugins
    /// </summary>
    [UiStartupAction(StartupOrder = (int)GreenshotUiStartupOrder.Plugins), UiShutdownAction]
    public class PluginLoader : IUiStartupAction, IUiShutdownAction
    {
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly IEnumerable<IGreenshotPlugin> _plugins;
        private readonly IEnumerable<IDestination> _destinations;
        private readonly IEnumerable<IProcessor> _processors;
        private static readonly LogSource Log = new LogSource();

        [ImportingConstructor]
        public PluginLoader(ICoreConfiguration coreConfiguration,
            [ImportMany] IEnumerable<IGreenshotPlugin> plugins,
            [ImportMany] IEnumerable<IDestination> destinations,
            [ImportMany] IEnumerable<IProcessor> processors)
        {
            _coreConfiguration = coreConfiguration;
            _plugins = plugins;
            _destinations = destinations;
            _processors = processors;
        }

        public void Start()
        {
            Log.Debug().WriteLine("Starting plugins");

            foreach (var greenshotPlugin in _plugins)
            {
                greenshotPlugin.Initialize();
            }

            // Check destinations, remove all that don't exist
            foreach (var destination in _coreConfiguration.OutputDestinations.ToArray())
            {
                if (_destinations.FirstOrDefault(p => p.Designation == destination && p.IsActive) == null)
                {
                    _coreConfiguration.OutputDestinations.Remove(destination);
                }
            }

            // we should have at least one!
            if (_coreConfiguration.OutputDestinations.Count == 0)
            {
                _coreConfiguration.OutputDestinations.Add("Editor");
            }

            Log.Debug().WriteLine("Started plugins");
        }

        public void Shutdown()
        {
            Log.Debug().WriteLine("Stopping plugins");

            foreach (var greenshotPlugin in _plugins)
            {
                greenshotPlugin.Shutdown();
            }
        }

        /// <summary>
        ///     A simple helper method which will call ProcessCapture for the Processor with the specified designation
        /// </summary>
        /// <param name="designation">Name of the processor</param>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        public void ProcessCapture(string designation, ISurface surface, ICaptureDetails captureDetails)
        {
            _processors.FirstOrDefault(p => p.Designation == designation && p.IsActive)?.ProcessCapture(surface, captureDetails);
        }

        /// <summary>
        ///     A simple helper method which will call ExportCapture for the destination with the specified designation
        /// </summary>
        /// <param name="manuallyInitiated"></param>
        /// <param name="designation"></param>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        public ExportInformation ExportCapture(bool manuallyInitiated, string designation, ISurface surface, ICaptureDetails captureDetails)
        {
            return _destinations.FirstOrDefault(p => p.Designation == designation && p.IsActive)?.ExportCapture(manuallyInitiated, surface, captureDetails);
        }
    }
}