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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Greenshot.Mcp
{
    /// <summary>
    /// What greenshot-mcp knows about the running Greenshot
    /// </summary>
    public static class GreenshotState
    {
        private static volatile bool _isClosedByUser;

        /// <summary>
        /// The user exited Greenshot: tool calls don't start it again, they say that Greenshot was closed
        /// </summary>
        public static bool IsClosedByUser => _isClosedByUser;

        /// <summary>
        /// Called with the reason Greenshot sends when it exits
        /// </summary>
        public static event Action<string>? ShuttingDown;

        internal static void OnShutdown(string? reason)
        {
            reason ??= "exit";
            _isClosedByUser = string.Equals(reason, "exit", StringComparison.OrdinalIgnoreCase);
            ShuttingDown?.Invoke(reason);
        }

        internal static void OnConnected()
        {
            _isClosedByUser = false;
        }
    }

    /// <summary>
    /// Keeps a watch connection to Greenshot: new or changed AI tool recipes are synced right away, and when Greenshot exits
    /// greenshot-mcp waits for its next start (the user closed it) or exits (an installer updates or removes Greenshot, a
    /// running greenshot-mcp.exe would keep the installation directory locked).
    /// </summary>
    public sealed class GreenshotWatch : BackgroundService
    {
        private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(3);
        // An older Greenshot without WATCH: the 30 s sync of the tools still works
        private static readonly TimeSpan OldGreenshotRetryInterval = TimeSpan.FromMinutes(1);

        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<GreenshotWatch> _logger;

        public GreenshotWatch(IHostApplicationLifetime lifetime, ILogger<GreenshotWatch> logger)
        {
            _lifetime = lifetime;
            _logger = logger;
            GreenshotState.ShuttingDown += OnShuttingDown;
        }

        private void OnShuttingDown(string reason)
        {
            if (string.Equals(reason, "update", StringComparison.OrdinalIgnoreCase) || string.Equals(reason, "uninstall", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Greenshot is being updated or removed, greenshot-mcp exits.");
                _lifetime.StopApplication();
            }
            else
            {
                _logger.LogInformation("Greenshot exited ({Reason}), waiting for it to be started again.", reason);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan wait = RetryInterval;
                try
                {
                    var (pipe, isRunning) = await GreenshotConnection.OpenWatchAsync(stoppingToken).ConfigureAwait(false);
                    if (pipe != null)
                    {
                        await using (pipe.ConfigureAwait(false))
                        {
                            GreenshotState.OnConnected();
                            RecipeToolSync.RequestSync();
                            await ReadEventsAsync(pipe, stoppingToken).ConfigureAwait(false);
                        }
                    }
                    else if (isRunning)
                    {
                        wait = OldGreenshotRetryInterval;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex) when (ex is GreenshotConnectionException || ex is IOException || ex is UnauthorizedAccessException)
                {
                    _logger.LogDebug(ex, "The watch connection to Greenshot ended.");
                }

                try
                {
                    await Task.Delay(wait, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static async Task ReadEventsAsync(Stream pipe, CancellationToken cancellationToken)
        {
            while (true)
            {
                var frame = await GreenshotConnection.ReadFrameAsync(pipe, cancellationToken).ConfigureAwait(false);
                if (frame == null)
                {
                    return;
                }
                string? greenshotEvent = (string?)frame["event"];
                if (string.Equals(greenshotEvent, "tools_changed", StringComparison.OrdinalIgnoreCase))
                {
                    RecipeToolSync.RequestSync();
                }
                else if (string.Equals(greenshotEvent, "shutdown", StringComparison.OrdinalIgnoreCase))
                {
                    GreenshotState.OnShutdown((string?)frame["reason"]);
                    return;
                }
            }
        }
    }
}
