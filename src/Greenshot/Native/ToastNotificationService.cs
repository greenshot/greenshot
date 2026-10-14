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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Native.Toasts;
using Greenshot.Base.Threading;
using log4net;
using Windows.Foundation.Metadata;
using NotificationSetting = Windows.UI.Notifications.NotificationSetting;

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// The toasts of Windows, preferred over the balloons of the tray icon where they are available
    /// </summary>
    public sealed class ToastNotificationServiceProvider : INotificationServiceProvider
    {
        /// <inheritdoc />
        public int Priority => 100;

        /// <inheritdoc />
        public INotificationService TryCreate() => ToastNotificationService.Create();
    }

    /// <summary>
    /// Informs the user with toasts. A click on a toast with a capture raises <see cref="CaptureActivated"/>, also when Windows
    /// starts Greenshot for the click; other toasts run their click action while Greenshot runs.
    /// Not in Greenshot Light, which has no toasts.
    /// </summary>
    public sealed class ToastNotificationService : INotificationService
    {
        /// <summary>
        /// The app id of Greenshot, the same as the one of the start menu shortcut (see tasks-icons-reg.iss)
        /// </summary>
        public const string AppId = "Greenshot";

        private static readonly ILog Log = LogManager.GetLogger(typeof(ToastNotificationService));
        private static readonly ICoreConfiguration CoreConfiguration = IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>
        /// The click actions of the toasts, by their tag: they only live as long as Greenshot runs
        /// </summary>
        private readonly ConcurrentDictionary<string, Action> _clickActions = new ConcurrentDictionary<string, Action>();

        private ToastNotificationService()
        {
        }

        /// <summary>
        /// Registers Greenshot for toasts, null when Windows can't or won't show them (then the tray icon shows the notifications)
        /// or for the PortableApp and the .zip, which don't write the registration
        /// </summary>
        public static ToastNotificationService Create()
        {
            if (GreenshotEnvironment.LeavesNoTraces)
            {
                Log.Info("Portable or .zip, the tray icon shows the notifications.");
                return null;
            }

            if (!ApiInformation.IsTypePresent("Windows.ApplicationModel.Background.ToastNotificationActionTrigger"))
            {
                Log.Warn("ToastNotificationActionTrigger not available.");
                return null;
            }

            // Subscribed before the registration: when Windows started Greenshot for a click, the click comes right after it
            var service = new ToastNotificationService();
            ToastActivator.Activated += service.OnActivated;
            try
            {
                DesktopToasts.Register(AppId, "Greenshot", WriteIcon(), $"\"{Application.ExecutablePath}\"");
                var setting = DesktopToasts.GetSetting(AppId);
                if (setting != NotificationSetting.Enabled)
                {
                    ToastActivator.Activated -= service.OnActivated;
                    Log.InfoFormat("Windows doesn't show Greenshot's toasts ({0}), the tray icon shows the notifications.", setting);
                    return null;
                }
                return service;
            }
            catch (Exception ex)
            {
                ToastActivator.Activated -= service.OnActivated;
                Log.Warn("Couldn't register for toasts, the tray icon shows the notifications.", ex);
                return null;
            }
        }

        /// <summary>
        /// The icon of the toasts, a png file
        /// </summary>
        private static string WriteIcon()
        {
            var iconPath = Path.Combine(GreenshotEnvironment.LocalDataDirectory, "greenshot.png");
            if (!File.Exists(iconPath))
            {
                Directory.CreateDirectory(GreenshotEnvironment.LocalDataDirectory);
                using var greenshotImage = GreenshotResources.GetGreenshotIcon().ToBitmap();
                greenshotImage.Save(iconPath, ImageFormat.Png);
            }
            return iconPath;
        }

        /// <inheritdoc />
        public bool ShowsCaptures => true;

        /// <inheritdoc />
        public IReadOnlyCollection<string> GetShownCaptureIds() => DesktopToasts.GetTags(AppId);

        /// <inheritdoc />
        public event EventHandler<NotificationActivatedEventArgs> CaptureActivated;

        /// <inheritdoc />
        public void ShowNotification(Notification notification)
        {
            // Do not inform the user if this is disabled
            if (!CoreConfiguration.ShowTrayNotification)
            {
                return;
            }

            string toastTag = notification.CaptureId ?? Guid.NewGuid().ToString("D");
            var content = new ToastContent(toastTag).AddText(notification.Message);
            if (notification.ImagePath != null)
            {
                content.AddImage(notification.ImagePath);
            }

            try
            {
                // Toasts which are gone from the notification center can't be clicked anymore
                foreach (var tag in _clickActions.Keys.Except(DesktopToasts.GetTags(AppId)))
                {
                    _clickActions.TryRemove(tag, out _);
                }
                if (notification.CaptureId == null && notification.OnClick != null)
                {
                    _clickActions[toastTag] = notification.OnClick;
                }

                var toast = DesktopToasts.Show(AppId, content.ToXml(), toastTag, notification.Timeout.HasValue ? DateTimeOffset.Now.Add(notification.Timeout.Value) : null);
                toast.Failed += (sender, args) => Log.WarnFormat("Failed to display a toast due to {0}", args.ErrorCode);
            }
            catch (Exception ex)
            {
                _clickActions.TryRemove(toastTag, out _);
                Log.Warn("Ignoring exception as this means that it was not possible to generate a toast.", ex);
            }
        }

        /// <summary>
        /// A click on a toast, with its tag, called by Windows on a COM thread. Handled on the UI thread: when Windows started
        /// Greenshot for the click, that is after the startup, which subscribes to <see cref="CaptureActivated"/>.
        /// </summary>
        private void OnActivated(string tag)
        {
            if (tag == null)
            {
                return;
            }
            UiDispatcher.Current.InvokeAsync(() =>
            {
                if (_clickActions.TryRemove(tag, out var onClick))
                {
                    onClick();
                }
                else
                {
                    // A toast with a capture (its tag is the capture id), also after a restart
                    CaptureActivated?.Invoke(this, new NotificationActivatedEventArgs(tag));
                }
                return Task.FromResult(true);
            }).FireAndLog("Toast click", Log);
        }

        public void ShowWarningMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowNotification(new Notification(NotificationKind.Warning, message, timeout, onClickAction));
        }

        public void ShowErrorMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowNotification(new Notification(NotificationKind.Error, message, timeout, onClickAction));
        }

        public void ShowInfoMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowNotification(new Notification(NotificationKind.Info, message, timeout, onClickAction));
        }
    }
}
