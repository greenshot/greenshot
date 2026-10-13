/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Greenshot.Base.Core;
using Greenshot.Helpers;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// One row of the "Shortcuts" settings tab: a hotkey, the capture function it triggers, and the destination
    /// the capture is sent to. Wraps a <see cref="HotkeyShortcutConfig"/> for editing in the UI.
    /// </summary>
    public class HotkeyShortcutItemViewModel : INotifyPropertyChanged
    {
        private string _id;
        private string _hotkey;
        private string _captureFunctionId;
        private string _destinationId;

        public string Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        public string Hotkey
        {
            get => _hotkey;
            set { if (_hotkey != value) { _hotkey = value; OnPropertyChanged(); } }
        }

        public string CaptureFunctionId
        {
            get => _captureFunctionId;
            set
            {
                if (_captureFunctionId != value)
                {
                    _captureFunctionId = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CaptureFunctionDisplayName));
                }
            }
        }

        public string CaptureFunctionDisplayName => HotkeyShortcutManager.GetCaptureFunctionDisplayName(CaptureFunctionId);

        /// <summary>
        /// The single destination this shortcut exports to, bound to the destinations dropdown
        /// (<see cref="HotkeyShortcutManager.GetAvailableDestinations"/>'s <c>Designation</c>).
        /// </summary>
        public string DestinationId
        {
            get => _destinationId;
            set { if (_destinationId != value) { _destinationId = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Converts this row back into the persisted configuration entry.
        /// </summary>
        public HotkeyShortcutConfig ToConfig()
        {
            return new HotkeyShortcutConfig
            {
                Id = Id,
                Hotkey = Hotkey,
                CaptureFunctionId = CaptureFunctionId,
                Destinations = string.IsNullOrEmpty(DestinationId) ? new List<string>() : new List<string> { DestinationId }
            };
        }

        /// <summary>
        /// Builds a row from a persisted configuration entry.
        /// </summary>
        public static HotkeyShortcutItemViewModel FromConfig(HotkeyShortcutConfig config)
        {
            return new HotkeyShortcutItemViewModel
            {
                Id = config?.Id,
                Hotkey = config?.Hotkey,
                CaptureFunctionId = config?.CaptureFunctionId,
                DestinationId = config?.Destinations != null && config.Destinations.Count > 0 ? config.Destinations[0] : null
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
