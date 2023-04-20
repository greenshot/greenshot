/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Imaging;
using System.IO;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using log4net;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Greenshot.Plugin.Win10
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
            if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                Log.Info("Greenshot was activated due to a toast.");
            }

            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Obtain any user input (text boxes, menu selections) from the notification
                ValueSet userInput = toastArgs.UserInput;

                Log.Info("Toast activated. Args: " + toastArgs.Argument);
            };

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
        /// <param name="timeout">TimeSpan until the toast timeouts</param>
        /// <param name="onClickAction">Action called when clicked</param>
        /// <param name="onClosedAction">Action called when the toast is closed</param>
        private void ShowMessage(string message, TimeSpan? timeout = default, Action onClickAction = null, Action onClosedAction = null)
        {
            // Do not inform the user if this is disabled
            if (!CoreConfiguration.ShowTrayNotification)
            {
                return;
            }

            // Prepare the toast notifier. Be sure to specify the AppUserModelId on your application's shortcut!
            Microsoft.Toolkit.Uwp.Notifications.ToastNotifierCompat toastNotifier = null;
            try
            {
                toastNotifier = ToastNotificationManagerCompat.CreateToastNotifier();
            }
            catch (Exception ex)
            {
                Log.Warn("Ignoring exception as this means that it was not possible to create a toast notifier.", ex);
                return;
            }

            // Here is an interesting article on reading the settings: https://www.rudyhuyn.com/blog/2018/02/10/toastnotifier-and-settings-careful-with-non-uwp-applications/
            try
            {
                if (toastNotifier.Setting != NotificationSetting.Enabled)
                {
                    Log.DebugFormat("Ignored toast due to {0}", toastNotifier.Setting);
                    return;
                }
            }
            catch (Exception)
            {
                Log.Info("Ignoring exception as this means that there was no stored settings.");
            }

            try
            {
                // Generate the toast and send it off
                new ToastContentBuilder()
                    .AddArgument("ToastID", 100)
                    // Inline image
                    .AddText(message)
                    // Profile (app logo override) image
                    //.AddAppLogoOverride(new Uri($@"file://{_imageFilePath}"), ToastGenericAppLogoCrop.None)
                    .Show(toast =>
                    {
                    // Windows 10 first with 1903: ExpiresOnReboot = true
                    toast.ExpirationTime = timeout.HasValue ? DateTimeOffset.Now.Add(timeout.Value) : (DateTimeOffset?)null;

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
                            Log.Debug($"Toast closed with reason {eventArgs.Reason}");
                            if (eventArgs.Reason != ToastDismissalReason.UserCanceled)
                            {
                                return;
                            }

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
                    });

            }
            catch (Exception ex)
            {
                Log.Warn("Ignoring exception as this means that it was not possible to generate a toast.", ex);
            }
        }

        private void ToastOnFailed(ToastNotification sender, ToastFailedEventArgs args)
        {
            Log.WarnFormat("Failed to display a toast due to {0}", args.ErrorCode);
            Log.Debug(sender.Content.GetXml());
        }

        public void ShowWarningMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction);
        }

        public void ShowErrorMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction);
        }

        public void ShowInfoMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction);
        }

        /// <summary>
        /// Factory method, helping with checking if the notification service is even available
        /// </summary>
        /// <returns>ToastNotificationService</returns>
        public static ToastNotificationService Create()
        {
            if (ApiInformation.IsTypePresent("Windows.ApplicationModel.Background.ToastNotificationActionTrigger"))
            {
                return new ToastNotificationService();
            }

            Log.Warn("ToastNotificationActionTrigger not available.");

            return null;
        }
    }
}