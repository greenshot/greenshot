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
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Editor.Destinations;
using Greenshot.Helpers;
using Hardcodet.Wpf.TaskbarNotification;
using log4net;

namespace Greenshot.Shell
{
    /// <summary>
    /// The Greenshot icon in the notification area, on wpf-notifyicon. The menu is our own <see cref="TrayMenu"/>, opened at the cursor:
    /// it is built when it opens and WPF places and scales it for the monitor under the cursor.
    /// </summary>
    internal sealed class TrayIcon : ITrayIcon, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TrayIcon));

        /// <summary>
        /// The id the previous WinForms tray icon registered with. Windows 11 remembers
        /// "show this icon on the taskbar" per executable and id, so a different id is a new
        /// icon and stays in the overflow.
        /// </summary>
        internal const uint LegacyTrayIconId = 1;

        private readonly IGreenshotShell _shell;
        private readonly TrayMenu _trayMenu;
        private readonly DispatcherTimer _doubleClickTimer;
        private TaskbarIcon _taskbarIcon;

        // The callbacks of the balloon which is shown
        private Action _balloonClicked;
        private Action _balloonClosed;

        private static ICoreConfiguration CoreConfig => IniConfigRegistry.GetSection<ICoreConfiguration>();

        public TrayIcon(IGreenshotShell shell)
        {
            _shell = shell;
            _trayMenu = new TrayMenu(shell);

            _doubleClickTimer = new DispatcherTimer(DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime)
            };
            _doubleClickTimer.Tick += (sender, args) =>
            {
                // No second click in time: a single click
                _doubleClickTimer.Stop();
                OnClick(CoreConfig.LeftClickAction);
            };

            // The icon id and the tooltip have to be stored before the icon is added: Windows 11
            // identifies the icon by them, and a new identity stays in the overflow.
            _taskbarIcon = CreateTaskbarIcon(GreenshotResources.GetGreenshotIcon(), BuildToolTip());
            // Startup can still be inside the call that added the icon. Apply the version again
            // once the dispatcher is idle, after that call has returned.
            _ = _taskbarIcon.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(ApplyNotifyVersion));

            _taskbarIcon.PreviewTrayContextMenuOpen += OnPreviewTrayContextMenuOpen;
            // MenuActivation matches a right click and then does nothing with it. The menu is
            // opened from the right-button-up. A context-menu message can follow that up, and
            // opening a second time would close the menu that just opened.
            _taskbarIcon.TrayRightMouseUp += OnTrayRightMouseUp;
            _taskbarIcon.TrayLeftMouseUp += OnTrayLeftMouseUp;
            _taskbarIcon.TrayBalloonTipClicked += OnBalloonTipClicked;
            _taskbarIcon.TrayBalloonTipClosed += OnBalloonTipClosed;
        }

        /// <summary>
        /// Creates the notification-area icon. The icon handle has to be assigned before the icon is shown,
        /// see the constructor.
        /// </summary>
        internal static TaskbarIcon CreateTaskbarIcon(Icon icon, string toolTip)
        {
            var taskbarIcon = new TaskbarIcon
            {
                Visibility = Visibility.Collapsed,
                Icon = icon,
                // Our own menu on a right click, see OnPreviewTrayContextMenuOpen
                MenuActivation = PopupActivationMode.RightClick,
                // The left click is handled here, with our own double click test
                NoLeftClickDelay = true
            };
            if (!TrySetIconId(taskbarIcon, LegacyTrayIconId))
            {
                Log.Warn("The tray icon keeps the library's icon id. Windows 11 will treat it as a new icon and hide it in the overflow.");
            }

            if (toolTip != null)
            {
                taskbarIcon.ToolTipText = toolTip;
            }

            taskbarIcon.Visibility = Visibility.Visible;
            if (!taskbarIcon.IsTaskbarIconCreated)
            {
                Log.Error("The notification area icon was not created.");
                return taskbarIcon;
            }

            // A new icon has no IsPromoted value, so Windows 11 leaves it in the overflow.
            // Explorer reads the value when the icon is added.
            if (TrayIconPromotion.PromoteIfUndecided(LegacyTrayIconId))
            {
                taskbarIcon.Visibility = Visibility.Collapsed;
                taskbarIcon.Visibility = Visibility.Visible;
                Log.Info("The tray icon is shown on the taskbar.");
            }

            // The library's own add switches the icon to NOTIFYICON_VERSION_4, including the add just above.
            KeepWindows2000NotifyMessages(taskbarIcon);
            return taskbarIcon;
        }

        /// <summary>
        /// NOTIFYICON_VERSION_4 is accepted, and the icon and its tooltip show, but Windows 11 then
        /// delivers no click to the icon. NOTIFYICON_VERSION (Windows 2000) delivers the button-up,
        /// which is what opens the menu. Balloon tips are part of that version.
        /// </summary>
        private void ApplyNotifyVersion()
        {
            if (_taskbarIcon != null && _taskbarIcon.IsTaskbarIconCreated)
            {
                KeepWindows2000NotifyMessages(_taskbarIcon);
            }
        }

        /// <summary>
        /// NIM_SETVERSION. The struct only has to identify the icon and carry the version.
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIcon")]
        private static extern bool ShellNotifyIcon(uint message, ref NotifyIconVersionData data);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NotifyIconVersionData
        {
            public int cbSize;
            public IntPtr WindowHandle;
            public uint TaskbarIconId;
            public uint ValidMembers;
            public uint CallbackMessageId;
            public IntPtr IconHandle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string ToolTipText;
            public uint IconState;
            public uint StateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string BalloonText;
            public uint VersionOrTimeout;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string BalloonTitle;
            public uint BalloonFlags;
            public Guid GuidItem;
            public IntPtr CustomBalloonIconHandle;
        }

        private static void KeepWindows2000NotifyMessages(TaskbarIcon taskbarIcon)
        {
            const uint windows2000 = 3;
            const uint setVersion = 4;
            object data = typeof(TaskbarIcon).GetField("iconData", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(taskbarIcon);
            var window = data?.GetType().GetField("WindowHandle")?.GetValue(data);
            var id = data?.GetType().GetField("TaskbarIconId")?.GetValue(data);
            if (!(window is IntPtr hwnd) || !(id is uint iconId))
            {
                Log.Warn("The tray icon stays on the Vista notify messages. A right click will not open the menu.");
                return;
            }

            var request = new NotifyIconVersionData
            {
                cbSize = Marshal.SizeOf(typeof(NotifyIconVersionData)),
                WindowHandle = hwnd,
                TaskbarIconId = iconId,
                VersionOrTimeout = windows2000
            };
            if (!ShellNotifyIcon(setVersion, ref request))
            {
                Log.Warn("The tray icon could not switch to the Windows 2000 notify messages. A right click will not open the menu.");
                return;
            }

            var sink = typeof(TaskbarIcon).GetField("messageSink", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(taskbarIcon);
            var version = sink?.GetType().GetProperty("Version");
            if (version == null)
            {
                Log.Warn("The tray icon's message window still expects Vista messages. A left click will not be reported.");
                return;
            }

            version.SetValue(sink, Enum.ToObject(version.PropertyType, windows2000));
        }

        private static bool TrySetIconId(TaskbarIcon taskbarIcon, uint iconId)
        {
            var dataField = typeof(TaskbarIcon).GetField("iconData", BindingFlags.Instance | BindingFlags.NonPublic);
            if (dataField == null)
            {
                return false;
            }

            object data = dataField.GetValue(taskbarIcon);
            var idField = data?.GetType().GetField("TaskbarIconId");
            if (idField == null)
            {
                return false;
            }

            idField.SetValue(data, iconId);
            dataField.SetValue(taskbarIcon, data);
            return true;
        }

        /// <summary>
        /// The tray menu, built when it opens
        /// </summary>
        public TrayMenu Menu => _trayMenu;

        /// <summary>
        /// Show or hide the icon in the notification area
        /// </summary>
        public bool Visible
        {
            get => _taskbarIcon?.Visibility == Visibility.Visible;
            set
            {
                if (_taskbarIcon == null)
                {
                    return;
                }

                _taskbarIcon.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                // Showing the icon again makes the library select Vista messages.
                if (value && _taskbarIcon.IsTaskbarIconCreated)
                {
                    KeepWindows2000NotifyMessages(_taskbarIcon);
                }
            }
        }

        /// <summary>
        /// The tooltip of the icon: the application title with the edition, e.g. "Greenshot Light - ..."
        /// </summary>
        public void UpdateToolTip()
        {
            if (_taskbarIcon == null)
            {
                return;
            }

            _taskbarIcon.ToolTipText = BuildToolTip();
        }

        private static string BuildToolTip()
        {
            string applicationTitle = Texts.Core.ApplicationTitle;
            if (applicationTitle != null && applicationTitle.StartsWith("Greenshot", StringComparison.Ordinal))
            {
                applicationTitle = GreenshotEdition.ProductName + applicationTitle.Substring("Greenshot".Length);
            }

            return NotifyIconTextHelper.ToNotifyIconText(applicationTitle);
        }

        /// <inheritdoc />
        public void ShowMenu()
        {
            _trayMenu.Show();
        }

        /// <inheritdoc />
        public void ShowBalloon(string title, string message, TrayBalloonLevel level, Action onClick = null, Action onClosed = null)
        {
            if (_taskbarIcon == null)
            {
                return;
            }

            // A new balloon replaces the one which is shown
            FinishBalloon(false);
            _balloonClicked = onClick;
            _balloonClosed = onClosed;

            var icon = level switch
            {
                TrayBalloonLevel.Warning => BalloonIcon.Warning,
                TrayBalloonLevel.Error => BalloonIcon.Error,
                _ => BalloonIcon.Info
            };
            _taskbarIcon.ShowBalloonTip(title, message, icon);
        }

        /// <inheritdoc />
        public void HideBalloon()
        {
            if (_taskbarIcon == null || (_balloonClicked == null && _balloonClosed == null))
            {
                return;
            }

            _taskbarIcon.HideBalloonTip();
            FinishBalloon(false);
        }

        private void OnBalloonTipClicked(object sender, RoutedEventArgs e)
        {
            FinishBalloon(true);
        }

        private void OnBalloonTipClosed(object sender, RoutedEventArgs e)
        {
            FinishBalloon(false);
        }

        /// <summary>
        /// The balloon is gone: call its click action (when clicked) and its closed action, once
        /// </summary>
        private void FinishBalloon(bool clicked)
        {
            var onClick = _balloonClicked;
            var onClosed = _balloonClosed;
            _balloonClicked = null;
            _balloonClosed = null;

            if (clicked)
            {
                try
                {
                    onClick?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception while handling the onclick action: ", ex);
                }
            }

            try
            {
                onClosed?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Warn("Exception while handling the onClosed action: ", ex);
            }
        }

        private void OnTrayRightMouseUp(object sender, RoutedEventArgs e)
        {
            if (_trayMenu.IsOpen)
            {
                return;
            }

            OpenTrayMenu();
        }

        /// <summary>
        /// wpf-notifyicon raises this before it opens its own context menu; we have none and open the tray menu instead.
        /// It is placed by WPF at the cursor, on the monitor under it with that monitor's DPI.
        /// </summary>
        private void OnPreviewTrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            // The right-button-up already opened it. Opening again would close that menu and build another.
            if (_trayMenu.IsOpen)
            {
                return;
            }

            OpenTrayMenu();
        }

        private void OpenTrayMenu()
        {
            try
            {
                _trayMenu.Show();
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't open the tray menu", ex);
            }
        }

        private void OnTrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            var conf = CoreConfig;
            if (conf.DoubleClickAction == ClickActions.DO_NOTHING)
            {
                // As there isn't a double-click we can start the Left click
                OnClick(conf.LeftClickAction);
                return;
            }

            // If the timer is enabled we are waiting for a double click...
            if (_doubleClickTimer.IsEnabled)
            {
                // User clicked a second time before the timer tick: Double-click!
                _doubleClickTimer.Stop();
                OnClick(conf.DoubleClickAction);
            }
            else
            {
                // If the timer ticks before the next click it was a single click
                _doubleClickTimer.Start();
            }
        }

        /// <summary>
        /// Handle a click on the tray icon
        /// </summary>
        private void OnClick(ClickActions clickAction)
        {
            try
            {
                switch (clickAction)
                {
                    case ClickActions.OPEN_LAST_IN_EXPLORER:
                        TrayActions.OpenLastCaptureLocation();
                        break;
                    case ClickActions.OPEN_LAST_IN_EDITOR:
                        TrayActions.OpenLastCaptureInEditor();
                        break;
                    case ClickActions.OPEN_SETTINGS:
                        _shell.ShowSetting();
                        break;
                    case ClickActions.SHOW_CONTEXT_MENU:
                        _trayMenu.Show();
                        break;
                    case ClickActions.CAPTURE_CLIPBOARD:
                        CaptureHelper.CaptureClipboard();
                        break;
                    case ClickActions.OPEN_CLIPBOARD_IN_EDITOR:
                        CaptureHelper.CaptureClipboard(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
                        break;
                    case ClickActions.OPEN_FILE_IN_EDITOR:
                        TrayActions.CaptureFile(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
                        break;
                    case ClickActions.CAPTURE_REGION:
                        CaptureHelper.CaptureRegion(false);
                        break;
                    case ClickActions.CAPTURE_SCREEN:
                        CaptureHelper.CaptureFullscreen(false, ScreenCaptureMode.FullScreen);
                        break;
                    case ClickActions.CAPTURE_WINDOW:
                        CaptureHelper.CaptureWindowInteractive(false);
                        break;
                    case ClickActions.OPEN_EMPTY_EDITOR:
                        TrayActions.OpenEmptyEditor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in the tray icon click action {clickAction}", ex);
            }
        }

        /// <summary>
        /// Remove the icon from the notification area, otherwise it stays until the mouse moves over it
        /// </summary>
        public void Dispose()
        {
            _doubleClickTimer.Stop();
            _trayMenu.Close();
            if (_taskbarIcon == null)
            {
                return;
            }

            _taskbarIcon.Visibility = Visibility.Collapsed;
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }
    }
}
