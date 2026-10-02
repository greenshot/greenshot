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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Expressions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Forms;
using Greenshot.Forms.Wpf;
using Greenshot.Pipeline;
using Greenshot.Recipes;
using Greenshot.Triggers;
using Greenshot.UI.SelfService;
using log4net;
using Greenshot.Base.Threading;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Strict whitelist-focused security dispatcher for incoming IPC messages.
    /// Rejects any unrecognized commands, enforces path canonicalization, image extension whitelisting,
    /// and in-memory image surface validation to prevent arbitrary execution or path-traversal attacks.
    /// </summary>
    public static class IpcSecurityDispatcher
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IpcSecurityDispatcher));

        private static readonly HashSet<string> AllowedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HANDSHAKE",
            "CLI",
            "IMPORT_CAPTURE",
            "TAB_CHANGED",
            "OPEN_FILE",
            "EXIT",
            "RELOAD_CONFIG",
            "FIRST_LAUNCH",
            "LIST_RECIPES",
            "RUN_RECIPE",
            "DESCRIBE_RECIPE",
            "VERSION",
            "URL_SCHEME",
            "SETTINGS",
            "ABOUT",
            "SELF_SERVICE",
            "RECIPE_EDITOR",
            "RECIPE_MANAGER",
            "LIST_WINDOWS",
            "LIST_AI_TOOLS",
            "RUN_AI_TOOL",
            "RECIPE_CATALOG",
            "VALIDATE_RECIPE",
            "PROPOSE_RECIPE"
        };

        /// <summary>
        /// The commands of AI tools (greenshot-mcp.exe): only allowed for the "mcp" source. Except LIST_AI_TOOLS they need the user's
        /// consent for the AI tool (see <see cref="AiToolAccess"/>).
        /// </summary>
        private static readonly HashSet<string> AiToolCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LIST_WINDOWS",
            "LIST_AI_TOOLS",
            "RUN_AI_TOOL",
            "RECIPE_CATALOG",
            "VALIDATE_RECIPE",
            "PROPOSE_RECIPE"
        };

        private static readonly HashSet<string> FallbackAllowedImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".tif", ".ico", ".greenshot", ".webp"
        };

        /// <summary>
        /// Retrieves allowed image extensions dynamically from registered IFileFormatHandler instances.
        /// Falls back to default well-known image extensions if format handlers are not yet initialized.
        /// </summary>
        public static IEnumerable<string> GetAllowedImageExtensions()
        {
            var handlers = SimpleServiceProvider.Current?.GetAllInstances<IFileFormatHandler>();
            if (handlers != null)
            {
                var registered = handlers.ExtensionsFor(FileFormatHandlerActions.LoadFromFile).ToList();
                if (registered.Count > 0)
                {
                    return registered;
                }
            }
            return FallbackAllowedImageExtensions;
        }

        /// <summary>
        /// Validates whether a file extension is supported for loading by registered IFileFormatHandler instances.
        /// </summary>
        public static bool IsAllowedImageExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return false;
            if (!extension.StartsWith(".")) extension = "." + extension;

            var handlers = SimpleServiceProvider.Current?.GetAllInstances<IFileFormatHandler>();
            if (handlers != null)
            {
                var supported = handlers.ExtensionsFor(FileFormatHandlerActions.LoadFromFile);
                if (supported != null && supported.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return FallbackAllowedImageExtensions.Contains(extension);
        }

        private static readonly HashSet<string> ReservedDeviceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            "CONIN$", "CONOUT$"
        };

        /// <summary>
        /// Validates, sanitizes, and canonicalizes a path supplied via IPC.
        /// Resolves non-rooted paths against envelope CWD.
        /// Guards against:
        /// - Control characters & null byte injection
        /// - Invisible Unicode formatting and Bidi override characters (e.g. U+202E spoofing)
        /// - Alternate Data Streams (colon outside drive specifier)
        /// - DOS reserved device names (CON, AUX, etc.)
        /// - Path traversal and invalid syntax
        /// - Unauthorized UNC network paths from untrusted callers (e.g. URL scheme, native messaging)
        /// </summary>
        public static bool TrySanitizeAndResolvePath(string rawPath, string cwd, string source, out string fullPath, out string errorMessage)
        {
            fullPath = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(rawPath))
            {
                errorMessage = "Path is empty or whitespace.";
                return false;
            }

            // 1. Check for null bytes, control characters, and Unicode Bidi/invisible spoofing characters
            for (int i = 0; i < rawPath.Length; i++)
            {
                char c = rawPath[i];
                if (c < 0x20)
                {
                    errorMessage = "Path contains illegal control characters.";
                    return false;
                }

                // Invisible / Bidi override characters:
                // U+200B..U+200F (zero-width spaces/marks), U+202A..U+202E (bidi embedding & overrides),
                // U+2066..U+2069 (bidi isolates), U+FEFF (BOM / zero-width non-breaking space)
                if ((c >= 0x200B && c <= 0x200F) ||
                    (c >= 0x202A && c <= 0x202E) ||
                    (c >= 0x2066 && c <= 0x2069) ||
                    c == 0xFEFF)
                {
                    errorMessage = "Path contains forbidden Unicode formatting or bidirectional override characters.";
                    return false;
                }
            }

            // 2. Alternate Data Streams (ADS) check:
            // A colon is ONLY valid as the drive letter delimiter at index 1 (e.g., C:\).
            int firstColon = rawPath.IndexOf(':');
            if (firstColon != -1)
            {
                if (firstColon != 1 || rawPath.IndexOf(':', firstColon + 1) != -1)
                {
                    errorMessage = "Path contains invalid colon or Alternate Data Stream specification.";
                    return false;
                }
                char driveLetter = rawPath[0];
                if (!((driveLetter >= 'a' && driveLetter <= 'z') || (driveLetter >= 'A' && driveLetter <= 'Z')))
                {
                    errorMessage = "Path contains invalid drive specification.";
                    return false;
                }
            }

            // 3. Combine with CWD if not rooted, and canonicalize
            string combined;
            try
            {
                if (!string.IsNullOrWhiteSpace(cwd) && !Path.IsPathRooted(rawPath))
                {
                    combined = Path.Combine(cwd, rawPath);
                }
                else
                {
                    combined = rawPath;
                }

                fullPath = Path.GetFullPath(combined);
            }
            catch (Exception ex)
            {
                errorMessage = $"Path resolution error: {ex.Message}";
                return false;
            }

            // 4. UNC / Network share validation:
            // Disallow UNC paths from untrusted sources (url_scheme, native_messaging, mcp) to prevent NTLM credential relay
            bool isUnc = fullPath.StartsWith(@"\\") || fullPath.StartsWith("//");
            if (isUnc && (string.Equals(source, "url_scheme", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(source, "native_messaging", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(source, IpcSources.Mcp, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "Network (UNC) paths are not permitted from this source.";
                return false;
            }

            // 5. Check for reserved DOS device names in any path segment
            string[] segments = fullPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(segment);
                if (ReservedDeviceNames.Contains(nameWithoutExt))
                {
                    errorMessage = $"Path refers to a reserved device name: '{nameWithoutExt}'.";
                    return false;
                }
            }

            return true;
        }

        private static bool IsUntrustedSource(string source)
        {
            return string.Equals(source, "url_scheme", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sources whose input originates from a browser (web page via custom URL scheme, or browser extension via native messaging).
        /// </summary>
        private static bool IsBrowserSource(string source)
        {
            return string.Equals(source, "url_scheme", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(source, "native_messaging", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Per-source command whitelists. Commands reachable from a browser (web page or extension) are deliberately limited:
        /// no EXIT / RELOAD / FIRST_LAUNCH, and no arbitrary file opening from a web page.
        /// Sources not listed here (e.g. "cli", or a local client without a source) use the global AllowedCommands list.
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> SourceAllowedCommands = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["url_scheme"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CLI",
                "URL_SCHEME",
                "RUN_RECIPE",
                "SETTINGS",
                "ABOUT",
                "SELF_SERVICE",
                "RECIPE_EDITOR",
                "RECIPE_MANAGER"
            },
            ["native_messaging"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "HANDSHAKE",
                "IMPORT_CAPTURE",
                "TAB_CHANGED",
                "VERSION",
                "LIST_RECIPES",
                "DESCRIBE_RECIPE",
                "RUN_RECIPE"
            },
            ["open_with"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CLI",
                "OPEN_FILE"
            },
            // greenshot-mcp.exe: what an AI tool may do, after the user allowed it. Everything it captures goes through a recipe
            // with an AI tool trigger (RUN_AI_TOOL), not through the command line recipes (RUN_RECIPE).
            [IpcSources.Mcp] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "VERSION",
                "LIST_WINDOWS",
                "LIST_AI_TOOLS",
                "RUN_AI_TOOL",
                "RECIPE_CATALOG",
                "VALIDATE_RECIPE",
                "PROPOSE_RECIPE"
            }
        };

        /// <summary>
        /// Checks the command against the global whitelist and, when the source has one, the source-specific whitelist.
        /// </summary>
        internal static bool IsCommandAllowedForSource(string command, string source)
        {
            if (string.IsNullOrEmpty(command) || !AllowedCommands.Contains(command))
            {
                return false;
            }
            // AI tool commands only for greenshot-mcp.exe: not for the command line, a web page or the browser extension
            if (AiToolCommands.Contains(command) && !string.Equals(source, IpcSources.Mcp, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(source) && SourceAllowedCommands.TryGetValue(source, out var sourceCommands))
            {
                return sourceCommands.Contains(command);
            }
            return true;
        }

        /// <summary>
        /// True when the command needs the user's consent for AI tools: every command from greenshot-mcp.exe except VERSION and
        /// LIST_AI_TOOLS (the tool names and descriptions, so the AI tool can show its tools before the user is asked),
        /// and the AI tool commands from any source. The recipe commands (catalog, validate, propose) need the consent too; a
        /// proposed recipe additionally needs the user's approval in the recipe approval window.
        /// </summary>
        internal static bool RequiresAiToolConsent(string command, string source)
        {
            if (string.Equals(command, "VERSION", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "LIST_AI_TOOLS", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return AiToolCommands.Contains(command) || string.Equals(source, IpcSources.Mcp, StringComparison.OrdinalIgnoreCase);
        }

        public static async Task DispatchAsync(IpcRequestContext context, Form mainForm, Action onExit, Action onReloadConfig, Action onFirstLaunch, Action<string> onOpenFile)
        {
            if (context?.Envelope == null)
            {
                Log.Warn("IPC message dropped: Empty or unparseable envelope.");
                return;
            }

            string command = context.Envelope.Command;
            // A greenshot: URL (CliCommandParser creates URL_SCHEME for it) names the command it runs
            if (string.Equals(command, "URL_SCHEME", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(context.Envelope.RawInput))
            {
                command = ParseUrlSchemeCommand(context.Envelope.RawInput, out var urlParams);
                if (context.Envelope.Parsed == null)
                {
                    context.Envelope.Parsed = new IpcParsedCommand();
                }
                if (urlParams != null)
                {
                    foreach (var kvp in urlParams)
                    {
                        context.Envelope.Parsed.Parameters[kvp.Key] = kvp.Value;
                        context.Envelope.Parameters[kvp.Key] = kvp.Value;
                    }
                }
            }

            if (string.IsNullOrEmpty(command))
            {
                Log.Warn("IPC message dropped: Missing command.");
                try
                {
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = "Missing command."
                    }).ConfigureAwait(false);
                }
                catch { }
                return;
            }

            // 1. Strict Whitelist Enforcement (global list plus per-source list)
            if (!IsCommandAllowedForSource(command, context.Envelope.Source))
            {
                Log.Warn($"[SECURITY] IPC command rejected: '{command}' is not in the allowed command whitelist for source '{context.Envelope.Source}'.");
                try
                {
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = $"[SECURITY] IPC command rejected: '{command}' is not in the allowed command whitelist for source '{context.Envelope.Source}'."
                    }).ConfigureAwait(false);
                }
                catch { }
                return;
            }

            // 2. AI tools (and anything reading the screen contents) need the user's consent
            if (RequiresAiToolConsent(command, context.Envelope.Source) &&
                !await AiToolAccess.EnsureAllowedAsync(context.AiClient).ConfigureAwait(false))
            {
                Log.Warn($"[SECURITY] IPC command rejected: '{command}' from source '{context.Envelope.Source}', the user did not allow {context.AiClient?.ToString() ?? "an unidentified program"}.");
                try
                {
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = AiToolAccess.NotAllowedMessage
                    }).ConfigureAwait(false);
                }
                catch { }
                return;
            }

            Log.Info($"Processing whitelisted IPC command: '{command}' from source '{context.Envelope.Source}'");

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

                case "CLI":
                    await HandleCliAsync(context, mainForm, onExit, onReloadConfig, onFirstLaunch, onOpenFile).ConfigureAwait(false);
                    break;
                case "LIST_RECIPES":
                    await HandleListRecipesAsync(context).ConfigureAwait(false);
                    break;

                case "RUN_RECIPE":
                    await HandleRunRecipeAsync(context).ConfigureAwait(false);
                    break;

                case "DESCRIBE_RECIPE":
                    await HandleDescribeRecipeAsync(context).ConfigureAwait(false);
                    break;

                case "OPEN_FILE":
                    await HandleOpenFileAsync(context, mainForm, onOpenFile).ConfigureAwait(false);
                    break;

                case "VERSION":
                    await HandleVersionAsync(context).ConfigureAwait(false);
                    break;

                case "SETTINGS":
                    await HandleSettingsAsync(context, mainForm).ConfigureAwait(false);
                    break;

                case "ABOUT":
                    await HandleAboutAsync(context, mainForm).ConfigureAwait(false);
                    break;

                case "SELF_SERVICE":
                    await HandleSelfServiceAsync(context, mainForm).ConfigureAwait(false);
                    break;

                case "RECIPE_EDITOR":
                    await HandleRecipeEditorAsync(context, mainForm).ConfigureAwait(false);
                    break;

                case "RECIPE_MANAGER":
                    await HandleRecipeManagerAsync(context, mainForm).ConfigureAwait(false);
                    break;

                case "LIST_WINDOWS":
                    await AiToolIpcHandler.HandleListWindowsAsync(context).ConfigureAwait(false);
                    break;

                case "LIST_AI_TOOLS":
                    await AiToolIpcHandler.HandleListAiToolsAsync(context).ConfigureAwait(false);
                    break;

                case "RUN_AI_TOOL":
                    await AiToolIpcHandler.HandleRunAiToolAsync(context).ConfigureAwait(false);
                    break;

                case "RECIPE_CATALOG":
                    await AiRecipeIpcHandler.HandleRecipeCatalogAsync(context).ConfigureAwait(false);
                    break;

                case "VALIDATE_RECIPE":
                    await AiRecipeIpcHandler.HandleValidateRecipeAsync(context).ConfigureAwait(false);
                    break;

                case "PROPOSE_RECIPE":
                    await AiRecipeIpcHandler.HandleProposeRecipeAsync(context).ConfigureAwait(false);
                    break;

                case "EXIT":
                    try
                    {
                        await context.ReplyAsync(new { status = "ok", exit_code = 0, stdout = "Greenshot exiting." }).ConfigureAwait(false);
                    }
                    catch { }
                    InvokeOnUi(mainForm, () => onExit?.Invoke());
                    break;

                case "RELOAD_CONFIG":
                    InvokeOnUi(mainForm, () => onReloadConfig?.Invoke());
                    try
                    {
                        await context.ReplyAsync(new { status = "ok", exit_code = 0, stdout = "Greenshot configuration reloaded." }).ConfigureAwait(false);
                    }
                    catch { }
                    break;

                case "FIRST_LAUNCH":
                    InvokeOnUi(mainForm, () => onFirstLaunch?.Invoke());
                    try
                    {
                        await context.ReplyAsync(new { status = "ok", exit_code = 0, stdout = "First launch completed." }).ConfigureAwait(false);
                    }
                    catch { }
                    break;

                default:
                    Log.Warn($"Unhandled whitelisted command: {command}");
                    break;
            }
        }

        private static void InvokeOnUi(Form form, Action action)
        {
            if (action == null)
            {
                return;
            }

            // The requests arrive on the pipe server threads
            UiDispatcher.Current.RunOnUiAsync(action).FireAndLog("IPC action", Log);
        }

        /// <summary>
        /// Run the action on the UI thread, later (the reply doesn't wait for it). Nothing happens without a main form (tests, headless).
        /// </summary>
        private static void RunOnUi(Form mainForm, Action action)
        {
            if (mainForm == null)
            {
                return;
            }

            UiDispatcher.Current.InvokeAsync(action).FireAndLog("IPC UI action", Log);
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
                RunOnUi(mainForm, new Action(() =>
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

        private static async Task HandleVersionAsync(IpcRequestContext context)
        {
            string versionStr = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.4.0";
            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                version = versionStr,
                stdout = $"Greenshot version {versionStr}"
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// True when the reply is printed by greenshot.com as text (and not requested as --json).
        /// </summary>
        private static bool WantsTextOutput(IpcRequestContext context)
        {
            return context.UsesTextFrames && !context.Envelope.Json;
        }

        /// <summary>
        /// CLI: a raw command line forwarded by greenshot.com / greenshot-proxy.exe. It is parsed according to the connection
        /// source and the resulting command is dispatched like any other request (including the per-source whitelist).
        /// </summary>
        private static async Task HandleCliAsync(IpcRequestContext context, Form mainForm, Action onExit, Action onReloadConfig, Action onFirstLaunch, Action<string> onOpenFile)
        {
            var result = CliCommandParser.Parse(context.Envelope.Argv, context.Envelope.Source, context.Envelope.Cwd);
            if (!result.Success)
            {
                Log.Info($"CLI command line rejected: {result.Error}");
                context.Envelope.Json = result.Json;
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = CliCommandParser.UsageExitCode,
                    stderr = result.Error
                }).ConfigureAwait(false);
                return;
            }

            result.Envelope.Source = context.Envelope.Source;
            await DispatchAsync(context.WithEnvelope(result.Envelope), mainForm, onExit, onReloadConfig, onFirstLaunch, onOpenFile).ConfigureAwait(false);
        }

        private static async Task HandleListRecipesAsync(IpcRequestContext context)
        {
            var recipeManager = SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true) ?? RecipeManager.Instance;
            var list = new List<object>();

            if (recipeManager != null)
            {
                foreach (var recipe in recipeManager.GetAllRecipes().Where(r => r.IsEnabled))
                {
                    if (recipe.Triggers == null) continue;
                    foreach (var tc in recipe.Triggers.Where(t => t.IsActive && string.Equals(t.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase)))
                    {
                        string cmd = tc.GetParameter<string>("Command") ?? recipe.Id;
                        string desc = tc.GetParameter<string>("Description") ?? recipe.Description ?? string.Empty;
                        bool fnf = tc.GetParameter<bool>("FireAndForget", false);
                        string stdoutExpr = tc.GetParameter<string>("Stdout");
                        var args = tc.GetParameter<List<CommandlineArgument>>("Arguments") ?? new List<CommandlineArgument>();

                        list.Add(new
                        {
                            id = recipe.Id,
                            name = recipe.Name,
                            command = cmd,
                            description = desc,
                            fire_and_forget = fnf,
                            stdout = stdoutExpr,
                            arguments = args.Select(a => new
                            {
                                name = a.Name,
                                variable = string.IsNullOrWhiteSpace(a.Variable) ? a.Name : a.Variable,
                                description = a.Description ?? string.Empty,
                                required = a.Required,
                                default_value = a.DefaultValue,
                                type = a.Type.ToString(),
                                allowed_values = a.AllowedValues
                            })
                        });
                    }
                }
            }

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                recipes = list,
                // Console output for greenshot.com; JSON clients use "recipes"
                stdout = WantsTextOutput(context) ? CliTextRenderer.RenderRecipeList(Newtonsoft.Json.Linq.JToken.FromObject(list)) : null
            }).ConfigureAwait(false);
        }

        private static async Task HandleDescribeRecipeAsync(IpcRequestContext context)
        {
            string target = context.Envelope.Recipe;
            if (string.IsNullOrEmpty(target) && context.Envelope.Parsed?.Parameters != null)
            {
                context.Envelope.Parsed.Parameters.TryGetValue("recipe", out target);
            }
            if (string.IsNullOrEmpty(target) && context.Envelope.Parameters != null)
            {
                if (context.Envelope.Parameters.TryGetValue("recipe", out var rVal) && rVal != null)
                {
                    target = rVal.ToString();
                }
            }
            if (string.IsNullOrEmpty(target))
            {
                target = context.Envelope.RawInput;
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = "Missing required argument: recipe name or ID to describe."
                }).ConfigureAwait(false);
                return;
            }

            target = target.Trim();

            var recipeManager = SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true) ?? RecipeManager.Instance;
            CaptureRecipe matchedRecipe = null;
            if (recipeManager != null)
            {
                foreach (var r in recipeManager.GetAllRecipes())
                {
                    if (string.Equals(r.Id, target, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.Name, target, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedRecipe = r;
                        break;
                    }
                    if (r.Triggers != null)
                    {
                        foreach (var tc in r.Triggers)
                        {
                            string cmd = tc.GetParameter<string>("Command");
                            if (string.Equals(cmd, target, StringComparison.OrdinalIgnoreCase))
                            {
                                matchedRecipe = r;
                                break;
                            }
                        }
                    }
                    if (matchedRecipe != null) break;
                }
            }

            if (matchedRecipe == null)
            {
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = $"Recipe '{target}' not found."
                }).ConfigureAwait(false);
                return;
            }

            var contract = RecipeContract.Analyze(matchedRecipe);

            var recipeInfo = new
            {
                id = matchedRecipe.Id,
                name = matchedRecipe.Name,
                description = matchedRecipe.Description ?? string.Empty,
                category = "General",
                triggers = matchedRecipe.Triggers?.Select(t => new
                {
                    type = t.TriggerType,
                    command = t.GetParameter<string>("Command"),
                    description = t.GetParameter<string>("Description"),
                    stdout = t.GetParameter<string>("Stdout"),
                    arguments = t.GetParameter<List<CommandlineArgument>>("Arguments")
                }),
                contract = new
                {
                    inputs = contract.Inputs.Select(i => new
                    {
                        name = i.Name,
                        type = i.DataType.ToString(),
                        required = i.Required,
                        description = i.Description,
                        default_value = i.ExampleValue
                    }),
                    outputs = contract.Outputs.Select(o => new
                    {
                        name = o.Name,
                        type = o.DataType.ToString(),
                        description = o.Description,
                        conditional = o.Conditional
                    }),
                    steps = contract.Steps.Select(s => new
                    {
                        node_id = s.NodeId,
                        step_type = s.StepType,
                        display_name = s.DisplayName,
                        required_inputs = s.RequiredInputs,
                        produced_outputs = s.ProducedOutputs,
                        requires_image = s.RequiresRawCapture == PayloadRequirement.Required,
                        reachable = s.Reachable
                    }),
                    lifecycle = new
                    {
                        acquires_image = contract.AcquiresImage,
                        mutates_pixels = contract.MutatesPixels,
                        extracts_text = contract.ExtractsText
                    },
                    warnings = contract.ValidationWarnings
                }
            };

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                recipe = recipeInfo,
                // Console output for greenshot.com; JSON clients use "recipe"
                stdout = WantsTextOutput(context) ? CliTextRenderer.RenderRecipeDescription(Newtonsoft.Json.Linq.JToken.FromObject(recipeInfo)) : null
            }).ConfigureAwait(false);
        }

        private static async Task HandleRunRecipeAsync(IpcRequestContext context)
        {
            string target = context.Envelope.Recipe;
            if (string.IsNullOrEmpty(target) && context.Envelope.Parsed?.Parameters != null)
            {
                context.Envelope.Parsed.Parameters.TryGetValue("recipe", out target);
            }
            if (string.IsNullOrEmpty(target))
            {
                target = context.Envelope.RawInput;
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = "Missing required argument: recipe command or recipe ID."
                }).ConfigureAwait(false);
                return;
            }

            target = target.Trim();

            var recipeManager = SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true) ?? RecipeManager.Instance;
            if (recipeManager == null)
            {
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = "Recipe manager not available."
                }).ConfigureAwait(false);
                return;
            }

            CaptureRecipe matchedRecipe = null;
            TriggerConfig matchedTriggerConfig = null;

            foreach (var recipe in recipeManager.GetAllRecipes().Where(r => r.IsEnabled))
            {
                if (recipe.Triggers == null) continue;
                foreach (var tc in recipe.Triggers.Where(t => t.IsActive && string.Equals(t.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase)))
                {
                    string cmd = tc.GetParameter<string>("Command");
                    if (string.Equals(cmd, target, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(recipe.Id, target, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(recipe.Name, target, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedRecipe = recipe;
                        matchedTriggerConfig = tc;
                        break;
                    }
                }
                if (matchedRecipe != null) break;
            }

            if (matchedRecipe == null)
            {
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = $"Recipe '{target}' not found or does not have an active CommandlineTrigger."
                }).ConfigureAwait(false);
                return;
            }

            // Recipes can only be started from a browser (web page URL or extension) when the trigger explicitly opts in.
            if (IsBrowserSource(context.Envelope.Source) && !(matchedTriggerConfig.GetParameter<bool>("AllowBrowserInvocation", false) && matchedTriggerConfig.IsBrowserInvocationApproved))
            {
                Log.Warn($"[SECURITY] RUN_RECIPE rejected: recipe '{matchedRecipe.Id}' does not allow invocation from source '{context.Envelope.Source}'.");
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = $"[SECURITY] Recipe '{target}' does not allow invocation from a browser. Set 'AllowBrowserInvocation' on its Commandline trigger to enable this."
                }).ConfigureAwait(false);
                return;
            }

            var pipeline = SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true) ?? CapturePipeline.Instance;
            if (pipeline == null)
            {
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = "Capture pipeline not available."
                }).ConfigureAwait(false);
                return;
            }

            bool fireAndForget = context.Envelope.Async || matchedTriggerConfig.GetParameter<bool>("FireAndForget", false);
            string cmdName = matchedTriggerConfig.GetParameter<string>("Command") ?? matchedRecipe.Id;
            string desc = matchedTriggerConfig.GetParameter<string>("Description") ?? matchedRecipe.Description;
            string stdoutExpr = matchedTriggerConfig.GetParameter<string>("Stdout");
            var declaredArgs = matchedTriggerConfig.GetParameter<List<CommandlineArgument>>("Arguments") ?? new List<CommandlineArgument>();

            var cmdTrigger = new CommandlineTrigger(
                $"cli_{matchedRecipe.Id}_{Guid.NewGuid():N}",
                cmdName,
                matchedRecipe.Id,
                cmdName,
                desc,
                fireAndForget,
                stdoutExpr,
                declaredArgs);

            var recipeToExecute = TriggerRecipePreparer.Prepare(matchedRecipe, cmdTrigger);

            // Arguments from the caller: CLI "key=value" arguments, or the query string of a greenshot: URL.
            // "recipe" is the target itself (from greenshot://recipe/<id>), not an argument.
            var suppliedArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (context.Envelope.Parameters != null)
            {
                foreach (var kvp in context.Envelope.Parameters)
                {
                    suppliedArguments[kvp.Key] = kvp.Value;
                }
            }
            if (context.Envelope.Parsed?.Parameters != null)
            {
                foreach (var kvp in context.Envelope.Parsed.Parameters)
                {
                    suppliedArguments[kvp.Key] = kvp.Value;
                }
            }
            suppliedArguments.Remove("recipe");

            // Only declared arguments are accepted, converted according to their declared type (see CommandlineArgumentBinder)
            var binding = CommandlineArgumentBinder.Bind(declaredArgs, suppliedArguments, cmdName, context.Envelope.Cwd, context.Envelope.Source);
            if (!binding.Success)
            {
                Log.Warn($"RUN_RECIPE '{matchedRecipe.Id}' rejected: {binding.Error}");
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = CliCommandParser.UsageExitCode,
                    stderr = binding.Error
                }).ConfigureAwait(false);
                return;
            }

            // --query and --json are protocol options (top-level envelope fields), never recipe arguments
            string queryExpr = string.IsNullOrWhiteSpace(context.Envelope.Query) ? null : context.Envelope.Query;
            bool outputJson = context.Envelope.Json;

            // --query evaluates arbitrary expressions (including environment and configuration values): command line only
            if (queryExpr != null && !string.Equals(context.Envelope.Source, IpcSources.Cli, StringComparison.OrdinalIgnoreCase))
            {
                Log.Warn($"[SECURITY] RUN_RECIPE rejected: --query is not allowed from source '{context.Envelope.Source}'.");
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = "[SECURITY] Queries are only allowed from the command line."
                }).ConfigureAwait(false);
                return;
            }

            // Plain runs stream Stdout / Stderr step output immediately. With --query only the queried value is printed,
            // and with --json everything is collected into the single JSON result, so stdout stays one parseable document.
            bool streamStdout = queryExpr == null && !outputJson;
            bool streamStderr = !outputJson;
            var collectedStdout = new List<string>();
            var collectedStderr = new List<string>();
            bool hasStreamedStdout = false;
            bool hasStreamedStderr = false;

            Action<CaptureFlowContext> configureContext = flowCtx =>
            {
                foreach (var kv in binding.Variables)
                {
                    flowCtx.Properties[kv.Key] = kv.Value;
                }

                // In fire-and-forget mode the final reply is sent immediately and the client disconnects,
                // so no streaming writers may stay bound to this IPC connection.
                if (fireAndForget)
                {
                    return;
                }

                flowCtx.StdoutWriter = async text =>
                {
                    lock (collectedStdout)
                    {
                        collectedStdout.Add(text);
                    }
                    if (streamStdout)
                    {
                        hasStreamedStdout = true;
                        await context.ReplyAsync(new
                        {
                            stream = "stdout",
                            text = text
                        }).ConfigureAwait(false);
                    }
                };

                flowCtx.StderrWriter = async text =>
                {
                    lock (collectedStderr)
                    {
                        collectedStderr.Add(text);
                    }
                    if (streamStderr)
                    {
                        hasStreamedStderr = true;
                        await context.ReplyAsync(new
                        {
                            stream = "stderr",
                            text = text
                        }).ConfigureAwait(false);
                    }
                };
            };

            var runner = CaptureFlowRunner.For(pipeline);
            if (fireAndForget)
            {
                // Tracked by the flow runner, the caller doesn't wait
                runner.Start(recipeToExecute, FlowTriggerContext.Empty(cmdTrigger), configureContext);
                await context.ReplyAsync(new
                {
                    status = "ok",
                    exit_code = 0,
                    recipe = matchedRecipe.Id,
                    stdout = $"Recipe '{matchedRecipe.Name}' triggered in background."
                }).ConfigureAwait(false);
                return;
            }

            var flowResult = await runner.Start(recipeToExecute, FlowTriggerContext.Empty(cmdTrigger), configureContext).Completion.ConfigureAwait(false);
            var flowContext = flowResult.Context ?? CreateContextForResult(recipeToExecute, cmdTrigger, flowResult);
            bool failed = flowContext.IsAborted || flowContext.State == CaptureFlowState.Failed;
            // Honors a custom exit code set by a StderrStep (also when it did not abort)
            int finalExitCode = flowContext.ExitCode != 0 ? flowContext.ExitCode : (failed ? 1 : 0);

            string stdoutText = null;
            string stderrText = null;
            if (failed)
            {
                string err = flowContext.AbortReason ?? flowContext.Error?.Message ?? "Recipe execution failed or was aborted.";
                // A StderrStep already streamed its message; repeating the abort reason would print it twice.
                if (!hasStreamedStderr)
                {
                    stderrText = collectedStderr.Count > 0 ? JoinOutput(collectedStderr) : err;
                }
                if (outputJson && collectedStdout.Count > 0)
                {
                    stdoutText = JoinOutput(collectedStdout);
                }
            }
            else
            {
                if (queryExpr != null)
                {
                    // Priority 1: --query replaces the recipe's own output
                    stdoutText = ExpressionEvaluator.Instance.Evaluate(queryExpr, flowContext)?.ToString();
                }
                else if (hasStreamedStdout)
                {
                    // Priority 2: StdoutStep(s) already streamed the output; don't emit it a second time
                    stdoutText = null;
                }
                else if (collectedStdout.Count > 0)
                {
                    // Priority 3: --json collected the StdoutStep output instead of streaming it
                    stdoutText = JoinOutput(collectedStdout);
                }
                else if (!string.IsNullOrWhiteSpace(stdoutExpr))
                {
                    // Priority 4: Explicit stdout template defined on the trigger
                    stdoutText = ExpressionEvaluator.Instance.Evaluate(stdoutExpr, flowContext)?.ToString();
                }
                else
                {
                    // Priority 5: Fallback if neither trigger stdout nor StdoutStep was used
                    stdoutText = await GetFallbackOutputAsync(flowContext, matchedRecipe).ConfigureAwait(false);
                }

                if (!hasStreamedStderr && collectedStderr.Count > 0)
                {
                    stderrText = JoinOutput(collectedStderr);
                }
            }

            if (outputJson)
            {
                // Only JSON-safe values: the flow context also holds forms, bitmaps, window handles etc.
                await context.ReplyAsync(new
                {
                    status = failed ? "error" : "ok",
                    exit_code = finalExitCode,
                    recipe = matchedRecipe.Id,
                    stdout = stdoutText,
                    stderr = stderrText,
                    payload = CreatePayloadSummary(flowContext.Payload),
                    variables = SnapshotJsonSafe(flowContext.Properties)
                }).ConfigureAwait(false);
            }
            else
            {
                await context.ReplyAsync(new
                {
                    status = failed ? "error" : "ok",
                    exit_code = finalExitCode,
                    stdout = stdoutText,
                    stderr = stderrText
                }).ConfigureAwait(false);
            }
        }

        private static string JoinOutput(List<string> chunks)
        {
            lock (chunks)
            {
                return string.Join("\n", chunks);
            }
        }

        /// <summary>
        /// Output of a recipe that has neither a Stdout step, a trigger Stdout expression nor a --query.
        /// </summary>
        private static async Task<string> GetFallbackOutputAsync(CaptureFlowContext flowCtx, CaptureRecipe matchedRecipe)
        {
            string outputText = null;
            if (flowCtx.Properties.TryGetValue("CommandResult", out var crObj) && crObj != null)
            {
                outputText = crObj.ToString();
            }
            else if (flowCtx.Properties.TryGetValue("OutputText", out var otObj) && otObj != null)
            {
                outputText = otObj.ToString();
            }
            else if (flowCtx.Properties.TryGetValue("LastStdout", out var lsObj) && lsObj != null)
            {
                outputText = lsObj.ToString();
            }
            else if (flowCtx.Properties.TryGetValue("Text", out var tObj) && tObj != null)
            {
                outputText = tObj.ToString();
            }
            else if (flowCtx.Properties.TryGetValue("OcrText", out var ocrObj) && ocrObj != null)
            {
                outputText = ocrObj.ToString();
            }
            else if (flowCtx.Properties.TryGetValue("Barcode.Text", out var bObj) && bObj != null)
            {
                outputText = bObj.ToString();
            }
            else if (flowCtx.Properties.TryGetValue("Zxing.DecodedText", out var zObj) && zObj != null)
            {
                outputText = zObj.ToString();
            }
            else if (!string.IsNullOrWhiteSpace(flowCtx.Payload?.ExtractedText))
            {
                outputText = flowCtx.Payload.ExtractedText;
            }
            else if (flowCtx.Properties.TryGetValue("Destination.Filename", out var dfObj) && dfObj != null)
            {
                outputText = dfObj.ToString();
            }
            else if (flowCtx.Payload?.RawCapture?.CaptureDetails != null)
            {
                var details = flowCtx.Payload.RawCapture.CaptureDetails;
                if (details.ProcessingTask != null)
                {
                    try
                    {
                        await details.ProcessingTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("The processing of the capture didn't finish", ex);
                    }
                }
                lock (details.Features)
                {
                    var ocrLines = details.Features.OfType<IOcrLineFeature>().ToList();
                    if (ocrLines.Any())
                    {
                        outputText = string.Join(Environment.NewLine, ocrLines.Select(l => l.Text));
                    }
                }
            }

            if (string.IsNullOrEmpty(outputText))
            {
                outputText = $"Recipe '{matchedRecipe.Name}' completed successfully.";
            }
            return outputText;
        }

        /// <summary>
        /// A flow which didn't start (rejected) or failed outside the pipeline has no context, create one describing the outcome.
        /// </summary>
        private static CaptureFlowContext CreateContextForResult(CaptureRecipe recipe, ITrigger trigger, CaptureFlowResult result)
        {
            var context = new CaptureFlowContext(recipe, trigger);
            if (result.State == CaptureFlowState.Failed)
            {
                context.Fail(result.Reason ?? "Recipe execution failed.", result.Error);
            }
            else if (result.State == CaptureFlowState.Cancelled)
            {
                context.Abort(result.Reason ?? "Recipe execution was cancelled.");
            }
            return context;
        }

        private static object CreatePayloadSummary(ICapturePayload payload)
        {
            if (payload == null)
            {
                return null;
            }

            var img = payload.Surface?.Image ?? payload.RawCapture?.Image;
            return new
            {
                width = img?.Width ?? 0,
                height = img?.Height ?? 0,
                format = img?.PixelFormat.ToString(),
                extracted_text = payload.ExtractedText,
                metadata = SnapshotJsonSafe(payload.Metadata)
            };
        }

        /// <summary>
        /// Copies the entries whose values can be represented in JSON as-is (strings, numbers, booleans, dates, enums,
        /// string lists). Everything else (forms, bitmaps, window details, settings objects) is left out.
        /// </summary>
        internal static SortedDictionary<string, object> SnapshotJsonSafe(IDictionary<string, object> values)
        {
            var result = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (values == null)
            {
                return result;
            }

            foreach (var kvp in values.ToList())
            {
                if (!string.IsNullOrEmpty(kvp.Key) && TryGetJsonSafeValue(kvp.Value, out var safeValue))
                {
                    result[kvp.Key] = safeValue;
                }
            }
            return result;
        }

        private static bool TryGetJsonSafeValue(object value, out object safeValue)
        {
            safeValue = null;
            switch (value)
            {
                case null:
                    return true;
                case string s:
                    safeValue = s;
                    return true;
                case char c:
                    safeValue = c.ToString();
                    return true;
                case decimal _:
                    safeValue = value;
                    return true;
                case DateTime dateTime:
                    safeValue = dateTime.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                case DateTimeOffset dateTimeOffset:
                    safeValue = dateTimeOffset.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                case TimeSpan timeSpan:
                    safeValue = timeSpan.ToString("c", System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                case Guid guid:
                    safeValue = guid.ToString();
                    return true;
                case Enum enumValue:
                    safeValue = enumValue.ToString();
                    return true;
                case Newtonsoft.Json.Linq.JValue jValue:
                    return TryGetJsonSafeValue(jValue.Value, out safeValue);
                case IEnumerable<string> strings:
                    safeValue = strings.ToList();
                    return true;
            }

            var type = value.GetType();
            if (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr))
            {
                safeValue = value;
                return true;
            }
            return false;
        }

        private static async Task HandleOpenFileAsync(IpcRequestContext context, Form mainForm, Action<string> onOpenFile)
        {
            var rawFiles = new List<string>();
            if (context.Envelope.Files != null && context.Envelope.Files.Count > 0)
            {
                rawFiles.AddRange(context.Envelope.Files);
            }

            if (rawFiles.Count == 0)
            {
                Log.Warn("OPEN_FILE rejected: No file path provided.");
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = "OPEN_FILE rejected: No file path provided."
                }).ConfigureAwait(false);
                return;
            }

            var validFiles = new List<string>();
            foreach (var filePath in rawFiles)
            {
                if (!TrySanitizeAndResolvePath(filePath, context.Envelope.Cwd, context.Envelope.Source, out string fullPath, out string sanitizeErr))
                {
                    Log.Warn($"[SECURITY] OPEN_FILE rejected '{filePath}': {sanitizeErr}");
                    string clientErr = IsUntrustedSource(context.Envelope.Source)
                        ? "[SECURITY] Invalid file path or operation not permitted."
                        : $"[SECURITY] OPEN_FILE rejected: {sanitizeErr}";
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = clientErr
                    }).ConfigureAwait(false);
                    return;
                }

                string ext = Path.GetExtension(fullPath);
                if (string.IsNullOrEmpty(ext) || !IsAllowedImageExtension(ext))
                {
                    Log.Warn($"[SECURITY] OPEN_FILE rejected: File extension '{ext}' is not permitted.");
                    string clientErr = IsUntrustedSource(context.Envelope.Source)
                        ? "[SECURITY] File type not permitted."
                        : $"[SECURITY] OPEN_FILE rejected: File extension '{ext}' is not permitted.";
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = clientErr
                    }).ConfigureAwait(false);
                    return;
                }

                if (!File.Exists(fullPath))
                {
                    Log.Warn($"OPEN_FILE rejected: File does not exist: '{fullPath}'");
                    string clientErr = IsUntrustedSource(context.Envelope.Source)
                        ? "File not found or access denied."
                        : $"OPEN_FILE rejected: File does not exist: '{fullPath}'";
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = clientErr
                    }).ConfigureAwait(false);
                    return;
                }

                validFiles.Add(fullPath);
            }

            var recipeManager = SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true) ?? RecipeManager.Instance;
            var openFileRecipes = new List<(CaptureRecipe Recipe, TriggerConfig Trigger)>();

            if (recipeManager != null)
            {
                foreach (var recipe in recipeManager.GetAllRecipes().Where(r => r.IsEnabled))
                {
                    if (recipe.Triggers == null) continue;
                    foreach (var tc in recipe.Triggers.Where(t => t.IsActive && string.Equals(t.TriggerType, TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase)))
                    {
                        openFileRecipes.Add((recipe, tc));
                    }
                }
            }

            if (openFileRecipes.Count > 0)
            {
                var pipeline = SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true) ?? CapturePipeline.Instance;
                int executedCount = 0;

                foreach (var file in validFiles)
                {
                    string fileExt = Path.GetExtension(file);

                    foreach (var pair in openFileRecipes)
                    {
                        string filter = pair.Trigger.GetParameter<string>("Filter");
                        if (!string.IsNullOrWhiteSpace(filter))
                        {
                            // The filter is a list of extensions separated by ';', e.g. ".png;.jpg"
                            var allowedExts = filter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(e => e.Trim());
                            if (!allowedExts.Any(e => string.Equals(e, fileExt, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                        }

                        bool fnf = pair.Trigger.GetParameter<bool>("FireAndForget", false);
                        var trigger = new OpenFileTrigger(
                            $"openfile_{pair.Recipe.Id}_{Guid.NewGuid():N}",
                            $"{pair.Recipe.Name} OpenFile",
                            pair.Recipe.Id,
                            filter,
                            fnf);

                        var recipeToExecute = TriggerRecipePreparer.Prepare(pair.Recipe, trigger);

                        var handle = CaptureFlowRunner.For(pipeline).Start(recipeToExecute, FlowTriggerContext.Empty(trigger), ctx => ctx.Properties["Filename"] = file);
                        if (!fnf)
                        {
                            await handle.Completion.ConfigureAwait(false);
                        }
                        executedCount++;
                    }
                }

                await context.ReplyAsync(new
                {
                    status = "ok",
                    exit_code = 0,
                    stdout = $"Opened {validFiles.Count} file(s) with {executedCount} recipe execution(s)."
                }).ConfigureAwait(false);
            }
            else
            {
                bool hasDefinedOpenFileTriggers = recipeManager?.GetAllRecipes()
                    .Any(r => r.Triggers != null && r.Triggers.Any(t => string.Equals(t.TriggerType, TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase))) ?? false;

                if (hasDefinedOpenFileTriggers)
                {
                    Log.Info("All OpenFileTrigger recipes are disabled. File opening skipped.");
                    await context.ReplyAsync(new
                    {
                        status = "ok",
                        exit_code = 0,
                        stdout = "File opening skipped: all OpenFileTrigger recipes are disabled."
                    }).ConfigureAwait(false);
                }
                else
                {
                    foreach (var file in validFiles)
                    {
                        InvokeOnUi(mainForm, () => onOpenFile?.Invoke(file));
                    }
                    await context.ReplyAsync(new
                    {
                        status = "ok",
                        exit_code = 0,
                        stdout = $"Opened {validFiles.Count} file(s)."
                    }).ConfigureAwait(false);
                }
            }
        }

        private static async Task HandleSettingsAsync(IpcRequestContext context, Form mainForm)
        {
            string tab = null;
            string plugin = null;
            context.Envelope.Parsed?.Parameters?.TryGetValue("tab", out tab);
            context.Envelope.Parsed?.Parameters?.TryGetValue("plugin", out plugin);
            if (string.IsNullOrEmpty(tab) && context.Envelope.Parameters != null) context.Envelope.Parameters.TryGetValue("tab", out tab);
            if (string.IsNullOrEmpty(plugin) && context.Envelope.Parameters != null) context.Envelope.Parameters.TryGetValue("plugin", out plugin);

            if (mainForm is MainForm mf)
            {
                RunOnUi(mainForm, new Action(() => mf.ShowSetting(plugin, tab)));
            }
            else
            {
                RunOnUi(mainForm, new Action(() =>
                {
                    var window = new SettingsWindow(plugin, tab);
                    window.ShowDialog();
                }));
            }

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                stdout = "Settings window displayed."
            }).ConfigureAwait(false);
        }

        private static async Task HandleAboutAsync(IpcRequestContext context, Form mainForm)
        {
            if (mainForm is MainForm mf)
            {
                RunOnUi(mainForm, new Action(() => mf.ShowAbout()));
            }

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                stdout = "About window displayed."
            }).ConfigureAwait(false);
        }

        private static async Task HandleSelfServiceAsync(IpcRequestContext context, Form mainForm)
        {
            string section = null;
            context.Envelope.Parsed?.Parameters?.TryGetValue("section", out section);
            if (string.IsNullOrEmpty(section) && context.Envelope.Parameters != null) context.Envelope.Parameters.TryGetValue("section", out section);

            RunOnUi(mainForm, new Action(() =>
            {
                SelfServiceWindow.ShowSelfService(initialSection: section);
            }));

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                stdout = "Self-Service window displayed."
            }).ConfigureAwait(false);
        }

        private static async Task HandleRecipeEditorAsync(IpcRequestContext context, Form mainForm)
        {
            string recipe = null;
            context.Envelope.Parsed?.Parameters?.TryGetValue("recipe", out recipe);
            if (string.IsNullOrEmpty(recipe) && context.Envelope.Parameters != null) context.Envelope.Parameters.TryGetValue("recipe", out recipe);

            RunOnUi(mainForm, new Action(() =>
            {
                var editorService = SimpleServiceProvider.Current?.GetInstance<IRecipeEditorService>(isOptional: true);
                editorService?.OpenEditor(recipe);
            }));

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                stdout = "Recipe Editor displayed."
            }).ConfigureAwait(false);
        }

        private static async Task HandleRecipeManagerAsync(IpcRequestContext context, Form mainForm)
        {
            RunOnUi(mainForm, new Action(() =>
            {
                var editorService = SimpleServiceProvider.Current?.GetInstance<IRecipeEditorService>(isOptional: true);
                editorService?.OpenRecipeManager();
            }));

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                stdout = "Recipe Manager displayed."
            }).ConfigureAwait(false);
        }

        private static string ParseUrlSchemeCommand(string rawUrl, out Dictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                return null;
            }

            try
            {
                string text = rawUrl.Trim();
                if (text.StartsWith("greenshot://", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(12);
                }
                else if (text.StartsWith("greenshot:", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(10);
                }

                string pathAndAction = text;
                int qIdx = text.IndexOf('?');
                if (qIdx >= 0)
                {
                    pathAndAction = text.Substring(0, qIdx);
                    string query = text.Substring(qIdx + 1);
                    foreach (string pair in query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] kv = pair.Split(new[] { '=' }, 2);
                        string key = Uri.UnescapeDataString(kv[0]).Trim();
                        string val = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
                        parameters[key] = val;
                    }
                }

                pathAndAction = pathAndAction.Trim('/');

                if (pathAndAction.StartsWith("recipe/", StringComparison.OrdinalIgnoreCase))
                {
                    parameters["recipe"] = pathAndAction.Substring(7);
                    return "RUN_RECIPE";
                }

                if (string.Equals(pathAndAction, "settings", StringComparison.OrdinalIgnoreCase))
                {
                    return "SETTINGS";
                }

                if (string.Equals(pathAndAction, "about", StringComparison.OrdinalIgnoreCase))
                {
                    return "ABOUT";
                }

                if (string.Equals(pathAndAction, "self-service", StringComparison.OrdinalIgnoreCase))
                {
                    return "SELF_SERVICE";
                }

                if (string.Equals(pathAndAction, "recipe-editor", StringComparison.OrdinalIgnoreCase))
                {
                    return "RECIPE_EDITOR";
                }

                if (string.Equals(pathAndAction, "recipe-manager", StringComparison.OrdinalIgnoreCase))
                {
                    return "RECIPE_MANAGER";
                }

                return pathAndAction.ToUpperInvariant();
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to parse custom URL scheme '{rawUrl}'", ex);
            }

            return null;
        }
    }
}
