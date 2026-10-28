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

using Greenshot.Linux.Core.Compositor;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Ai;

/// <summary>
/// Consent, exclusions and notifications for AI tools (MCP clients) which use Greenshot through greenshot-mcp.
/// An AI tool can see whatever is on the screen, so:
/// - nothing works until the user allowed that program (identified by Greenshot, see <see cref="AiToolCaller"/>; asked the first
///   time it connects, stored in AiToolsAllowedClients and revocable in the settings),
/// - windows of excluded applications (password managers by default) are never listed and are blacked out in captures,
/// - every capture shows a notification (AiToolsNotifyOnCapture).
/// </summary>
public static class AiToolAccess
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AiToolAccess));
    private static readonly SemaphoreSlim PromptLock = new(1, 1);
    private static readonly HashSet<string> DeniedClients = new(StringComparer.Ordinal);

    /// <summary>
    /// Asks the user whether the AI tool may use Greenshot, set by the app; without it nobody is allowed
    /// </summary>
    public static Func<AiToolClient, CancellationToken, Task<bool>>? ConsentPrompt { get; set; }

    /// <summary>
    /// The configuration to use, replaceable for tests
    /// </summary>
    internal static Func<CoreSettings?> ConfigurationProvider { get; set; } = () => RecipeServices.Settings;

    public const string NotAllowedMessage = "The user did not allow this program to use Greenshot. Greenshot doesn't ask again until it is restarted; " +
                                            "the user can allow it in the Greenshot settings (AI tools tab), or click \"Ask again\" there.";

    public const string DisabledMessage = "AI tools are switched off in Greenshot. To use Greenshot from an AI tool, switch them on in the Greenshot settings (AI tools tab).";

    public const string ProposalsDisabledMessage = "The user switched off recipe proposals by AI tools in the Greenshot settings (AI tools tab).";

    /// <summary>
    /// AI tools may use Greenshot at all (opt-in, AiToolsEnabled)
    /// </summary>
    public static bool IsEnabled => ConfigurationProvider()?.AiToolsEnabled == true;

    /// <summary>
    /// AI tools may propose recipes (AiToolsAllowRecipeProposals)
    /// </summary>
    public static bool AreRecipeProposalsAllowed => ConfigurationProvider()?.AiToolsAllowRecipeProposals != false;

    /// <summary>
    /// The programs the user allowed (AiToolsAllowedClients)
    /// </summary>
    public static IReadOnlyList<string> AllowedClients =>
        (ConfigurationProvider()?.AiToolsAllowedClients ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();

    public static bool IsAllowed(AiToolClient? client) =>
        client != null && !string.IsNullOrEmpty(client.Identity) && AllowedClients.Contains(client.Identity, StringComparer.Ordinal);

    /// <summary>
    /// True when the user allowed the program; asks once (per program and Greenshot run) when not
    /// </summary>
    /// <param name="client">The program, as identified by Greenshot; null (unidentified) is never allowed</param>
    public static async Task<bool> EnsureAllowedAsync(AiToolClient? client, CancellationToken cancellationToken = default)
    {
        var config = ConfigurationProvider();
        if (config == null || client == null || string.IsNullOrEmpty(client.Identity))
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
                if (DeniedClients.Contains(client.Identity))
                {
                    return false;
                }
            }

            bool allowed = ConsentPrompt != null && await ConsentPrompt(client, cancellationToken).ConfigureAwait(false);
            if (!allowed)
            {
                lock (DeniedClients)
                {
                    DeniedClients.Add(client.Identity);
                }

                Log.Info($"The user did not allow {client} to use Greenshot.");
                return false;
            }

            void Save()
            {
                config.AiToolsAllowedClients = AllowedClients.Append(client.Identity).Distinct(StringComparer.Ordinal).ToList();
                try
                {
                    config.Save();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Log.Warn("Could not save the configuration after allowing an AI tool", ex);
                }
            }

            // The configuration is written on the UI thread, like the settings window does
            if (RecipeServices.Ui != null)
            {
                await RecipeServices.Ui.InvokeAsync(Save, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Save();
            }

            Log.Info($"The user allowed {client} to use Greenshot.");
            return true;
        }
        finally
        {
            PromptLock.Release();
        }
    }

    /// <summary>
    /// The programs the user didn't allow in this Greenshot run: they aren't asked again until Greenshot restarts or the
    /// user clicks "Ask again" in the settings
    /// </summary>
    public static IReadOnlyList<string> GetDeniedClients()
    {
        lock (DeniedClients)
        {
            return DeniedClients.Order(StringComparer.Ordinal).ToList();
        }
    }

    /// <summary>
    /// Forget the "don't allow" of this program: the next request asks again
    /// </summary>
    public static void ForgetDenied(string identity)
    {
        lock (DeniedClients)
        {
            DeniedClients.Remove(identity);
        }
    }

    internal static void ResetDeniedClients()
    {
        lock (DeniedClients)
        {
            DeniedClients.Clear();
        }
    }

    /// <summary>
    /// The applications excluded from AI tools (AiToolsExcludedProcesses)
    /// </summary>
    public static IReadOnlyList<string> ExcludedApplications =>
        (ConfigurationProvider()?.AiToolsExcludedProcesses ?? []).Select(e => e.Trim()).Where(e => e.Length > 0).ToList();

    /// <summary>
    /// True when windows of the process with this name may not be listed or captured by AI tools
    /// </summary>
    public static bool IsProcessExcluded(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        string name = processName.Trim();
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        return ExcludedApplications.Any(e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// True when the window belongs to an excluded application: by its process (name or executable) or its app id
    /// (org.keepassxc.KeePassXC matches "KeePassXC" and "org.keepassxc.KeePassXC")
    /// </summary>
    public static bool IsWindowExcluded(WindowInfo window)
    {
        if (ExcludedApplications.Count == 0)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(window.AppId) &&
            (IsProcessExcluded(window.AppId) || IsProcessExcluded(window.AppId[(window.AppId.LastIndexOf('.') + 1)..])))
        {
            return true;
        }

        if (window.ProcessId > 0)
        {
            return IsProcessExcluded(AiToolCaller.GetProcessName(window.ProcessId)) ||
                   IsProcessExcluded(Path.GetFileName(AiToolCaller.GetExecutable(window.ProcessId)));
        }

        return false;
    }

    /// <summary>
    /// The name of the window's application for the AI tool: the app id, else the process name
    /// </summary>
    public static string GetApplicationName(WindowInfo window) =>
        !string.IsNullOrEmpty(window.AppId) ? window.AppId : window.ProcessId > 0 ? AiToolCaller.GetProcessName(window.ProcessId) : string.Empty;

    /// <summary>
    /// Shows a notification that an AI tool took a capture (when enabled)
    /// </summary>
    public static void NotifyCapture(string? clientName, string what)
    {
        if (ConfigurationProvider()?.AiToolsNotifyOnCapture != true || RecipeServices.UserInteraction is not { } interaction)
        {
            return;
        }

        string client = string.IsNullOrWhiteSpace(clientName) ? "An AI tool" : clientName.Trim();
        _ = interaction.NotifyAsync(NotificationKind.Info, $"{client} captured {what} with Greenshot.")
            .ContinueWith(t => Log.Warn("The notification of an AI tool capture failed", t.Exception), TaskContinuationOptions.OnlyOnFaulted);
    }
}
