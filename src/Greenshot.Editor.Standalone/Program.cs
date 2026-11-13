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
using Avalonia;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Ipc;

namespace Greenshot.Editor.Standalone
{
    /// <summary>
    /// greenshot-editor: the Greenshot editor as its own program.
    /// greenshot-editor [file]... opens documents (.greenshot) and images, greenshot-editor --handover file opens a capture
    /// which Greenshot handed over as a file. Only one editor runs per user, a second one gives its files to the first.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var request = StartRequest.Parse(args);
            if (SingleInstance.TrySend(request))
            {
                return 0;
            }

            try
            {
                EditorLog.Info($"Greenshot editor {typeof(Program).Assembly.GetName().Version} starts");
                SingleInstance.Listen();
                App.Request = request;
                return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                EditorLog.Error("The editor stopped with an error", ex);
                throw;
            }
            finally
            {
                SingleInstance.Stop();
            }
        }

        // Also used by the previewer of the IDE
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
    }
}
