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
using System.Reactive.Linq;
using System.Threading;
using Autofac.Features.AttributeFilters;
using Dapplo.Addons;
using Dapplo.Log;
using Dapplo.Windows.Input.Keyboard;
using Greenshot.Addons.Core;
using Greenshot.Helpers;

namespace Greenshot.Components
{
    /// <summary>
    /// This startup action registers the hotkeys
    /// </summary>
    [Service(nameof(HotkeyService), nameof(MainFormStartup), TaskSchedulerName = "ui")]
    public class HotkeyService : IStartup, IShutdown
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly SynchronizationContext _synchronizationContext;
        private IDisposable _subscriptions;

        public HotkeyService(ICoreConfiguration coreConfiguration,
            [KeyFilter("ui")] SynchronizationContext synchronizationContext)
        {
            _coreConfiguration = coreConfiguration;
            _synchronizationContext = synchronizationContext;
        }

        public void Startup()
        {
            Log.Debug().WriteLine("Registering hotkeys");

            var syncedEvents = KeyboardHook.KeyboardEvents
                .Synchronize()
                .ObserveOn(_synchronizationContext)
                .SubscribeOn(_synchronizationContext);

            var regionHotkeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.RegionHotkey));
            _subscriptions = syncedEvents
                .Where(regionHotkeyHandler)
                .Subscribe(CaptureRegion, () => regionHotkeyHandler.Dispose());

            var fullScreenHotkeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.FullscreenHotkey));
            _subscriptions = syncedEvents
                .Where(fullScreenHotkeyHandler)
                .Subscribe(CaptureFullscreen, () => fullScreenHotkeyHandler.Dispose());

            var lastRegionHotKeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.LastregionHotkey));
            _subscriptions = syncedEvents
                .Where(lastRegionHotKeyHandler)
                .Subscribe(CaptureLast, () => lastRegionHotKeyHandler.Dispose());

            var windowHotKeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.WindowHotkey));
            _subscriptions = syncedEvents
                .Where(windowHotKeyHandler)
                .Subscribe(CaptureWindow, () => windowHotKeyHandler.Dispose());

            var ieHotKeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.IEHotkey));
            _subscriptions = syncedEvents
                .Where(ieHotKeyHandler)
                .Subscribe(CaptureIe, () => ieHotKeyHandler.Dispose());

            Log.Debug().WriteLine("Started hotkeys");
        }

        public void Shutdown()
        {
            Log.Debug().WriteLine("Stopping hotkeys");

            _subscriptions.Dispose();
        }

        private void CaptureRegion(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            CaptureHelper.CaptureRegion(true);
        }

        private void CaptureWindow(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            if (_coreConfiguration.CaptureWindowsInteractive)
            {
                CaptureHelper.CaptureWindowInteractive(true);
            }
            else
            {
                CaptureHelper.CaptureWindow(true);
            }
        }

        private void CaptureFullscreen(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            CaptureHelper.CaptureFullscreen(true, _coreConfiguration.ScreenCaptureMode);
        }

        private void CaptureLast(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            CaptureHelper.CaptureLastRegion(true);
        }

        private void CaptureIe(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            if (_coreConfiguration.IECapture)
            {
                CaptureHelper.CaptureIe(true, null);
            }
        }
    }
}
