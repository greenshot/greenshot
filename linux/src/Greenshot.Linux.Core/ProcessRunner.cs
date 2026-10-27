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

using System.Diagnostics;

namespace Greenshot.Linux.Core;

public sealed record ProcessResult(int ExitCode, byte[] Output, string Error)
{
    public string OutputText => System.Text.Encoding.UTF8.GetString(Output);
}

/// <summary>
/// Runs helper programs like grim, swaymsg and wl-copy. Arguments are passed as a list, never through a shell.
/// </summary>
public static class ProcessRunner
{
    /// <param name="captureOutput">
    /// False for programs which leave a child running in the background, like wl-copy: the child would keep the pipes open
    /// and reading them would wait until it ends
    /// </param>
    public static async Task<ProcessResult> RunAsync(string fileName, IEnumerable<string> arguments, byte[]? input = null,
        TimeSpan? timeout = null, CancellationToken cancellationToken = default, bool captureOutput = true, string? workingDirectory = null)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory ?? string.Empty,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            RedirectStandardInput = input != null,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {fileName}");
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));

        var outputTask = captureOutput ? ReadAllAsync(process.StandardOutput.BaseStream, timeoutSource.Token) : Task.FromResult(Array.Empty<byte>());
        var errorTask = captureOutput ? process.StandardError.ReadToEndAsync(timeoutSource.Token) : Task.FromResult(string.Empty);
        if (input != null)
        {
            await process.StandardInput.BaseStream.WriteAsync(input, timeoutSource.Token).ConfigureAwait(false);
            process.StandardInput.Close();
        }

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return new ProcessResult(process.ExitCode, await outputTask.ConfigureAwait(false), await errorTask.ConfigureAwait(false));
    }

    /// <summary>
    /// Start a program and don't wait for it, like xdg-open
    /// </summary>
    public static void StartDetached(string fileName, params string[] arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo(fileName) { UseShellExecute = false };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var _ = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not start {fileName}", ex);
        }
    }

    public static bool IsOnPath(string program)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(':', StringSplitOptions.RemoveEmptyEntries)
            .Any(dir => File.Exists(Path.Combine(dir, program)));
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return memory.ToArray();
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already gone
        }
    }
}
