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
using System.Linq;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.Log;
using Dapplo.Windows.Input.Keyboard;
using Greenshot.Addons.Core;

namespace Greenshot.Components
{
    /// <summary>
    /// This handles the hotkey for the region capture
    /// </summary>
    public class HotKeyHandler : KeyCombinationHandler, IDisposable
    {
        private readonly IDisposable _propertyChangeSubscription;
        private static readonly LogSource Log = new LogSource();
        public override bool Handle(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            var result = base.Handle(keyboardHookEventArgs);
            Log.Debug().WriteLine("{0} - {1} == {2} : {3}- {4}", result, AvailableKeys.Length, AvailableKeys.Count(b => b), string.Join("+", TriggerCombination), keyboardHookEventArgs.ToString());
            return result;
        }

        /// <summary>
        /// Create a RegionHotKeyHandler for the specified VirtualKeyCodes
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="propertyName">string with the property of the ICoreConfiguration to use</param>
        public HotKeyHandler(ICoreConfiguration coreConfiguration, string propertyName)
        {
            string RetrieveFunc() => coreConfiguration.GetIniValue(propertyName).Value as string;
            _propertyChangeSubscription = coreConfiguration
                .OnPropertyChanged(propertyName)
                .Subscribe(pc => UpdateKeyCombination(RetrieveFunc));
            UpdateKeyCombination(RetrieveFunc);
        }

        /// <summary>
        /// This updates the key combination
        /// </summary>
        /// <param name="retrieveHotkeyFunc"></param>
        private void UpdateKeyCombination(Func<string> retrieveHotkeyFunc)
        {
            var keyCombinationString = retrieveHotkeyFunc();
            var combination = KeyHelper.VirtualKeyCodesFromString(keyCombinationString);
            Configure(combination);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _propertyChangeSubscription.Dispose();
        }
    }
}
