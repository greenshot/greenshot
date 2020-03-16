/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Windows.UI.Notifications;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotWin10Plugin.Native;
using log4net;

namespace GreenshotWin10Plugin
{
    /// <summary>
    /// This service provides a way to inform (notify) the user.
    /// </summary>
    public class ToastNotificationService : INotificationService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ToastNotificationService));
        private static readonly CoreConfiguration CoreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();

        private readonly string _imageFilePath;
        public ToastNotificationService()
        {
            // Register AUMID and COM server (for Desktop Bridge apps, this no-ops)
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<GreenshotNotificationActivator>("Greenshot");
            // Register COM server and activator type
            DesktopNotificationManagerCompat.RegisterActivator<GreenshotNotificationActivator>();

            var localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Greenshot");
            if (!Directory.Exists(localAppData))
            {
                Directory.CreateDirectory(localAppData);
            }
            _imageFilePath = Path.Combine(localAppData, "greenshot.png");

            if (File.Exists(_imageFilePath))
            {
                return;
            }

            using var greenshotImage = GreenshotResources.GetGreenshotIcon().ToBitmap();
            greenshotImage.Save(_imageFilePath, ImageFormat.Png);
        }

        /// <summary>
        /// This creates the actual toast
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">milliseconds until the toast timeouts</param>
        /// <param name="onClickAction">Action called when clicked</param>
        /// <param name="onClosedAction">Action called when the toast is closed</param>
        private void ShowMessage(string message, int timeout, Action onClickAction, Action onClosedAction)
        {
            // Do not inform the user if this is disabled
            if (!CoreConfiguration.ShowTrayNotification)
            {
                return;
            }
            // Prepare the toast notifier. Be sure to specify the AppUserModelId on your application's shortcut!
            var toastNotifier = DesktopNotificationManagerCompat.CreateToastNotifier();
            if (toastNotifier.Setting != NotificationSetting.Enabled)
            {
                Log.DebugFormat("Ignored toast due to {0}", toastNotifier.Setting);
                return;
            }

            // Get a toast XML template
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);

            // Fill in the text elements
            var stringElement = toastXml.GetElementsByTagName("text").First();
            stringElement.AppendChild(toastXml.CreateTextNode(message));

            if (_imageFilePath != null && File.Exists(_imageFilePath))
            {
                // Specify the absolute path to an image
                var imageElement = toastXml.GetElementsByTagName("image").First();
                var imageSrcNode = imageElement.Attributes.GetNamedItem("src");
                if (imageSrcNode != null)
                {
                    imageSrcNode.NodeValue = _imageFilePath;
                }
            }

            // Create the toast and attach event listeners
            var toast = new ToastNotification(toastXml)
            {
                ExpiresOnReboot = true,
                ExpirationTime = timeout > 0 ? DateTimeOffset.Now.AddMilliseconds(timeout) : (DateTimeOffset?)null
            };

            void ToastActivatedHandler(ToastNotification toastNotification, object sender)
            {
                try
                {
                    onClickAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception while handling the onclick action: ", ex);
                }

                toast.Activated -= ToastActivatedHandler;
            }

            if (onClickAction != null)
            {
                toast.Activated += ToastActivatedHandler;
            }

            void ToastDismissedHandler(ToastNotification toastNotification, ToastDismissedEventArgs eventArgs)
            {
                Log.Debug("Toast closed");
                try
                {
                    onClosedAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception while handling the onClosed action: ", ex);
                }

                toast.Dismissed -= ToastDismissedHandler;
                // Remove the other handler too
                toast.Activated -= ToastActivatedHandler;
                toast.Failed -= ToastOnFailed;
            }
            toast.Dismissed += ToastDismissedHandler;
            toast.Failed += ToastOnFailed;
            toastNotifier.Show(toast);
        }

        private void ToastOnFailed(ToastNotification sender, ToastFailedEventArgs args)
        {
            Log.WarnFormat("Failed to display a toast due to {0}", args.ErrorCode);
            Log.Debug(sender.Content.GetXml());
        }

        public void ShowWarningMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction);
        }

        public void ShowErrorMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction);
        }

        public void ShowInfoMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction);
        }
    }
}
