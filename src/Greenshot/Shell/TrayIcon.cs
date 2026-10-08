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

            _taskbarIcon = new TaskbarIcon
            {
                Icon = GreenshotResources.GetGreenshotIcon(),
                // Our own menu on a right click, see OnPreviewTrayContextMenuOpen
                MenuActivation = PopupActivationMode.RightClick,
                // The left click is handled here, with our own double click test
                NoLeftClickDelay = true
            };
            UpdateToolTip();

            _taskbarIcon.PreviewTrayContextMenuOpen += OnPreviewTrayContextMenuOpen;
            _taskbarIcon.TrayLeftMouseUp += OnTrayLeftMouseUp;
            _taskbarIcon.TrayBalloonTipClicked += OnBalloonTipClicked;
            _taskbarIcon.TrayBalloonTipClosed += OnBalloonTipClosed;
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
                if (_taskbarIcon != null)
                {
                    _taskbarIcon.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
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

            string applicationTitle = Texts.Core.ApplicationTitle;
            if (applicationTitle.StartsWith("Greenshot", StringComparison.Ordinal))
            {
                applicationTitle = GreenshotEdition.ProductName + applicationTitle.Substring("Greenshot".Length);
            }

            _taskbarIcon.ToolTipText = NotifyIconTextHelper.ToNotifyIconText(applicationTitle);
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

        /// <summary>
        /// wpf-notifyicon raises this before it opens its own context menu; we have none and open the tray menu instead.
        /// It is placed by WPF at the cursor, on the monitor under it with that monitor's DPI.
        /// </summary>
        private void OnPreviewTrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
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
