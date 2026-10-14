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
    /// Initializes the toasts of Windows, preferred over the balloons of the tray icon where they are available: checks whether
    /// Windows can show them and registers Greenshot for them (its app id, the COM class which receives the clicks and the command
    /// line Windows starts for a click while Greenshot isn't running, see <see cref="DesktopToasts.Register"/>).
    /// When the user or a policy turned them off in Windows, Greenshot shows nothing, the balloons don't take over.
    /// </summary>
    public sealed class ToastNotificationServiceProvider : INotificationServiceProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ToastNotificationServiceProvider));

        /// <inheritdoc />
        public int Priority => 100;

        /// <inheritdoc />
        /// <returns>null when Windows can't show toasts (then the tray icon shows the notifications), or for the PortableApp and
        /// the .zip, which don't write the registration</returns>
        public INotificationService TryCreate()
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
            ToastActivator.Activated += service.HandleClick;
            try
            {
                // Before the registration, which replaces the CustomActivator the Toolkit wrote for our app id
                RemoveOldRegistrations();
                DesktopToasts.Register(ToastNotificationService.AppId, "Greenshot", WriteIcon(), $"\"{Application.ExecutablePath}\"");
                var setting = DesktopToasts.GetSetting(ToastNotificationService.AppId);
                if (setting == NotificationSetting.DisabledByManifest)
                {
                    // Not a choice of the user, like the other settings (see IsEnabled): this Greenshot can't show toasts at all
                    ToastActivator.Activated -= service.HandleClick;
                    Log.InfoFormat("Windows can't show Greenshot's toasts ({0}), the tray icon shows the notifications.", setting);
                    return null;
                }
                if (setting != NotificationSetting.Enabled)
                {
                    Log.InfoFormat("Notifications are turned off ({0}), Greenshot shows none while they are off.", setting);
                }
                return service;
            }
            catch (Exception ex)
            {
                ToastActivator.Activated -= service.HandleClick;
                Log.Warn("Couldn't register for toasts, the tray icon shows the notifications.", ex);
                return null;
            }
        }

        /// <summary>
        /// Removes the registrations earlier versions left with the Windows Community Toolkit (one per exe path, otherwise Windows lists
        /// Greenshot several times in its notification settings, and a COM class of its own for our app id)
        /// </summary>
        private static void RemoveOldRegistrations()
        {
            try
            {
                foreach (var appId in DesktopToasts.RemoveOldRegistrations(ToastNotificationService.AppId, Path.GetFileName(Application.ExecutablePath)))
                {
                    Log.InfoFormat("Removed the old toast registration {0}.", appId);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't remove the old toast registrations.", ex);
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
    }

    /// <summary>
    /// Informs the user with toasts, initialized by <see cref="ToastNotificationServiceProvider"/>. A click on a toast with a capture
    /// raises <see cref="CaptureActivated"/>, also when Windows starts Greenshot for the click; other toasts run their click action
    /// while Greenshot runs. Not in Greenshot Light, which has no toasts.
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

        internal ToastNotificationService()
        {
        }

        /// <inheritdoc />
        /// <remarks>Asked each time, the user can turn the toasts on and off in Windows while Greenshot runs</remarks>
        public bool IsEnabled
        {
            get
            {
                if (!CoreConfiguration.ShowTrayNotification)
                {
                    return false;
                }
                try
                {
                    return DesktopToasts.GetSetting(AppId) == NotificationSetting.Enabled;
                }
                catch (Exception ex)
                {
                    Log.Warn("Couldn't read whether Windows shows Greenshot's toasts.", ex);
                    return false;
                }
            }
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
            // Do not inform the user if this is disabled, in Greenshot or in Windows
            if (!IsEnabled)
            {
                return;
            }

            string toastTag = notification.CaptureId ?? Guid.NewGuid().ToString("D");
            var content = new ToastContent(toastTag);
            if (notification.Lines.Count > 0)
            {
                content.AddText(notification.Title ?? "Greenshot");
                if (notification.Lines.Count == 1)
                {
                    // One destination: its icon is the big image (visible also when the toast is collapsed in the notification
                    // center), the row is the text only
                    var line = notification.Lines[0];
                    if (line.IconPath != null)
                    {
                        content.SetAppLogo(line.IconPath);
                    }
                    content.AddRow(null, line.Text);
                }
                else
                {
                    // Several destinations: the big image stays Greenshot's icon, each row has the icon of its destination
                    foreach (var line in notification.Lines)
                    {
                        content.AddRow(line.IconPath, line.Text);
                    }
                }
            }
            else
            {
                content.AddText(notification.Message);
            }
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
        /// A click on a toast, with its tag, called by Windows on a COM thread (see <see cref="ToastActivator.Activated"/>). Handled on
        /// the UI thread: when Windows started Greenshot for the click, that is after the startup, which subscribes to <see cref="CaptureActivated"/>.
        /// </summary>
        internal void HandleClick(string tag)
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
