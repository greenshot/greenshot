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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using log4net;

namespace Greenshot.Notifications
{
    /// <summary>
    /// The buttons of the notification of an export (see DestinationDispatcher.Notify): edit the kept capture, open the saved file or
    /// the upload, show the file in its folder, copy the link. A click can come after a restart, so a button carries what it acts on
    /// (<see cref="NotificationButton.Value"/>); as the arguments of a toast can come from any local process, it is checked before
    /// it is used. Not in Greenshot Light, which has no toasts.
    /// </summary>
    internal static class NotificationButtons
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotificationButtons));

        internal const string Edit = "edit";
        internal const string OpenFile = "open-file";
        internal const string ShowInFolder = "show-in-folder";
        internal const string OpenLink = "open-link";
        internal const string CopyLink = "copy-link";

        /// <summary>
        /// The buttons for the exports of a notification
        /// </summary>
        /// <param name="captureId">The kept capture, null for none</param>
        /// <param name="savedFile">The file the capture was saved to, null for none</param>
        /// <param name="uploadUrl">The link of the upload, null for none</param>
        public static IReadOnlyList<NotificationButton> Create(string captureId, string savedFile, string uploadUrl)
        {
            var buttons = new List<NotificationButton>();
            if (captureId != null)
            {
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonEdit, Edit));
            }
            if (!string.IsNullOrEmpty(savedFile))
            {
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonOpen, OpenFile, savedFile));
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonFolder, ShowInFolder, savedFile));
            }
            else if (IsWebLink(uploadUrl))
            {
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonOpen, OpenLink, uploadUrl));
            }
            if (IsWebLink(uploadUrl))
            {
                buttons.Add(new NotificationButton(Texts.Core.NotificationButtonCopylink, CopyLink, uploadUrl));
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
                    case OpenFile when File.Exists(args.Value) && !IsProgram(args.Value):
                        Process.Start(new ProcessStartInfo(args.Value) { UseShellExecute = true });
                        break;
                    case ShowInFolder when File.Exists(args.Value):
                        ExplorerHelper.OpenInExplorer(args.Value);
                        break;
                    case OpenLink when IsWebLink(args.Value):
                        Process.Start(new ProcessStartInfo(args.Value) { UseShellExecute = true });
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
        /// Greenshot saves images and documents, a button never starts a program
        /// </summary>
        private static bool IsProgram(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                return true;
            }
            var programs = (Environment.GetEnvironmentVariable("PATHEXT") ?? "") + ";.LNK;.SCR;.MSI;.MSP;.PS1;.URL;.PIF;.HTA;.CPL";
            return programs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Any(p => string.Equals(p, extension, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsWebLink(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
