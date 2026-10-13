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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Native.Toasts;
using Greenshot.Base.Threading;
using log4net;
using Windows.Foundation.Metadata;
#if !GREENSHOT_LIGHT
using Greenshot.Notifications;
#endif

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// Informs the user with toasts. A click on a toast runs its click action while Greenshot runs; a toast with a capture
    /// offers the capture in the destination picker, also when Windows starts Greenshot for the click.
    /// </summary>
    public class ToastNotificationService : INotificationService
    {
        /// <summary>
        /// The app id of Greenshot, the same as the one of the start menu shortcut (see tasks-icons-reg.iss)
        /// </summary>
        public const string AppId = "Greenshot";

        private const string OpenVerb = "open:";
        private const string PickVerb = "pick:";

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
        /// Registers Greenshot for toasts, null when Windows can't show them or for the PortableApp and the .zip, which don't write the registration
        /// </summary>
        public static ToastNotificationService Create()
        {
            if (GreenshotEnvironment.LeavesNoTraces)
            {
                return null;
            }

            if (!ApiInformation.IsTypePresent("Windows.ApplicationModel.Background.ToastNotificationActionTrigger"))
            {
                Log.Warn("ToastNotificationActionTrigger not available.");
                return null;
            }

            try
            {
                DesktopToasts.Register(AppId, "Greenshot", WriteIcon(), $"\"{Application.ExecutablePath}\"");
                // The clicks are dispatched by the message loop of this (the UI) thread, none comes in before this subscription
                var service = new ToastNotificationService();
                ToastActivator.Activated += service.OnActivated;
#if !GREENSHOT_LIGHT
                NotificationCaptures.RemoveUnused();
#endif
                return service;
            }
            catch (Exception ex)
            {
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
        public void ShowNotification(Notification notification)
        {
            // Do not inform the user if this is disabled
            if (!CoreConfiguration.ShowTrayNotification)
            {
                return;
            }

            string toastTag = notification.CaptureId ?? Guid.NewGuid().ToString("D");
            var content = new ToastContent(OpenVerb + toastTag).AddText(notification.Message);
            if (notification.ImagePath != null)
            {
                content.AddImage(notification.ImagePath);
            }
            if (notification.CaptureId != null)
            {
                content.AddButton(Texts.Settings.Destination, PickVerb + toastTag);
            }

            try
            {
                // Toasts which are gone from the notification center can't be clicked anymore
                foreach (var tag in _clickActions.Keys.Except(DesktopToasts.GetTags(AppId)))
                {
                    _clickActions.TryRemove(tag, out _);
                }
                if (notification.OnClick != null)
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
        /// A click on a toast or its button, called by Windows on a COM thread
        /// </summary>
        private void OnActivated(string arguments)
        {
            string tag = arguments?.Substring(arguments.IndexOf(':') + 1);
            UiDispatcher.Current.InvokeAsync(() =>
            {
                if (arguments?.StartsWith(OpenVerb, StringComparison.Ordinal) == true && _clickActions.TryRemove(tag, out var onClick))
                {
                    onClick();
                    return Task.FromResult(true);
                }
#if GREENSHOT_LIGHT
                return Task.FromResult(false);
#else
                // The click action is gone after a restart: the toast offers its capture in the destination picker, like its button
                return NotificationCaptures.PickDestinationAsync(tag);
#endif
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
