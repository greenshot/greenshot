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
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Wait for a process without blocking a thread (rule R1), .NET Framework has no Process.WaitForExitAsync.
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// Completes when the process exited. Cancellation stops the waiting, not the process.
        /// </summary>
        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            var exited = Tcs.Create<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => exited.TrySetResult(true);
            // The process could have exited before the handler was registered
            if (process.HasExited)
            {
                exited.TrySetResult(true);
            }

            return exited.Task.WaitAsync(cancellationToken);
        }

        /// <summary>
        /// Start the (configured) process and wait for it, the redirected output and error are read while it runs.
        /// </summary>
        /// <returns>exit code, standard output (null when not redirected), standard error (null when not redirected)</returns>
        public static async Task<(int ExitCode, string Output, string Error)> RunAsync(this Process process, CancellationToken cancellationToken = default)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            process.EnableRaisingEvents = true;
            process.Start();
            // Read both streams while the process runs, a full pipe buffer would block it otherwise
            var outputTask = process.StartInfo.RedirectStandardOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult<string>(null);
            var errorTask = process.StartInfo.RedirectStandardError ? process.StandardError.ReadToEndAsync() : Task.FromResult<string>(null);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            string output = await outputTask.ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);
            return (process.ExitCode, output, error);
        }
    }
}
