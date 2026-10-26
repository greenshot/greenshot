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

using System.Reflection;
using Avalonia;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Ipc;

namespace Greenshot.Linux;

internal static class Program
{
    /// <summary>
    /// The command the first instance runs after starting, e.g. a capture when "greenshot capture region" started it
    /// </summary>
    internal static AppCommand StartCommand { get; private set; } = new(AppCommandKind.Start);

    [STAThread]
    public static int Main(string[] args)
    {
        var command = AppCommand.Parse(args);
        switch (command?.Kind)
        {
            case null:
                Console.Error.WriteLine($"Unknown command: {string.Join(' ', args)}");
                Console.Error.WriteLine();
                Console.Error.WriteLine(AppCommand.Usage);
                return 2;
            case AppCommandKind.Help:
                Console.WriteLine(AppCommand.Usage);
                return 0;
            case AppCommandKind.Version:
                Console.WriteLine($"Greenshot {Version}");
                return 0;
        }

        // A running Greenshot does the work, this process only passes the command on
        var reply = InstanceChannel.TrySendAsync(command.ToWire()).GetAwaiter().GetResult();
        if (reply != null)
        {
            if (reply.StartsWith("error", StringComparison.Ordinal))
            {
                Console.Error.WriteLine(reply);
                return 1;
            }

            return 0;
        }

        if (command.Kind == AppCommandKind.Exit)
        {
            Console.Error.WriteLine("Greenshot is not running");
            return 0;
        }

        StartCommand = command;
        Log.Info($"Greenshot {Version} starts on {Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "an unknown desktop"}");
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
    }

    public static string Version =>
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "?";

    // Also used by the Avalonia designer
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions { WmClass = "greenshot" })
            .LogToTrace();
}
