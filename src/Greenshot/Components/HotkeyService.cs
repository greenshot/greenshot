﻿#region Greenshot GNU General Public License

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
using System.Collections.Generic;
using System.Reactive.Linq;
using Caliburn.Micro;
using Dapplo.Addons;
using Dapplo.Log;
using Dapplo.Windows.Input.Keyboard;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
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
        private readonly IEnumerable<IFormEnhancer> _formEnhancers;
        private IDisposable _subscriptions;

        public HotkeyService(ICoreConfiguration coreConfiguration, IEnumerable<IFormEnhancer> formEnhancers = null)
        {
            _coreConfiguration = coreConfiguration;
            _formEnhancers = formEnhancers;
        }

        /// <summary>
        /// If set to false, hotkey presses don't trigger the functionality
        /// </summary>
        public bool AreHotkeysActive { get; set; } = true;

        /// <summary>
        /// This can be used to disable or enable the triggering of hotkeys, depending in different logic
        /// </summary>
        /// <param name="keyboardHookEventArgs">KeyboardHookEventArgs</param>
        /// <returns>bool</returns>
        private bool HotkeyTrigger(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            return AreHotkeysActive;
        }

        /// <inheritdoc />
        public void Startup()
        {
            Log.Debug().WriteLine("Registering hotkeys");

            var syncedEvents = KeyboardHook.KeyboardEvents;

            // Region hotkey
            var regionHotkeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.RegionHotkey));
            _subscriptions = syncedEvents
                .Where(regionHotkeyHandler)
                .Where(HotkeyTrigger)
                .Subscribe(CaptureRegion, () => regionHotkeyHandler.Dispose());

            // Fullscreen hotkey
            var fullScreenHotkeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.FullscreenHotkey));
            _subscriptions = syncedEvents
                .Where(fullScreenHotkeyHandler)
                .Where(HotkeyTrigger)
                .Subscribe(CaptureFullscreen, () => fullScreenHotkeyHandler.Dispose());

            // Last region hotkey
            var lastRegionHotKeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.LastregionHotkey));
            _subscriptions = syncedEvents
                .Where(lastRegionHotKeyHandler)
                .Where(HotkeyTrigger)
                .Subscribe(CaptureLast, () => lastRegionHotKeyHandler.Dispose());

            // Window hotkey
            var windowHotKeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.WindowHotkey));
            _subscriptions = syncedEvents
                .Where(windowHotKeyHandler)
                .Where(HotkeyTrigger)
                .Subscribe(CaptureWindow, () => windowHotKeyHandler.Dispose());

            // IE hotkey
            var ieHotKeyHandler = new HotKeyHandler(_coreConfiguration, nameof(ICoreConfiguration.IEHotkey));
            _subscriptions = syncedEvents
                .Where(ieHotKeyHandler)
                .Where(HotkeyTrigger)
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
            Execute.BeginOnUIThread(() => CaptureHelper.CaptureRegion(_formEnhancers, true));
        }

        private void CaptureWindow(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            if (_coreConfiguration.CaptureWindowsInteractive)
            {
                Execute.BeginOnUIThread(() => CaptureHelper.CaptureWindowInteractive(_formEnhancers, true));
            }
            else
            {
                Execute.BeginOnUIThread(() => CaptureHelper.CaptureWindow(_formEnhancers, true));
            }
        }

        private void CaptureFullscreen(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            Execute.BeginOnUIThread(() => CaptureHelper.CaptureFullscreen(_formEnhancers, true, _coreConfiguration.ScreenCaptureMode));
        }

        private void CaptureLast(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            Execute.BeginOnUIThread(() => CaptureHelper.CaptureLastRegion(_formEnhancers, true));
        }

        private void CaptureIe(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            if (_coreConfiguration.IECapture)
            {
                Execute.BeginOnUIThread(() => CaptureHelper.CaptureIe(_formEnhancers, true, null));
            }
        }
    }
}
