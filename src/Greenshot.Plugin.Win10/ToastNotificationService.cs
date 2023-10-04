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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
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

        private const string _heroImageFilePrefix = "hero-";
        private readonly string _imageFilePath;
        private readonly string _localAppData;
        private readonly ToastNotifierCompat _toastNotifier;
        private readonly SynchronizationContext _mainSynchronizationContext;

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

            _toastNotifier = ToastNotificationManagerCompat.CreateToastNotifier();
            _mainSynchronizationContext = SynchronizationContext.Current;
            _localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Greenshot");
            if (!Directory.Exists(_localAppData))
            {
                Directory.CreateDirectory(_localAppData);
            }
            
            // Cleanup old hero images cache
            foreach (var heroImagePath in Directory.EnumerateFiles(_localAppData, $"{_heroImageFilePrefix}*", SearchOption.TopDirectoryOnly))
            {
                File.Delete(heroImagePath);
            }

            _imageFilePath = Path.Combine(_localAppData, "greenshot.png");

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
        /// <param name="heroImage"></param>
        private void ShowMessage(string message, TimeSpan? timeout = default, Action onClickAction = null, Action onClosedAction = null, Image heroImage = null)
        {
            // Do not inform the user if this is disabled
            if (!CoreConfiguration.ShowTrayNotification)
            {
                return;
            }

            // Prepare the toast notifier. Be sure to specify the AppUserModelId on your application's shortcut!
            var toastNotifier = ToastNotificationManagerCompat.CreateToastNotifier();

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
                var toastBuilder = new ToastContentBuilder();
                var heroImagePath = Path.Combine(_localAppData, $"{_heroImageFilePrefix}{Guid.NewGuid()}.jpeg");
                var heroImageUri = new Uri(heroImagePath).AbsoluteUri;

                toastBuilder
                    .AddArgument("ToastID", 100)
                    .AddText(message);
                    // Profile (app logo override) image
                    //.AddAppLogoOverride(new Uri($@"file://{_imageFilePath}"), ToastGenericAppLogoCrop.None)

                if (heroImage != null)
                {
                    heroImage = heroImage.GetThumbnailImage(364, 180, () => false, IntPtr.Zero);

                    using var fileStream = new FileStream(heroImagePath, FileMode.CreateNew);
                    heroImage.Save(fileStream, ImageFormat.Jpeg);
                    toastBuilder.AddHeroImage(new Uri(heroImageUri, UriKind.Absolute));
                }

                toastBuilder.Show(toast =>
                    {
                        void DisposeNotification()
                        {
                            if (onClickAction != null)
                            {
                                toast.Activated -= ToastActivatedHandler;
                            }

                            toast.Dismissed -= ToastDismissedHandler;
                            toast.Failed -= ToastOnFailed;
                            
                            if (heroImage != null)
                            {
                                File.Delete(heroImagePath);
                            }
                        }
                        
                        // Windows 10 first with 1903: ExpiresOnReboot = true
                        toast.ExpirationTime = timeout.HasValue ? DateTimeOffset.Now.Add(timeout.Value) : null;

                        void ToastOnFailed(ToastNotification toastNotification, ToastFailedEventArgs args)
                        {
                            DisposeNotification();
                            Log.WarnFormat("Failed to display a toast due to {0}", args.ErrorCode);
                            Log.Debug(toastNotification.Content.GetXml());
                        }

                        void ToastActivatedHandler(ToastNotification toastNotification, object sender)
                        {
                            DisposeNotification();
                            InvokeAction(onClickAction);
                        }

                        void ToastDismissedHandler(ToastNotification toastNotification, ToastDismissedEventArgs eventArgs)
                        {
                            Log.Debug($"Toast closed with reason {eventArgs.Reason}");
                            if (eventArgs.Reason != ToastDismissalReason.UserCanceled)
                            {
                                return;
                            }

                            DisposeNotification();
                            
                            // Windows lets you Dismiss the notification two time, this is really odd
                            // So we need to force the removal of the notification
                            _mainSynchronizationContext.Post(ForceCloseNotification, toastNotification);

                            InvokeAction(onClosedAction);
                        }

                        if (onClickAction != null)
                        {
                            toast.Activated += ToastActivatedHandler;
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

        public void ShowWarningMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null, Image heroImage = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction, heroImage);
        }

        public void ShowErrorMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null, Image heroImage = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction, heroImage);
        }

        public void ShowInfoMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null, Image heroImage = null)
        {
            ShowMessage(message, timeout, onClickAction, onClosedAction, heroImage);
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

        private void ForceCloseNotification(object state)
        {
            _toastNotifier.Hide((ToastNotification)state);
        }
        
        private void InvokeAction(Action action)
        {
            if (action != null)
            {
                _mainSynchronizationContext.Post(InternalInvokeAction, action);
            }
        } 
        
        private void InternalInvokeAction(object state)
        {
            try
            {
                ((Action)state).Invoke();
            }
            catch (Exception ex)
            {
                Log.Warn("Exception while handling the onClosed action: ", ex);
            }
        }
    }
}