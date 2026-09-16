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
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Triggers;
using log4net;

namespace Greenshot.Triggers
{
    /// <summary>
    /// Event-driven clipboard monitor trigger.
    /// Listens for OS clipboard update events, checks clipboard ownership to prevent
    /// self-triggering loops from Greenshot's own clipboard operations, and invokes
    /// the target recipe when external clipboard content matches criteria.
    /// </summary>
    public class ClipboardTrigger : TriggerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClipboardTrigger));
        private IDisposable _subscription;
        private readonly int _currentProcessId;

        public override string TriggerType => TriggerConfig.TypeClipboard;
        public bool OnImageCopied { get; set; } = true;
        public string FormatFilter { get; set; }

        public ClipboardTrigger(string id, string name, string targetRecipeId, bool onImageCopied = true, string formatFilter = null)
            : base(id, name, targetRecipeId)
        {
            OnImageCopied = onImageCopied;
            FormatFilter = formatFilter;
            _currentProcessId = Process.GetCurrentProcess().Id;
        }

        public ClipboardTrigger(string targetRecipeId, TriggerConfig config)
            : base(Guid.NewGuid().ToString("N"), config?.Name ?? "Clipboard Monitor", targetRecipeId)
        {
            if (config != null)
            {
                OnImageCopied = config.GetParameter<bool>("OnImageCopied", true);
                FormatFilter = config.GetParameter<string>("FormatFilter");
            }
            _currentProcessId = Process.GetCurrentProcess().Id;
        }

        public override void Start()
        {
            Stop();

            try
            {
                _subscription = ClipboardNative.OnUpdate
                    .ObserveOn(System.Reactive.Concurrency.Scheduler.Default)
                    .Subscribe(OnClipboardUpdateReceived, ex => Log.Error("Error in ClipboardNative.OnUpdate stream", ex));

                Log.InfoFormat("Started ClipboardTrigger: {0} ({1})", Name, Id);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start ClipboardTrigger '{Name}'", ex);
            }
        }

        public override void Stop()
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
                Log.InfoFormat("Stopped ClipboardTrigger: {0} ({1})", Name, Id);
            }
        }

        private void OnClipboardUpdateReceived(ClipboardUpdateInformation updateInfo)
        {
            if (!IsEnabled) return;

            try
            {
                // 1. Strict Ownership Check:
                // Check if the window that updated the clipboard belongs to this Greenshot process.
                IntPtr ownerHwnd = updateInfo.OwnerHandle != IntPtr.Zero ? updateInfo.OwnerHandle : ClipboardNative.CurrentOwner;
                if (ownerHwnd != IntPtr.Zero)
                {
                    User32Api.GetWindowThreadProcessId(ownerHwnd, out var ownerProcessId);
                    if (ownerProcessId == _currentProcessId)
                    {
                        Log.DebugFormat("ClipboardTrigger '{0}' ignoring clipboard update from own process (PID: {1})", Name, ownerProcessId);
                        return;
                    }
                }

                // 2. Format & Image Check
                var formats = updateInfo.Formats?.ToList() ?? new List<string>();
                bool hasImage = false;

                if (OnImageCopied)
                {
                    // Check standard formats or ClipboardHelper
                    hasImage = formats.Any(f =>
                        string.Equals(f, "PNG", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f, "DeviceIndependentBitmap", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f, "Format17", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f, "Bitmap", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f, "System.Drawing.Bitmap", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f, DataFormats.Bitmap, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f, DataFormats.Dib, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f, DataFormats.Tiff, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f, DataFormats.FileDrop, StringComparison.OrdinalIgnoreCase));

                    if (!hasImage)
                    {
                        // Fallback check on STA/UI or direct check
                        hasImage = ClipboardHelper.ContainsImage();
                    }

                    if (!hasImage)
                    {
                        Log.DebugFormat("ClipboardTrigger '{0}' skipped: clipboard update does not contain an image.", Name);
                        return;
                    }
                }

                // 3. FormatFilter Check (if specified, e.g. "PNG", "DIB", "DeviceIndependentBitmap")
                if (!string.IsNullOrWhiteSpace(FormatFilter))
                {
                    bool matchesFilter = formats.Any(f => string.Equals(f, FormatFilter, StringComparison.OrdinalIgnoreCase)
                        || f.IndexOf(FormatFilter, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!matchesFilter)
                    {
                        Log.DebugFormat("ClipboardTrigger '{0}' skipped: clipboard formats [{1}] do not match filter '{2}'.",
                            Name, string.Join(", ", formats), FormatFilter);
                        return;
                    }
                }

                // 4. Construct parameters for pipeline execution
                var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Clipboard.Formats"] = formats,
                    ["Clipboard.Format"] = formats.FirstOrDefault() ?? "Unknown",
                    ["Clipboard.OwnerHandle"] = ownerHwnd
                };

                Log.InfoFormat("ClipboardTrigger '{0}' fired for target recipe '{1}' (formats: {2})",
                    Name, TargetRecipeId, string.Join(", ", formats));

                OnTriggered(parameters);
            }
            catch (Exception ex)
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
