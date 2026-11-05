/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Greenshot.Base.Wpf.Views
{
    /// <summary>
    /// The box of the hotkey editor which takes the pressed keys. A plain Border has no automation peer, so a screen reader
    /// would find nothing when it gets the focus: this one is a read-only edit field whose value is the captured hotkey.
    /// </summary>
    public class HotkeyCaptureBox : Border
    {
        private string _value = string.Empty;

        /// <summary>
        /// The captured hotkey as text, the value a screen reader reads
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                string newValue = value ?? string.Empty;
                if (newValue == _value)
                {
                    return;
                }
                string oldValue = _value;
                _value = newValue;
                // Only when someone listens, creating the peer for nothing isn't needed
                if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
                {
                    var peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
                    peer?.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
                }
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new HotkeyCaptureBoxAutomationPeer(this);
        }

        private sealed class HotkeyCaptureBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
        {
            public HotkeyCaptureBoxAutomationPeer(HotkeyCaptureBox owner) : base(owner)
            {
            }

            private HotkeyCaptureBox Box => (HotkeyCaptureBox)Owner;

            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

            protected override string GetClassNameCore() => nameof(HotkeyCaptureBox);

            protected override bool IsControlElementCore() => true;

            protected override bool IsContentElementCore() => true;

            public override object GetPattern(PatternInterface patternInterface)
            {
                return patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);
            }

            // The keys are pressed, not typed: read-only for UI Automation
            public bool IsReadOnly => true;

            public string Value => Box.Value;

            public void SetValue(string value)
            {
                throw new ElementNotEnabledException();
            }
        }
    }
}
