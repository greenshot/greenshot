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

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.DBus;
using Greenshot.Linux.Core.Ipc;

namespace Greenshot.Linux.Tests.Ipc;

public sealed class IpcTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gs-ipc-" + Guid.NewGuid().ToString("N")[..8]);

    private string SocketPath => Path.Combine(_folder, "test.sock");

    [Theory]
    [InlineData(new string[0], AppCommandKind.Start, null)]
    [InlineData(new[] { "capture", "region" }, AppCommandKind.Capture, CaptureMode.Region)]
    [InlineData(new[] { "capture", "fullscreen" }, AppCommandKind.Capture, CaptureMode.FullScreen)]
    [InlineData(new[] { "capture", "last" }, AppCommandKind.Capture, CaptureMode.LastRegion)]
    [InlineData(new[] { "capture", "activewindow" }, AppCommandKind.Capture, CaptureMode.ActiveWindow)]
    [InlineData(new[] { "--exit" }, AppCommandKind.Exit, null)]
    [InlineData(new[] { "/exit" }, AppCommandKind.Exit, null)]
    [InlineData(new[] { "--help" }, AppCommandKind.Help, null)]
    public void Command_lines_are_understood(string[] args, AppCommandKind kind, CaptureMode? mode)
    {
        var command = AppCommand.Parse(args);
        Assert.NotNull(command);
        Assert.Equal(kind, command.Kind);
        Assert.Equal(mode, command.Mode);
    }

    [Theory]
    [InlineData("capture")]
    [InlineData("capture", "everything")]
    [InlineData("dance")]
    public void Unknown_command_lines_are_rejected(params string[] args)
    {
        Assert.Null(AppCommand.Parse(args));
    }

    [Theory]
    [InlineData(CaptureMode.Region)]
    [InlineData(CaptureMode.Window)]
    [InlineData(CaptureMode.ActiveWindow)]
    [InlineData(CaptureMode.FullScreen)]
    [InlineData(CaptureMode.LastRegion)]
    public void Wire_format_round_trips(CaptureMode mode)
    {
        var command = new AppCommand(AppCommandKind.Capture, mode);
        Assert.Equal(command, AppCommand.Parse(command.ToWire().Split(' ')));
    }

    [Fact]
    public void Files_are_opened_with_absolute_paths_even_with_spaces()
    {
        Directory.CreateDirectory(_folder);
        var file = Path.Combine(_folder, "my capture.png");
        File.WriteAllBytes(file, [0]);

        var bare = AppCommand.Parse([file]);
        Assert.Equal(new AppCommand(AppCommandKind.Open, Path: file), bare);

        var wire = bare!.ToWire();
        Assert.Equal(bare, AppCommand.ParseWire(wire));
        Assert.Equal(file, AppCommand.Parse(["open", "file://" + file.Replace(" ", "%20")])!.Path);
        Assert.Equal(AppCommandKind.OpenClipboard, AppCommand.ParseWire("clipboard")!.Kind);
    }

    [Fact]
    public async Task A_second_instance_talks_to_the_first()
    {
        var received = new List<string>();
        using var channel = InstanceChannel.TryListen(line =>
        {
            received.Add(line);
            return Task.FromResult("ok");
        }, SocketPath);
        Assert.NotNull(channel);

        Assert.Null(InstanceChannel.TryListen(_ => Task.FromResult("second"), SocketPath));
        Assert.Equal("ok", await InstanceChannel.TrySendAsync("capture region", SocketPath));
        Assert.Equal(["capture region"], received);
    }

    [Fact]
    public async Task Nothing_answers_when_no_instance_runs()
    {
        Assert.Null(await InstanceChannel.TrySendAsync("capture region", SocketPath));
    }

    [Fact]
    public async Task A_stale_socket_is_taken_over()
    {
        // A crashed instance leaves its socket file, nobody listens on it.
        // .NET removes the file when a socket is disposed, so a plain file stands in for it.
        Directory.CreateDirectory(_folder);
        await File.WriteAllTextAsync(SocketPath, string.Empty);

        using var second = InstanceChannel.TryListen(_ => Task.FromResult("second"), SocketPath);
        Assert.NotNull(second);
        Assert.Equal("second", await InstanceChannel.TrySendAsync("x", SocketPath));
    }

    [Theory]
    [InlineData("PrintScreen", "Print")]
    [InlineData("Shift + PrintScreen", "SHIFT+Print")]
    [InlineData("Ctrl + Alt + A", "CTRL+ALT+a")]
    [InlineData("None", null)]
    [InlineData("", null)]
    public void Hotkeys_convert_to_xdg_triggers(string hotkey, string? expected)
    {
        Assert.Equal(expected, GlobalShortcutsPortal.ToXdgTrigger(hotkey));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
