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
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Helpers;
using Greenshot.Ipc;
using Greenshot.Ipc.BrowserExtension;
using log4net;

[assembly: GreenshotModule(typeof(BrowserExtensionIpcExtension), 10)]

namespace Greenshot.Ipc.BrowserExtension
{
    /// <summary>
    /// The IPC commands of the browser extension (source "native_messaging"): handshake, captures and the active tab
    /// </summary>
    internal sealed class BrowserExtensionIpcExtension : IIpcCommandExtension
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BrowserExtensionIpcExtension));

        public IEnumerable<string> Commands { get; } = new[]
        {
            "HANDSHAKE",
            "IMPORT_CAPTURE",
            "TAB_CHANGED"
        };

        public IReadOnlyDictionary<string, IEnumerable<string>> SourceCommands { get; } = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [IpcSources.NativeMessaging] = new[]
            {
                "HANDSHAKE",
                "IMPORT_CAPTURE",
                "TAB_CHANGED",
                "VERSION",
                "LIST_RECIPES",
                "DESCRIBE_RECIPE",
                "RUN_RECIPE"
            }
        };

        public bool IsAllowedForSource(string command, string source) => true;

        public Task<string> CheckAccessAsync(string command, IpcRequestContext context) => Task.FromResult<string>(null);

        public async Task HandleAsync(string command, IpcRequestContext context, Form mainForm)
        {
            switch (command.ToUpperInvariant())
            {
                case "HANDSHAKE":
                    await HandleHandshakeAsync(context).ConfigureAwait(false);
                    break;

                case "IMPORT_CAPTURE":
                    await HandleImportCaptureAsync(context, mainForm).ConfigureAwait(false);
                    break;

                case "TAB_CHANGED":
                    HandleTabChanged(context);
                    break;

                default:
                    Log.Warn($"Unhandled browser extension command: {command}");
                    break;
            }
        }

        private static async Task HandleHandshakeAsync(IpcRequestContext context)
        {
            string versionStr = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.4.0";

            var response = new
            {
                status = "ready",
                greenshot_running = true,
                greenshot_version = versionStr,
                config = new
                {
                    capture_format = "png",
                    track_tab_urls = true,
                    full_page_delay_ms = 250
                }
            };

            await context.ReplyAsync(response).ConfigureAwait(false);
            Log.Info("Handshake response sent successfully.");
        }

        /// <summary>
        /// Imports a browser capture. The image is validated (PNG/JPEG only, dimension limits) before it is decoded,
        /// then handed to the UI thread. The extension gets an acknowledgement with "reply_to": "IMPORT_CAPTURE":
        /// status "ok" when the capture was accepted, or "error" with the reason.
        /// </summary>
        private static async Task HandleImportCaptureAsync(IpcRequestContext context, Form mainForm)
        {
            if (!ImportCaptureDecoder.TryDecode(context.Envelope.Data?.Payload, out Bitmap importedBmp, out string error))
            {
                Log.Warn($"IMPORT_CAPTURE rejected: {error}.");
                await ReplyImportCaptureAsync(context, false, $"Error: {error}.").ConfigureAwait(false);
                return;
            }

            if (mainForm == null || mainForm.IsDisposed)
            {
                importedBmp.Dispose();
                Log.Warn("IMPORT_CAPTURE rejected: Greenshot is not ready to import captures.");
                await ReplyImportCaptureAsync(context, false, "Error: Greenshot is not ready to import captures.").ConfigureAwait(false);
                return;
            }

            string title = context.Envelope.Metadata?.Title ?? "Browser Capture";
            string url = context.Envelope.Metadata?.Url ?? string.Empty;
            string browser = context.Envelope.Browser;
            int width = importedBmp.Width;
            int height = importedBmp.Height;

            if (!string.IsNullOrEmpty(url))
            {
                BrowserContextTracker.Instance.UpdateContext(url, title);
            }

            try
            {
                IpcSecurityDispatcher.RunOnUi(mainForm, new Action(() =>
                {
                    try
                    {
                        var details = new CaptureDetails
                        {
                            Title = title,
                            CaptureMode = CaptureMode.Import
                        };
                        if (!string.IsNullOrEmpty(url))
                        {
                            details.AddMetaData("url", url);
                        }
                        if (!string.IsNullOrEmpty(browser))
                        {
                            details.AddMetaData("browser", browser);
                        }

                        var capture = new Capture
                        {
                            Image = importedBmp,
                            CaptureDetails = details
                        };
                        CaptureHelper.ImportExtensionCapture(capture, browser);
                        Log.Info($"Browser capture successfully imported into pipeline. Title='{title}' Browser='{browser}'");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error forwarding imported capture to CaptureHelper", ex);
                    }
                }));
            }
            catch (InvalidOperationException ex)
            {
                // The window handle is gone (Greenshot is shutting down)
                importedBmp.Dispose();
                Log.Warn("IMPORT_CAPTURE could not be handed to the UI thread", ex);
                await ReplyImportCaptureAsync(context, false, "Error: Greenshot is not ready to import captures.").ConfigureAwait(false);
                return;
            }

            Log.Debug($"IMPORT_CAPTURE accepted ({width}x{height}).");
            await ReplyImportCaptureAsync(context, true, null, width, height).ConfigureAwait(false);
        }

        private static async Task ReplyImportCaptureAsync(IpcRequestContext context, bool success, string stderr, int width = 0, int height = 0)
        {
            try
            {
                if (success)
                {
                    await context.ReplyAsync(new { status = "ok", reply_to = "IMPORT_CAPTURE", exit_code = 0, width, height }).ConfigureAwait(false);
                }
                else
                {
                    await context.ReplyAsync(new { status = "error", reply_to = "IMPORT_CAPTURE", exit_code = 1, stderr }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Could not send the IMPORT_CAPTURE acknowledgement", ex);
            }
        }

        private static void HandleTabChanged(IpcRequestContext context)
        {
            string url = context.Envelope.Url ?? string.Empty;
            string title = context.Envelope.Title ?? string.Empty;

            BrowserContextTracker.Instance.UpdateContext(url, title);
        }
    }
}
