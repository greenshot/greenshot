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

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using Dapplo.Ini;
using Dapplo.Log;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Forms;
using Greenshot.Helpers;
using Application = System.Windows.Application;

#endregion

namespace Greenshot.Components
{
    /// <summary>
    /// This startup action starts the Greenshot "server", which allows to open files etc.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    [StartupOrder((int)GreenshotStartupOrder.Server)]
    public class GreenshotServerAction : IGreenshotContract, IStartupAsync, IShutdownAsync
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly MainForm _mainForm;
        private readonly HotkeyHandler _hotkeyHandler;
        private readonly IEnumerable<IDestination> _destinations;
        private ServiceHost _host;

        public static string Identity
        {
            get
            {
                var windowsIdentity = WindowsIdentity.GetCurrent();
                return windowsIdentity.User?.ToString();
            }
        }
        public static string EndPoint => $"net.pipe://localhost/Greenshot/Greenshot_{Identity}";

        public GreenshotServerAction(ICoreConfiguration coreConfiguration, MainForm mainForm, HotkeyHandler hotkeyHandler, IEnumerable<IDestination> destinations)
        {
            _coreConfiguration = coreConfiguration;
            _mainForm = mainForm;
            _hotkeyHandler = hotkeyHandler;
            _destinations = destinations;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            Log.Debug().WriteLine("Starting Greenshot server");
            await Task.Factory.StartNew(() => {
                _host = new ServiceHost(this, new Uri("net.pipe://localhost/Greenshot"));
                _host.AddServiceEndpoint(typeof(IGreenshotContract), new NetNamedPipeBinding(), "Greenshot_" + Identity);
                _host.Open();
            }, cancellationToken);
            Log.Debug().WriteLine("Started Greenshot server");
        }

        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            Log.Debug().WriteLine("Stopping Greenshot server");

            return Task.Factory.FromAsync((callback, stateObject) => _host.BeginClose(callback, stateObject), asyncResult => _host.EndClose(asyncResult), null);
        }

        #region IGreenshotContract

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
                IniConfig.Current?.ReloadAsync().Wait();
                _mainForm.Invoke((MethodInvoker)(() =>
                {
                    // Even update language when needed, this should be done automatically :)
                    _mainForm.UpdateUi();

                    // Make sure the current hotkeys are disabled
                    HotkeyControl.UnregisterHotkeys();
                    // and registered again (should be automated)
                    _hotkeyHandler.RegisterHotkeys(true);
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
                CaptureHelper.CaptureFile(filename);
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
            var destination = _destinations.FirstOrDefault(d => d.Designation == designation && d.IsActive);

            switch (captureMode.ToLower())
            {
                case "region":
                    CaptureHelper.CaptureRegion(false, destination);
                    break;
                case "window":
                    CaptureHelper.CaptureWindow(false, destination);
                    break;
                case "fullscreen":
                    CaptureHelper.CaptureFullscreen(false, ScreenCaptureMode.FullScreen, destination);
                    break;
                default:
                    Log.Warn().WriteLine("Unknown capture option");
                    break;
            }
        }
        #endregion
    }
}
