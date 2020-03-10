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
using System.IO;
using System.Linq;
using Windows.UI.Notifications;
using Greenshot.Helpers;
using log4net;

namespace GreenshotWin10Plugin
{
    public class ToastNotificationService : INotificationService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ToastNotificationService));

        private void ShowMessage(string message, Action onClickAction, Action onClosedAction)
        {
            // Get a toast XML template
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);

            // Fill in the text elements
            var stringElement = toastXml.GetElementsByTagName("text").First();
            stringElement.AppendChild(toastXml.CreateTextNode(message));

            // Specify the absolute path to an image
            //string imagePath = "file:///" + Path.GetFullPath("toastImageAndText.png");
            //var imageElement = toastXml.GetElementsByTagName("image").First();
            //imageElement.Attributes.GetNamedItem("src").NodeValue = imagePath;

            // Create the toast and attach event listeners
            var toast = new ToastNotification(toastXml);

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
            }
            toast.Dismissed += ToastDismissedHandler;
            // Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
            ToastNotificationManager.CreateToastNotifier(@"Greenshot").Show(toast);
        }

        public void ShowWarningMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, onClickAction, onClosedAction);
        }

        public void ShowErrorMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, onClickAction, onClosedAction);
        }

        public void ShowInfoMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, onClickAction, onClosedAction);
        }
    }
}
