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
using System.Threading;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using log4net;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Threading.Tasks;
using Greenshot.Base.Threading;

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// This service provides a way to inform (notify) the user.
    /// </summary>
    public class ToastNotificationService : INotificationService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ToastNotificationService));
        private static readonly ICoreConfiguration CoreConfiguration = IniConfigRegistry.GetSection<ICoreConfiguration>();

        private const string ExportIdArgument = "exportId";
        private const string ActionArgument = "action";
        private const string OpenAction = "open";
        private const string SendToAction = "sendTo";
        private const string EditAction = "edit";

        // The export notifications which can still be clicked, by id: Windows reports a click with OnActivated, with the arguments of the toast
        private static readonly ConcurrentDictionary<string, (ExportNotification Notification, DateTimeOffset Expires)> ExportNotificationsById =
            new ConcurrentDictionary<string, (ExportNotification Notification, DateTimeOffset Expires)>();

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
                OnExportNotificationActivated(args);
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
            ShowMessageAsync(message, timeout, onClickAction, onClosedAction).FireAndLog("Show a toast", Log);
        }

        private async Task ShowMessageAsync(string message, TimeSpan? timeout, Action onClickAction, Action onClosedAction)
        {
            // Do not inform the user if this is disabled
            if (!CoreConfiguration.ShowTrayNotification)
            {
                return;
            }

            // Do not inform the user if ToastNotification is not enabled (the continuation returns to the caller's context)
            if (!await IsToastNotificationEnabledAsync())
            {
                return;
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

        /// <inheritdoc />
        public void ShowExportNotification(ExportNotification notification)
        {
            ShowExportNotificationAsync(notification).FireAndLog("Show an export toast", Log);
        }

        private async Task ShowExportNotificationAsync(ExportNotification notification)
        {
            // Do not inform the user if this is disabled
            if (notification == null || !CoreConfiguration.ShowTrayNotification)
            {
                return;
            }

            if (!await IsToastNotificationEnabledAsync())
            {
                return;
            }

            try
            {
                RemoveExpiredExportNotifications();
                string id = Guid.NewGuid().ToString("N");
                var builder = new ToastContentBuilder()
                    // Also the argument of every button
                    .AddArgument(ExportIdArgument, id)
                    .AddText(notification.Title);
                if (!string.IsNullOrEmpty(notification.Detail))
                {
                    builder.AddText(notification.Detail);
                }

                // Where the capture went: the icon of the destination
                builder.AddAppLogoOverride(new Uri(notification.IconPath ?? _imageFilePath), ToastGenericAppLogoCrop.None);
                if (notification.PreviewPath != null)
                {
                    builder.AddInlineImage(new Uri(notification.PreviewPath));
                }

                int buttons = 0;
                if (notification.ShowButtons)
                {
                    buttons += AddButton(builder, notification.Open, Texts.Core.NotificationOpen, OpenAction);
                    buttons += AddButton(builder, notification.SendTo, Texts.Core.NotificationSendTo, SendToAction);
                    buttons += AddButton(builder, notification.Edit, Texts.Core.NotificationEdit, EditAction);
                }

                if (!notification.Succeeded && buttons > 0)
                {
                    // A failure stays until the user did something with it (a reminder needs a button)
                    builder.SetToastScenario(ToastScenario.Reminder);
                }

                var expires = notification.Timeout.HasValue ? DateTimeOffset.Now.Add(notification.Timeout.Value) : DateTimeOffset.Now.AddDays(1);
                ExportNotificationsById[id] = (notification, expires);
                builder.Show(toast =>
                {
                    toast.ExpirationTime = expires;
                    toast.Failed += ToastOnFailed;
                });
            }
            catch (Exception ex)
            {
                Log.Warn("Ignoring exception as this means that it was not possible to generate a toast.", ex);
            }
        }

        private static int AddButton(ToastContentBuilder builder, Action action, string text, string actionArgument)
        {
            if (action == null)
            {
                return 0;
            }

            builder.AddButton(new ToastButton().SetContent(text).AddArgument(ActionArgument, actionArgument));
            return 1;
        }

        /// <summary>
        /// The user clicked an export notification or one of its buttons, Windows reports it on another thread
        /// </summary>
        private static void OnExportNotificationActivated(ToastArguments args)
        {
            if (!args.TryGetValue(ExportIdArgument, out string id) || !ExportNotificationsById.TryGetValue(id, out var entry))
            {
                // Not an export notification, or from before Greenshot was started again
                return;
            }

            args.TryGetValue(ActionArgument, out string actionArgument);
            var notification = entry.Notification;
            var action = actionArgument switch
            {
                OpenAction => notification.Open,
                SendToAction => notification.SendTo,
                EditAction => notification.Edit,
                _ => notification.DefaultAction
            };
            if (action == null)
            {
                return;
            }

            UiDispatcher.Current.InvokeAsync(action, CancellationToken.None).FireAndLog($"Export notification action {actionArgument ?? "click"}", Log);
        }

        private static void RemoveExpiredExportNotifications()
        {
            var now = DateTimeOffset.Now;
            foreach (var pair in ExportNotificationsById)
            {
                if (pair.Value.Expires < now)
                {
                    ExportNotificationsById.TryRemove(pair.Key, out _);
                }
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

        private static async Task<bool> IsToastNotificationEnabledAsync()
        {
            try
            {
                // The notifier is created on an STA worker, the setting is read with a timeout
                var readSetting = StaWorkers.Get("Toast").RunAsync(() =>
                {
                    // Prepare the toast notifier. Be sure to specify the AppUserModelId on your application's shortcut!
                    try
                    {
                        var toastNotifier = ToastNotificationManagerCompat.CreateToastNotifier();
                        return (Created: true, Setting: toastNotifier.Setting);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Could not create a toast notifier.", ex);
                        return (Created: false, Setting: NotificationSetting.DisabledForApplication);
                    }
                });

                var (toastNotifierCreated, setting) = await readSetting.WaitAsync(TimeSpan.FromMilliseconds(500));
                if (!toastNotifierCreated)
                {
                    return false;
                }

                if (setting != NotificationSetting.Enabled)
                {
                    Log.DebugFormat("Ignored toast due to {0}", setting);
                    return false;
                }
            }
            catch (TimeoutException)
            {
                Log.Warn("Timed out reading toast notification setting; skipping setting check.");
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Exception reading toast notification setting, skipping check: {0}", ex.Message);
            }

            return true;
        }
    }
}