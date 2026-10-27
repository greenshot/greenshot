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

#nullable disable


using System.Diagnostics;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Output;

namespace Greenshot.Linux.Recipes.Triggers
{
    /// <summary>
    /// Runs the recipe when an image is copied by another application. Wayland has no clipboard event for applications:
    /// "wl-paste --watch" reports each change; changes right after Greenshot's own copies are ignored.
    /// </summary>
    public class ClipboardTrigger : TriggerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClipboardTrigger));
        private Process _watcher;

        public override string TriggerType => TriggerConfig.TypeClipboard;

        public bool OnImageCopied { get; set; } = true;

        public string FormatFilter { get; set; }

        public ClipboardTrigger(string id, string name, string targetRecipeId, bool onImageCopied = true, string formatFilter = null)
            : base(id, name, targetRecipeId)
        {
            OnImageCopied = onImageCopied;
            FormatFilter = formatFilter;
        }

        public ClipboardTrigger(string targetRecipeId, TriggerConfig config)
            : base(Guid.NewGuid().ToString("N"), config?.Name ?? "Clipboard Monitor", targetRecipeId)
        {
            if (config != null)
            {
                OnImageCopied = config.GetParameter<bool>("OnImageCopied", true);
                FormatFilter = config.GetParameter<string>("FormatFilter");
            }
        }

        public override void Start()
        {
            Stop();
            if (!ProcessRunner.IsOnPath("wl-paste"))
            {
                Log.WarnFormat("ClipboardTrigger '{0}' needs wl-paste (wl-clipboard)", Name);
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo("wl-paste") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var argument in new[] { "--watch", "echo", "changed" })
                {
                    startInfo.ArgumentList.Add(argument);
                }

                var watcher = Process.Start(startInfo);
                _watcher = watcher;
                bool first = true;
                watcher.OutputDataReceived += (_, e) =>
                {
                    if (e.Data == null)
                    {
                        return;
                    }

                    // The watch starts with the current clipboard, the trigger only fires for changes after it started
                    if (first)
                    {
                        first = false;
                        return;
                    }

                    _ = OnClipboardChangedAsync();
                };
                watcher.BeginOutputReadLine();
                Log.InfoFormat("Started ClipboardTrigger: {0} ({1})", Name, Id);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                Log.Error($"Failed to start ClipboardTrigger '{Name}'", ex);
            }
        }

        public override void Stop()
        {
            var watcher = _watcher;
            _watcher = null;
            if (watcher == null)
            {
                return;
            }

            try
            {
                if (!watcher.HasExited)
                {
                    watcher.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // Ended already
            }

            watcher.Dispose();
            Log.InfoFormat("Stopped ClipboardTrigger: {0} ({1})", Name, Id);
        }

        private async Task OnClipboardChangedAsync()
        {
            if (!IsEnabled) return;
            try
            {
                if (DateTime.UtcNow - SystemClipboard.LastSetUtc < TimeSpan.FromSeconds(2))
                {
                    Log.DebugFormat("ClipboardTrigger '{0}' ignoring Greenshot's own copy", Name);
                    return;
                }

                var formats = await SystemClipboard.GetTypesAsync().ConfigureAwait(false);
                if (OnImageCopied && !formats.Any(f => f.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
                {
                    Log.DebugFormat("ClipboardTrigger '{0}' skipped: clipboard update does not contain an image.", Name);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(FormatFilter))
                {
                    // PNG matches image/png, a MIME type matches itself
                    var filters = FormatFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (!formats.Any(f => filters.Any(filter => f.Contains(filter, StringComparison.OrdinalIgnoreCase))))
                    {
                        Log.DebugFormat("ClipboardTrigger '{0}' skipped: clipboard formats [{1}] do not match filter '{2}'.", Name, string.Join(", ", formats), FormatFilter);
                        return;
                    }
                }

                Log.InfoFormat("ClipboardTrigger '{0}' fired for target recipe '{1}' (formats: {2})", Name, TargetRecipeId, string.Join(", ", formats));
                OnTriggered(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Clipboard.Formats"] = formats.ToList(),
                    ["Clipboard.Format"] = formats.FirstOrDefault() ?? "Unknown",
                });
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                Log.Error($"Error evaluating clipboard update in ClipboardTrigger '{Name}'", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Stop();
            }
        }
    }
}
