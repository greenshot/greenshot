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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using log4net;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Consent, exclusions and notifications for AI tools (MCP clients) which use Greenshot through greenshot-mcp.exe.
    /// An AI tool can see whatever is on the screen, so:
    /// - nothing works until the user allowed that program (identified by Greenshot, see <see cref="AiToolCaller"/>; asked the first
    ///   time it connects, stored in AiToolsAllowedClients and revocable in the settings),
    /// - windows of excluded processes (password managers by default) are never listed or captured,
    /// - every capture shows a notification (AiToolsNotifyOnCapture).
    /// </summary>
    public static class AiToolAccess
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AiToolAccess));
        private static readonly SemaphoreSlim PromptLock = new SemaphoreSlim(1, 1);
        private static readonly HashSet<string> DeniedClients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Asks the user whether the AI tool may use Greenshot. Replaceable for tests.
        /// </summary>
        internal static Func<AiToolClient, CancellationToken, Task<bool>> ConsentPrompt { get; set; } = ShowConsentPromptAsync;

        /// <summary>
        /// The configuration to use, replaceable for tests.
        /// </summary>
        internal static Func<ICoreConfiguration> ConfigurationProvider { get; set; } = () => IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>
        /// The message for a rejected request
        /// </summary>
        public const string NotAllowedMessage = "The user did not allow this program to use Greenshot. Allowed programs are managed in the Greenshot settings (AI tools).";

        /// <summary>
        /// True when the user allowed this program (AiToolsAllowedClients).
        /// </summary>
        public static bool IsAllowed(AiToolClient client)
        {
            var allowed = ConfigurationProvider()?.AiToolsAllowedClients;
            return client != null && !string.IsNullOrEmpty(client.ExePath) && allowed != null &&
                   allowed.Any(path => string.Equals(path?.Trim(), client.ExePath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns true when the user allowed the program; asks once (per program and Greenshot run) when not.
        /// </summary>
        /// <param name="client">The program, as identified by Greenshot; null (unidentified) is never allowed</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static async Task<bool> EnsureAllowedAsync(AiToolClient client, CancellationToken cancellationToken = default)
        {
            var config = ConfigurationProvider();
            if (config == null || client == null || string.IsNullOrEmpty(client.ExePath))
            {
                return false;
            }
            if (IsAllowed(client))
            {
                return true;
            }

            await PromptLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Another request may have been answered while this one waited
                if (IsAllowed(client))
                {
                    return true;
                }
                lock (DeniedClients)
                {
                    if (DeniedClients.Contains(client.ExePath))
                    {
                        return false;
                    }
                }

                bool allowed = await ConsentPrompt(client, cancellationToken).ConfigureAwait(false);
                if (!allowed)
                {
                    lock (DeniedClients)
                    {
                        DeniedClients.Add(client.ExePath);
                    }
                    Log.Info($"The user did not allow {client} to use Greenshot.");
                    return false;
                }

                // Configuration is written on the UI thread (single writer, its change events have UI subscribers)
                await UiDispatcher.Current.InvokeAsync(() =>
                {
                    var allowedClients = new List<string>(config.AiToolsAllowedClients ?? new List<string>()) { client.ExePath };
                    config.AiToolsAllowedClients = allowedClients;
                    try
                    {
                        IniConfigRegistry.Get()?.Save();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Could not save the configuration after allowing an AI tool", ex);
                    }
                }, cancellationToken).ConfigureAwait(false);
                Log.Info($"The user allowed {client} to use Greenshot.");
                return true;
            }
            finally
            {
                PromptLock.Release();
            }
        }

        /// <summary>
        /// Forget the "denied" answers of this Greenshot run (for tests).
        /// </summary>
        internal static void ResetDeniedClients()
        {
            lock (DeniedClients)
            {
                DeniedClients.Clear();
            }
        }

        /// <summary>
        /// True when windows of the process with this name may not be listed or captured by AI tools.
        /// </summary>
        /// <param name="processName">Process name, with or without .exe</param>
        public static bool IsProcessExcluded(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }
            var excluded = ConfigurationProvider()?.AiToolsExcludedProcesses;
            if (excluded == null || excluded.Count == 0)
            {
                return false;
            }
            string name = NormalizeProcessName(processName);
            return excluded.Any(e => string.Equals(NormalizeProcessName(e), name, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeProcessName(string processName)
        {
            string name = (processName ?? string.Empty).Trim();
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4);
            }
            return name;
        }

        /// <summary>
        /// Shows a notification that an AI tool took a capture (when enabled).
        /// </summary>
        public static void NotifyCapture(string clientName, string what)
        {
            if (ConfigurationProvider()?.AiToolsNotifyOnCapture != true)
            {
                return;
            }
            var notificationService = SimpleServiceProvider.Current?.GetInstance<INotificationService>(isOptional: true);
            if (notificationService == null)
            {
                return;
            }
            string client = string.IsNullOrWhiteSpace(clientName) ? "An AI tool" : clientName.Trim();
            string message = $"{client} captured {what} with Greenshot.";
            UiDispatcher.Current.RunOnUiAsync(() => notificationService.ShowInfoMessage(message, TimeSpan.FromSeconds(5))).FireAndLog("AI tool capture notification", Log);
        }

        private static Task<bool> ShowConsentPromptAsync(AiToolClient client, CancellationToken cancellationToken)
        {
            string signer = string.IsNullOrEmpty(client.Signer) ? "This program is not signed." : $"Signed by: {client.Signer}";
            string text = $"{client.DisplayName} wants to use Greenshot to list your windows and take screenshots, and to run Greenshot recipes.\r\n\r\n" +
                          $"Program: {client.ExePath}\r\n{signer}\r\n\r\n" +
                          "Screenshots can contain anything that is visible on your screen. Windows of excluded applications " +
                          "(password managers by default, see AiToolsExcludedProcesses in greenshot.ini) are never shared. " +
                          "You can remove the permission in the Greenshot settings.\r\n\r\n" +
                          $"Allow {client.DisplayName} to use Greenshot?";
            return UiDispatcher.Current.InvokeAsync(() =>
            {
                // On top, so the question doesn't end up behind the AI tool's window
                int choice = ThemedMessageBox.ShowChoice(null, "AI Tool Access", text, System.Windows.MessageBoxImage.Question,
                    new[] { "Allow", "Don't Allow" }, defaultIndex: 1, cancelIndex: 1, onTop: true);
                return choice == 0;
            }, cancellationToken);
        }
    }
}
