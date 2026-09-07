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

using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Triggers;
using log4net;

namespace Greenshot.Triggers
{
    /// <summary>
    /// Trigger that fires a recipe when a specific global keyboard hotkey is pressed.
    /// </summary>
    public class HotkeyTrigger : TriggerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyTrigger));

        private int _registrationId = -1;
        public string HotkeyString { get; set; }

        public HotkeyTrigger(string id, string name, string hotkeyString, string targetRecipeId)
            : base(id, name, targetRecipeId)
        {
            HotkeyString = hotkeyString;
        }

        public override void Start()
        {
            if (string.IsNullOrEmpty(HotkeyString)) return;

            Stop();

            Keys modifierKeyCode = HotkeyManager.HotkeyModifiersFromString(HotkeyString);
            Keys virtualKeyCode = HotkeyManager.HotkeyFromString(HotkeyString);

            if (virtualKeyCode == Keys.None)
            {
                Log.InfoFormat("Skipping hotkey registration for {0}, no hotkey set!", Name);
                return;
            }

            SynchronizationContext uiContext = null;
            try
            {
                uiContext = SimpleServiceProvider.Current.GetInstance<SynchronizationContext>() ?? SynchronizationContext.Current;
            }
            catch
            {
                uiContext = SynchronizationContext.Current;
            }

            _registrationId = HotkeyManager.RegisterHotKey(modifierKeyCode, virtualKeyCode, () =>
            {
                Log.DebugFormat("Hotkey '{0}' pressed for trigger '{1}' -> recipe '{2}'", HotkeyString, Name, TargetRecipeId);
                if (uiContext != null)
                {
                    uiContext.Post(_ => OnTriggered(), null);
                }
                else
                {
                    OnTriggered();
                }
            });

            if (_registrationId >= 0)
            {
                Log.DebugFormat("Registered hotkey '{0}' for trigger '{1}' (ID {2})", HotkeyString, Name, _registrationId);
            }
            else
            {
                Log.WarnFormat("Failed to register hotkey '{0}' for trigger '{1}'", HotkeyString, Name);
            }
        }

        public override void Stop()
        {
            if (_registrationId >= 0)
            {
                HotkeyManager.UnregisterHotKey(_registrationId);
                _registrationId = -1;
                Log.DebugFormat("Unregistered hotkey for trigger '{0}'", Name);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Stop();
            }
        }
    }
}
