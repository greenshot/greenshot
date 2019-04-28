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

#if !NETCOREAPP3_0

using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using Dapplo.Log;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Core.Enums;
using Greenshot.Forms;
using Greenshot.Helpers;
using Application = System.Windows.Application;

namespace Greenshot.Components
{
    /// <summary>
    /// This startup action starts the Greenshot "server", which allows to open files etc.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    [Service(nameof(GreenshotServerAction), nameof(MainFormStartup))]
    public class GreenshotServerAction : IGreenshotContract, IStartupAsync, IShutdownAsync
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly MainForm _mainForm;
        private readonly HotkeyService _hotkeyService;
        private readonly DestinationHolder _destinationHolder;
        private ServiceHost _host;
        private CaptureSupportInfo _captureSupportInfo;

        public static string Identity
        {
            get
            {
                var windowsIdentity = WindowsIdentity.GetCurrent();
                return windowsIdentity.User?.ToString();
            }
        }
        public static string EndPoint => $"net.pipe://localhost/Greenshot/Greenshot_{Identity}";

        public GreenshotServerAction(
            ICoreConfiguration coreConfiguration,
            MainForm mainForm,
            HotkeyService hotkeyService,
            DestinationHolder destinationHolder,
            CaptureSupportInfo captureSupportInfo)
        {
            _captureSupportInfo = captureSupportInfo;
            _coreConfiguration = coreConfiguration;
            _mainForm = mainForm;
            _hotkeyService = hotkeyService;
            _destinationHolder = destinationHolder;
        }

        /// <inheritdoc />
        public Task StartupAsync(CancellationToken cancellationToken = default)
        {
            // TODO: Test performance with Startup without async
            Log.Debug().WriteLine("Starting Greenshot server");
            return Task.Run(() => {
                _host = new ServiceHost(this, new Uri("net.pipe://localhost/Greenshot"));
                _host.AddServiceEndpoint(typeof(IGreenshotContract), new NetNamedPipeBinding(), "Greenshot_" + Identity);
                _host.Open();
                Log.Debug().WriteLine("Started Greenshot server");
            }, cancellationToken);
        }

        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            Log.Debug().WriteLine("Stopping Greenshot server");

            return Task.Factory.FromAsync((callback, stateObject) => _host.BeginClose(callback, stateObject), asyncResult => _host.EndClose(asyncResult), null);
        }

        /// <inheritdoc />
        public void Exit()
        {
            Application.Current.Shutdown();
        }

        /// <inheritdoc />
        public void ReloadConfig()
        {
            Log.Info().WriteLine("Reload requested");
            try
            {
                // TODO: Fix
                //IniConfig.Current?.ReloadAsync().Wait();
                _mainForm.Invoke((MethodInvoker)(() =>
                {
                    // Even update language when needed, this should be done automatically :)
                    _mainForm.UpdateUi();
                }));
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex, "Exception while reloading configuration: ");
            }
        }

        /// <inheritdoc />
        public void OpenFile(string filename)
        {
            Log.Debug().WriteLine("Open file requested: {0}", filename);
            if (File.Exists(filename))
            {
                CaptureHelper.CaptureFile(_captureSupportInfo, filename);
            }
            else
            {
                Log.Warn().WriteLine("No such file: {0}", filename);
            }
        }

        /// <inheritdoc />
        public void Capture(string parameters)
        {

            if (MainForm.Instance.InvokeRequired)
            {
                MainForm.Instance.Invoke((MethodInvoker)(() => Capture(parameters)));
                return;
            }

            Log.Info().WriteLine("Capture requested: {0}", parameters);

            string[] optionsArray = parameters.Split(',');
            string captureMode = optionsArray[0];
            // Fallback-Destination

            var designation = _coreConfiguration.OutputDestinations.FirstOrDefault();
            var destination = _destinationHolder.SortedActiveDestinations.FirstOrDefault(d => d.Designation == designation);

            switch (captureMode.ToLower())
            {
                case "region":
                    CaptureHelper.CaptureRegion(_captureSupportInfo, false, destination);
                    break;
                case "window":
                    CaptureHelper.CaptureWindow(_captureSupportInfo, false, destination);
                    break;
                case "fullscreen":
                    CaptureHelper.CaptureFullscreen(_captureSupportInfo, false, ScreenCaptureMode.FullScreen, destination);
                    break;
                default:
                    Log.Warn().WriteLine("Unknown capture option");
                    break;
            }
        }
    }
}
#endif