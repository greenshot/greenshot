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

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Greenshot.Linux.Core;

namespace Greenshot.Linux;

public partial class App : Application
{
    private GreenshotController? _controller;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // A tray icon or portal which loses its D-Bus connection must not take the capture tool down with it
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            if (e.Exception is OperationCanceledException)
            {
                // The D-Bus tray icon of Avalonia cancels its watch on shutdown
                Log.Debug($"Cancelled: {e.Exception.Message}");
            }
            else
            {
                Log.Error("Unhandled error", e.Exception);
            }

            e.Handled = true;
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _controller = new GreenshotController(desktop);
            desktop.Exit += (_, _) => _controller.Dispose();
            _ = _controller.StartAsync(Program.StartCommand);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
