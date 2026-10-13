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
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using Xunit;

namespace Greenshot.Tests.Shell
{
    public class TrayIconTests
    {
        [InteractiveDesktopFact]
        public void CreateTaskbarIcon_UsesTheLegacyIconIdAndShowsItOnTheTaskbar()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using (var icon = new Icon(SystemIcons.Application, 16, 16))
                    {
                        var shown = Greenshot.Shell.TrayIcon.CreateTaskbarIcon(icon, "Greenshot");
                        try
                        {
                            Assert.True(shown.IsTaskbarIconCreated);
                            Assert.Equal(1, PromotedState(Greenshot.Shell.TrayIcon.LegacyTrayIconId));
                            // Vista messages (version 4) show the icon and then deliver no click.
                            Assert.Equal(3u, NotifyMessageVersion(shown));
                        }
                        finally
                        {
                            shown.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(20)));
            Assert.Null(threadEx);
        }

        /// <summary>
        /// The version the message window will interpret. 3 is NOTIFYICON_VERSION (Windows 2000).
        /// </summary>
        private static uint NotifyMessageVersion(TaskbarIcon icon)
        {
            object sink = typeof(TaskbarIcon).GetField("messageSink", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(icon);
            object version = sink?.GetType().GetProperty("Version")?.GetValue(sink);
            return Convert.ToUInt32(version);
        }

        /// <summary>
        /// IsPromoted for this test process and icon id: 1 shown, 0 hidden, null when Windows has no entry.
        /// </summary>
        private static int? PromotedState(uint iconId)
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            using (var settings = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings"))
            {
                if (settings == null)
                {
                    return null;
                }

                foreach (var name in settings.GetSubKeyNames())
                {
                    using (var iconKey = settings.OpenSubKey(name))
                    {
                        if (iconKey == null)
                        {
                            continue;
                        }

                        if (!string.Equals(iconKey.GetValue("ExecutablePath") as string, exePath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!(iconKey.GetValue("UID") is int id) || unchecked((uint)id) != iconId)
                        {
                            continue;
                        }

                        return iconKey.GetValue("IsPromoted") as int?;
                    }
                }
            }

            return null;
        }
    }
}
