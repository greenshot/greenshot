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


using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// A slider which tells a screen reader the value it shows. A plain WPF slider only has a range, which Narrator reads as a percentage
    /// of it: "32 %" for a slider which shows "7 px". Set <see cref="ValueFormat"/> and bind the text next to the slider to
    /// <see cref="ValueText"/>, so both say the same:
    /// <code>&lt;wpf:ValueSlider x:Name="SizeSlider" ValueFormat="{}{0:0} px" .../&gt;
    /// &lt;TextBlock Text="{Binding ElementName=SizeSlider, Path=ValueText}"/&gt;</code>
    /// </summary>
    public class ValueSlider : Slider
    {
        public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
            nameof(ValueFormat), typeof(string), typeof(ValueSlider), new PropertyMetadata("{0:0}", (d, _) => ((ValueSlider)d).UpdateValueText()));

        private static readonly DependencyPropertyKey ValueTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ValueText), typeof(string), typeof(ValueSlider), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueTextProperty = ValueTextPropertyKey.DependencyProperty;

        public ValueSlider()
        {
            UpdateValueText();
        }

        /// <summary>
        /// The composite format of the value, with the unit, e.g. "{0:0} px"
        /// </summary>
        public string ValueFormat
        {
            get => (string)GetValue(ValueFormatProperty);
            set => SetValue(ValueFormatProperty, value);
        }

        /// <summary>
        /// The value as it is shown and read
        /// </summary>
        public string ValueText => (string)GetValue(ValueTextProperty);

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            UpdateValueText();
        }

        private void UpdateValueText()
        {
            string oldText = ValueText;
            string newText = string.Format(CultureInfo.CurrentCulture, ValueFormat ?? "{0}", Value);
            if (oldText == newText)
            {
                return;
            }

            SetValue(ValueTextPropertyKey, newText);
            if (UIElementAutomationPeer.FromElement(this) is { } peer && AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
            {
                peer.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldText, newText);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ValueSliderAutomationPeer(this);

        /// <summary>
        /// The slider peer with the value pattern added, a screen reader reads the value text from it
        /// </summary>
        private sealed class ValueSliderAutomationPeer : SliderAutomationPeer, IValueProvider
        {
            private readonly ValueSlider _owner;

            public ValueSliderAutomationPeer(ValueSlider owner) : base(owner)
            {
                _owner = owner;
            }

            public override object GetPattern(PatternInterface patternInterface)
            {
                return patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);
            }

            string IValueProvider.Value => _owner.ValueText;

            bool IValueProvider.IsReadOnly => !_owner.IsEnabled;

            void IValueProvider.SetValue(string value)
            {
                // Accepts the number with or without the unit, the range pattern does the rest
                string text = (value ?? string.Empty).Trim();
                int end = 0;
                while (end < text.Length && (char.IsDigit(text[end]) || "+-.,".IndexOf(text[end]) >= 0))
                {
                    end++;
                }

                if (!double.TryParse(text.Substring(0, end), NumberStyles.Float, CultureInfo.CurrentCulture, out double parsed))
                {
                    throw new ArgumentException("Not a number", nameof(value));
                }

                ((IRangeValueProvider)base.GetPattern(PatternInterface.RangeValue)).SetValue(parsed);
            }
        }
    }
}
