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


using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Status and error texts which a screen reader says when they change or appear, without moving the focus:
    /// <c>wpf:LiveRegion.Mode="Polite"</c> on a TextBlock, TextBox or ContentControl (Assertive for errors).
    /// Code which shows a message in another way calls <see cref="Announce"/>.
    /// </summary>
    public static class LiveRegion
    {
        public static readonly DependencyProperty ModeProperty = DependencyProperty.RegisterAttached(
            "Mode", typeof(AutomationLiveSetting), typeof(LiveRegion), new PropertyMetadata(AutomationLiveSetting.Off, OnModeChanged));

        public static AutomationLiveSetting GetMode(DependencyObject element) => (AutomationLiveSetting)element.GetValue(ModeProperty);

        public static void SetMode(DependencyObject element, AutomationLiveSetting value) => element.SetValue(ModeProperty, value);

        /// <summary>
        /// Let a screen reader say the (new) text of the element, its LiveSetting decides how urgent
        /// </summary>
        public static void Announce(UIElement element)
        {
            if (element == null || !element.IsVisible || !AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
            {
                return;
            }

            var peer = UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement element))
            {
                return;
            }

            var mode = (AutomationLiveSetting)e.NewValue;
            AutomationProperties.SetLiveSetting(element, mode);
            var descriptor = GetTextDescriptor(element);
            // The descriptor holds the element: only while it is in a window
            element.Loaded -= OnLoaded;
            element.Unloaded -= OnUnloaded;
            element.IsVisibleChanged -= OnIsVisibleChanged;
            descriptor?.RemoveValueChanged(element, OnTextChanged);
            if (mode == AutomationLiveSetting.Off)
            {
                return;
            }

            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;
            element.IsVisibleChanged += OnIsVisibleChanged;
            if (element.IsLoaded)
            {
                descriptor?.AddValueChanged(element, OnTextChanged);
            }
        }

        private static DependencyPropertyDescriptor GetTextDescriptor(FrameworkElement element)
        {
            DependencyProperty property = element switch
            {
                TextBlock _ => TextBlock.TextProperty,
                TextBox _ => TextBox.TextProperty,
                ContentControl _ => ContentControl.ContentProperty,
                _ => null
            };
            return property == null ? null : DependencyPropertyDescriptor.FromProperty(property, element.GetType());
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            var descriptor = GetTextDescriptor(element);
            descriptor?.RemoveValueChanged(element, OnTextChanged);
            descriptor?.AddValueChanged(element, OnTextChanged);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            GetTextDescriptor(element)?.RemoveValueChanged(element, OnTextChanged);
        }

        private static void OnTextChanged(object sender, System.EventArgs e)
        {
            Announce((UIElement)sender);
        }

        private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // An error text which is shown (Visibility bound to the error) is new to the user too
            if ((bool)e.NewValue)
            {
                Announce((UIElement)sender);
            }
        }
    }
}
