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


using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Interop;
using log4net;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Status and error texts which a screen reader says when they change or appear, without moving the focus:
    /// <c>wpf:LiveRegion.Mode="Polite"</c> on a TextBlock, TextBox or ContentControl (Assertive for errors).
    /// Code which shows a message in another way calls <see cref="Announce"/>.
    /// </summary>
    public static class LiveRegion
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LiveRegion));

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

        /// <summary>
        /// Let a screen reader say a text which is in no element, e.g. what changed in a drawn window like the capture window.
        /// A UI Automation notification: a newer one with the same activity id replaces one which wasn't said yet.
        /// WPF on .NET Framework has no RaiseNotificationEvent, so this does what the newer WPF does there (Windows 10 1709 and newer):
        /// UiaRaiseNotificationEvent with the WPF provider of the window's automation peer.
        /// A window which calls this returns a <see cref="NotifyingWindowAutomationPeer"/> from OnCreateAutomationPeer.
        /// </summary>
        /// <param name="element">The element the text is about, e.g. the window</param>
        /// <param name="text">What to say</param>
        /// <param name="activityId">Groups the notifications, e.g. "GreenshotCapture"</param>
        public static void Notify(UIElement element, string text, string activityId)
        {
            if (string.IsNullOrEmpty(text) || element == null)
            {
                return;
            }

            var peer = UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element);
            // The provider of the window's own peer, as WPF uses it; any other element gets the provider of its window handle
            var provider = (peer as NotifyingWindowAutomationPeer)?.Provider;
            if (provider == null && PresentationSource.FromVisual(element) is HwndSource source && source.Handle != IntPtr.Zero)
            {
                provider = AutomationInteropProvider.HostProviderFromHandle(source.Handle);
            }
            if (provider == null)
            {
                return;
            }

            try
            {
                UiaRaiseNotificationEvent(provider, NotificationKindOther, NotificationProcessingImportantMostRecent, text, activityId);
            }
            catch (Exception ex) when (ex is EntryPointNotFoundException || ex is DllNotFoundException || ex is COMException)
            {
                // Older Windows, or no UI Automation: nothing is said
                Log.Debug($"No UI Automation notification: {ex.Message}");
            }
        }

        private const int NotificationKindOther = 4;
        private const int NotificationProcessingImportantMostRecent = 1;

        [DllImport("UIAutomationCore.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void UiaRaiseNotificationEvent([MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider, int notificationKind,
            int notificationProcessing, [MarshalAs(UnmanagedType.BStr)] string displayString, [MarshalAs(UnmanagedType.BStr)] string activityId);

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
