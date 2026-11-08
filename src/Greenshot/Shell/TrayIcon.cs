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
using System.Windows;
using System.Windows.Threading;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Helpers;
using Greenshot.Recipes;
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

        private readonly TrayMenu _trayMenu;
        private readonly DispatcherTimer _doubleClickTimer;
        private TaskbarIcon _taskbarIcon;

        // The callbacks of the balloon which is shown
        private Action _balloonClicked;
        private Action _balloonClosed;

        private static ICoreConfiguration CoreConfig => IniConfigRegistry.GetSection<ICoreConfiguration>();

        public TrayIcon(IGreenshotShell shell)
        {
            _trayMenu = new TrayMenu(shell);

            _doubleClickTimer = new DispatcherTimer(DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime)
            };
            _doubleClickTimer.Tick += (sender, args) =>
            {
                // No second click in time: a single click
                _doubleClickTimer.Stop();
                TrayClickRecipes.Start(CoreConfig.TrayClickRecipe, "Tray icon click");
            };

            _taskbarIcon = new TaskbarIcon
            {
                // The constructor adds the icon without icon and tooltip, Windows 11 (26200) never shows such an icon.
                // Removed here, it is added again with both when the shell sets Visible.
                Visibility = Visibility.Collapsed,
                Icon = GreenshotResources.GetGreenshotIcon(),
                // Our own menu on a right click, see OnPreviewTrayContextMenuOpen
                MenuActivation = PopupActivationMode.RightClick,
                // The left click is handled here: it waits for a double click only when a double click runs something
                NoLeftClickDelay = true
            };
            UpdateToolTip();

            _taskbarIcon.PreviewTrayContextMenuOpen += OnPreviewTrayContextMenuOpen;
            _taskbarIcon.TrayLeftMouseUp += OnTrayLeftMouseUp;
            _taskbarIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;
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

        /// <summary>
        /// The left button went up. For a double click this comes only once, for the first click: wpf-notifyicon raises
        /// TrayMouseDoubleClick instead of the second (#1708)
        /// </summary>
        private void OnTrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            var conf = CoreConfig;
            if (string.IsNullOrWhiteSpace(conf.TrayDoubleClickRecipe))
            {
                // Nothing on a double click: no need to wait for one
                TrayClickRecipes.Start(conf.TrayClickRecipe, "Tray icon click");
                return;
            }

            // If the timer ticks before a double click it was a single click
            _doubleClickTimer.Start();
        }

        private void OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // Not a single click after all; without a double click recipe the single click ran already
            _doubleClickTimer.Stop();
            TrayClickRecipes.Start(CoreConfig.TrayDoubleClickRecipe, "Tray icon double-click");
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
