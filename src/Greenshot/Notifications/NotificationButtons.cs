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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using log4net;

namespace Greenshot.Notifications
{
    /// <summary>
    /// The buttons of the notification of the exports (see the Notification step): what acts on the capture (edit the kept capture,
    /// show the saved file in its folder) and what acts on an export (copy its link; with several links a drop-down chooses which).
    /// Nothing opens a file or a link: that only asks Windows for the app, and a link of a QR code may be anything. A click can come
    /// after a restart, so a button carries what it acts on (<see cref="NotificationButton.Value"/>); as the arguments of a toast can
    /// come from any local process, it is checked before it is used. Not in Greenshot Light, which has no toasts.
    /// </summary>
    internal static class NotificationButtons
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotificationButtons));

        internal const string Edit = "edit";
        internal const string ShowInFolder = "show-in-folder";
        internal const string CopyLink = "copy-link";

        /// <summary>
        /// The buttons for the exports of a notification
        /// </summary>
        /// <param name="captureId">The kept capture, null for none</param>
        /// <param name="savedFile">The file the capture was saved to, null for none</param>
        /// <param name="links">The links of the exports (and of the QR codes), with the text the drop-down shows</param>
        public static IReadOnlyList<NotificationButton> Create(string captureId, string savedFile, IReadOnlyList<NotificationChoice> links)
        {
            var buttons = new List<NotificationButton>();
            if (captureId != null)
            {
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonEdit, Edit));
            }
            if (!string.IsNullOrEmpty(savedFile))
            {
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonFolder, ShowInFolder, savedFile));
            }
            var webLinks = (links ?? Array.Empty<NotificationChoice>()).Where(l => IsWebLink(l.Value)).GroupBy(l => l.Value).Select(g => g.First()).ToList();
            if (webLinks.Count == 1)
            {
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonCopylink, CopyLink, webLinks[0].Value));
            }
            else if (webLinks.Count > 1)
            {
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonCopylink, CopyLink, webLinks));
            }
            return buttons;
        }

        /// <summary>
        /// Runs the clicked button, on the UI thread
        /// </summary>
        public static void Run(NotificationButtonEventArgs args)
        {
            try
            {
                switch (args.Action)
                {
                    case Edit:
                        NotificationCaptures.EditCapture(args.CaptureId);
                        return;
                    case ShowInFolder when File.Exists(args.Value):
                        ExplorerHelper.OpenInExplorer(args.Value);
                        break;
                    case CopyLink when IsWebLink(args.Value):
                        ClipboardHelper.SetClipboardData(args.Value);
                        break;
                    default:
                        Log.WarnFormat("Ignoring the notification button '{0}' with '{1}'.", args.Action, args.Value);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"The notification button '{args.Action}' failed.", ex);
            }
            // The toast is gone with the click, its capture isn't needed anymore
            NotificationCaptures.Discard(args.CaptureId);
        }

        /// <summary>
        /// Only http and https are links: no file:, javascript: or the protocol of an app
        /// </summary>
        internal static bool IsWebLink(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
