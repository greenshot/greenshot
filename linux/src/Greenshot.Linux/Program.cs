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
        // Started by the desktop: files from a file manager, a greenshot: link from a browser. They may do less than the terminal.
        string source = IpcSources.Cli;
        if (args.Length > 0 && args[0] is "--open-with" or "--url")
        {
            source = args[0] == "--url" ? IpcSources.UrlScheme : IpcSources.OpenWith;
            args = args[1..];
            if (args.Length == 0)
            {
                // Started from the menu or the autostart
                source = IpcSources.Cli;
            }
        }

        // Files and links go to a Greenshot started in the background, like the options of greenshot-cli
        var command = source == IpcSources.Cli ? AppCommand.Parse(args) : null;
        switch (command?.Kind)
        {
            case AppCommandKind.Help:
                Console.WriteLine(AppCommand.Usage);
                return 0;
            case AppCommandKind.Version:
                Console.WriteLine($"Greenshot {Version}");
                return 0;
        }

        // A running Greenshot does the work, this process passes the command line on and prints the answer
        int? exitCode = RunInRunningGreenshot(args, source);
        if (exitCode != null)
        {
            return exitCode.Value;
        }

        if (command?.Kind == AppCommandKind.Exit || args.Any(a => a is "--exit"))
        {
            Console.Error.WriteLine("Greenshot is not running");
            return 0;
        }

        if (command == null)
        {
            // An option of greenshot-cli (-l, -r, ...) or a link: Greenshot starts in the background and answers it
            if (!StartInBackground())
            {
                Console.Error.WriteLine("Greenshot could not be started");
                return 1;
            }

            return RunInRunningGreenshot(args, source) ?? 1;
        }

        StartCommand = command;
        Log.Info($"Greenshot {Version} starts on {Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "an unknown desktop"}");
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
    }

    private static int? RunInRunningGreenshot(string[] args, string source)
    {
        try
        {
            return InstanceClient.RunCommandLineAsync(args, Environment.CurrentDirectory, Console.Out, Console.Error, source: source).GetAwaiter().GetResult();
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"The connection to Greenshot failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Starts Greenshot in the tray and waits until it listens
    /// </summary>
    private static bool StartInBackground()
    {
        string? process = Environment.ProcessPath;
        if (process == null)
        {
            return false;
        }

        // "dotnet greenshot.dll" when it isn't published. In a session of its own, without the terminal: its log doesn't
        // end up in the output of this command, and closing the terminal doesn't stop it.
        var arguments = Path.GetFileNameWithoutExtension(process) == "dotnet" ? new[] { typeof(Program).Assembly.Location } : [];
        ProcessRunner.StartDetached("/bin/sh", ["-c", "command -v setsid >/dev/null && exec setsid \"$@\" </dev/null >/dev/null 2>&1 || exec \"$@\" </dev/null >/dev/null 2>&1",
            "greenshot", process, .. arguments]);
        var waited = System.Diagnostics.Stopwatch.StartNew();
        while (waited.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (InstanceChannel.IsRunning())
            {
                return true;
            }

            Thread.Sleep(200);
        }

        return false;
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
