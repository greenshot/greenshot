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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Forms;
using Greenshot.Forms.Wpf;
using Greenshot.Pipeline;
using Greenshot.Recipes;
using Greenshot.Triggers;
using Greenshot.UI.SelfService;
using log4net;

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
            "IMPORT_CAPTURE",
            "TAB_CHANGED",
            "OPEN_FILE",
            "OPEN",
            "EXIT",
            "RELOAD_CONFIG",
            "RELOAD",
            "FIRST_LAUNCH",
            "LIST_RECIPES",
            "RUN_RECIPE",
            "VERSION",
            "URL_SCHEME",
            "SETTINGS",
            "ABOUT",
            "SELF_SERVICE",
            "SELFSERVICE",
            "RECIPE_EDITOR",
            "RECIPE_MANAGER"
        };

        private static readonly HashSet<string> AllowedImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".tif", ".ico", ".greenshot"
        };

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
            // Disallow UNC paths from untrusted sources (url_scheme, native_messaging) to prevent NTLM credential relay
            bool isUnc = fullPath.StartsWith(@"\\") || fullPath.StartsWith("//");
            if (isUnc && (string.Equals(source, "url_scheme", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(source, "native_messaging", StringComparison.OrdinalIgnoreCase)))
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

        public static async Task DispatchAsync(IpcRequestContext context, Form mainForm, Action onExit, Action onReloadConfig, Action onFirstLaunch, Action<string> onOpenFile)
        {
            if (context?.Envelope == null)
            {
                Log.Warn("IPC message dropped: Empty or unparseable envelope.");
                return;
            }

            // Determine command name from Command property, Parsed Action, or RawInput
            string command = context.Envelope.Command;
            if (string.IsNullOrEmpty(command))
            {
                command = context.Envelope.Parsed?.Action;
            }
            if ((string.IsNullOrEmpty(command) || string.Equals(command, "URL_SCHEME", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(context.Envelope.RawInput))
            {
                if (context.Envelope.RawInput.StartsWith("greenshot:", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(context.Envelope.Source, "url_scheme", StringComparison.OrdinalIgnoreCase))
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
                else if (string.IsNullOrEmpty(command))
                {
                    command = "OPEN_FILE";
                }
            }

            if (string.IsNullOrEmpty(command))
            {
                Log.Warn("IPC message dropped: Missing command/action identifier.");
                try
                {
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = "Missing command or action identifier."
                    }).ConfigureAwait(false);
                }
                catch { }
                return;
            }

            // 1. Strict Whitelist Enforcement
            if (!AllowedCommands.Contains(command))
            {
                Log.Warn($"[SECURITY] IPC command rejected: '{command}' is not in the allowed command whitelist.");
                try
                {
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = $"[SECURITY] IPC command rejected: '{command}' is not in the allowed command whitelist."
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
                    HandleImportCapture(context, mainForm);
                    break;

                case "TAB_CHANGED":
                    HandleTabChanged(context);
                    break;

                case "LIST_RECIPES":
                    await HandleListRecipesAsync(context).ConfigureAwait(false);
                    break;

                case "RUN_RECIPE":
                    await HandleRunRecipeAsync(context).ConfigureAwait(false);
                    break;

                case "OPEN_FILE":
                case "OPEN":
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
                case "SELFSERVICE":
                    await HandleSelfServiceAsync(context, mainForm).ConfigureAwait(false);
                    break;

                case "RECIPE_EDITOR":
                    await HandleRecipeEditorAsync(context, mainForm).ConfigureAwait(false);
                    break;

                case "RECIPE_MANAGER":
                    await HandleRecipeManagerAsync(context, mainForm).ConfigureAwait(false);
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
                case "RELOAD":
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

            if (form != null && form.InvokeRequired)
            {
                form.BeginInvoke(action);
            }
            else
            {
                action();
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

        private static void HandleImportCapture(IpcRequestContext context, Form mainForm)
        {
            string base64Payload = context.Envelope.Data?.Payload;
            if (string.IsNullOrWhiteSpace(base64Payload))
            {
                Log.Warn("IMPORT_CAPTURE rejected: Missing or empty payload.");
                return;
            }

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64Payload);
                if (imageBytes.Length == 0)
                {
                    Log.Warn("IMPORT_CAPTURE rejected: Zero length decoded byte array.");
                    return;
                }

                // Strictly validate in memory by loading into Bitmap
                using (var ms = new MemoryStream(imageBytes))
                using (var sourceBmp = new Bitmap(ms))
                {
                    // Clone bitmap so it remains valid after MemoryStream is disposed
                    var importedBmp = new Bitmap(sourceBmp);

                    string title = context.Envelope.Metadata?.Title ?? context.Envelope.Title ?? "Browser Capture";
                    string url = context.Envelope.Metadata?.Url ?? context.Envelope.Url ?? string.Empty;

                    if (!string.IsNullOrEmpty(url))
                    {
                        BrowserContextTracker.Instance.UpdateContext(url, title);
                    }

                    mainForm?.BeginInvoke(new Action(() =>
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
                            if (!string.IsNullOrEmpty(context.Envelope.Browser))
                            {
                                details.AddMetaData("browser", context.Envelope.Browser);
                            }

                            var capture = new Capture
                            {
                                Image = importedBmp,
                                CaptureDetails = details
                            };
                            CaptureHelper.ImportExtensionCapture(capture, context.Envelope.Browser);
                            Log.Info($"Browser capture successfully imported into pipeline. Title='{title}' Browser='{context.Envelope.Browser}'");
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error forwarding imported capture to CaptureHelper", ex);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to decode or validate imported capture image surface", ex);
            }
        }

        private static void HandleTabChanged(IpcRequestContext context)
        {
            string url = context.Envelope.Url ?? context.Envelope.Metadata?.Url ?? string.Empty;
            string title = context.Envelope.Title ?? context.Envelope.Metadata?.Title ?? string.Empty;

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

        private static async Task HandleListRecipesAsync(IpcRequestContext context)
        {
            var recipeManager = SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true) ?? RecipeManager.Instance;
            var list = new List<object>();

            if (recipeManager != null)
            {
                foreach (var recipe in recipeManager.GetAllRecipes().Where(r => r.IsEnabled))
                {
                    if (recipe.Triggers == null) continue;
                    foreach (var tc in recipe.Triggers.Where(t => t.Enabled && string.Equals(t.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase)))
                    {
                        string cmd = tc.GetParameter<string>("Command") ?? recipe.Id;
                        string desc = tc.GetParameter<string>("Description") ?? recipe.Description ?? string.Empty;
                        bool fnf = tc.GetParameter<bool>("FireAndForget", false);

                        list.Add(new
                        {
                            id = recipe.Id,
                            name = recipe.Name,
                            command = cmd,
                            description = desc,
                            fire_and_forget = fnf
                        });
                    }
                }
            }

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                recipes = list
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
                foreach (var tc in recipe.Triggers.Where(t => t.Enabled && string.Equals(t.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase)))
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

            var cmdTrigger = new CommandlineTrigger(
                $"cli_{matchedRecipe.Id}_{Guid.NewGuid():N}",
                cmdName,
                matchedRecipe.Id,
                cmdName,
                desc,
                fireAndForget);

            var recipeToExecute = TriggerRecipePreparer.Prepare(matchedRecipe, cmdTrigger);

            var cliParams = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (context.Envelope.Parameters != null)
            {
                foreach (var kvp in context.Envelope.Parameters)
                {
                    cliParams[kvp.Key] = kvp.Value;
                }
            }
            if (context.Envelope.Parsed?.Parameters != null)
            {
                foreach (var kvp in context.Envelope.Parsed.Parameters)
                {
                    cliParams[kvp.Key] = kvp.Value;
                }
            }

            // 1. Resolve relative paths in any parameter whose value points to an existing file/directory relative to CWD
            if (!string.IsNullOrWhiteSpace(context.Envelope.Cwd))
            {
                var keysToUpdate = new List<(string Key, string ResolvedPath)>();
                foreach (var kvp in cliParams)
                {
                    if (kvp.Value is string strVal && !string.IsNullOrWhiteSpace(strVal) && !Path.IsPathRooted(strVal))
                    {
                        if (TrySanitizeAndResolvePath(strVal, context.Envelope.Cwd, context.Envelope.Source, out string resolved, out _))
                        {
                            if (File.Exists(resolved) || Directory.Exists(resolved))
                            {
                                keysToUpdate.Add((kvp.Key, resolved));
                            }
                        }
                    }
                }
                foreach (var (k, res) in keysToUpdate)
                {
                    cliParams[k] = res;
                }
            }

            // 2. Normalize 'file' and 'path' aliases to 'Filename' (expected by SourceAcquisitionStep)
            if (!cliParams.ContainsKey("Filename"))
            {
                if (cliParams.TryGetValue("file", out var fVal))
                {
                    cliParams["Filename"] = fVal;
                }
                else if (cliParams.TryGetValue("path", out var pVal))
                {
                    cliParams["Filename"] = pVal;
                }
            }

            // 3. If Filename is present, validate and sanitize it
            if (cliParams.TryGetValue("Filename", out var fnObj) && fnObj is string fnStr && !string.IsNullOrWhiteSpace(fnStr))
            {
                if (TrySanitizeAndResolvePath(fnStr, context.Envelope.Cwd, context.Envelope.Source, out string fullPath, out string sanitizeErr))
                {
                    cliParams["Filename"] = fullPath;
                    cliParams["file"] = fullPath;
                    cliParams["path"] = fullPath;
                }
                else
                {
                    Log.Warn($"[SECURITY] RUN_RECIPE rejected invalid filename '{fnStr}': {sanitizeErr}");
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = $"[SECURITY] Invalid filename parameter: {sanitizeErr}"
                    }).ConfigureAwait(false);
                    return;
                }
            }

            Action<CaptureFlowContext> configureContext = flowCtx =>
            {
                foreach (var kv in cliParams)
                {
                    flowCtx.Properties[kv.Key] = kv.Value;
                }
            };

            if (fireAndForget)
            {
                _ = pipeline.ExecuteAsync(recipeToExecute, cmdTrigger, configureContext);
                await context.ReplyAsync(new
                {
                    status = "ok",
                    exit_code = 0,
                    stdout = $"Recipe '{matchedRecipe.Name}' triggered in background."
                }).ConfigureAwait(false);
            }
            else
            {
                var flowCtx = await pipeline.ExecuteAsync(recipeToExecute, cmdTrigger, configureContext).ConfigureAwait(false);
                if (flowCtx.IsAborted || flowCtx.State == CaptureFlowState.Failed)
                {
                    string err = flowCtx.AbortReason ?? flowCtx.Error?.Message ?? "Recipe execution failed or was aborted.";
                    await context.ReplyAsync(new
                    {
                        status = "error",
                        exit_code = 1,
                        stderr = err
                    }).ConfigureAwait(false);
                }
                else
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
                    else if (flowCtx.Properties.TryGetValue("Text", out var tObj) && tObj != null)
                    {
                        outputText = tObj.ToString();
                    }
                    else if (flowCtx.Properties.TryGetValue("OcrText", out var ocrObj) && ocrObj != null)
                    {
                        outputText = ocrObj.ToString();
                    }
                    else if (!string.IsNullOrWhiteSpace(flowCtx.Payload?.ExtractedText))
                    {
                        outputText = flowCtx.Payload.ExtractedText;
                    }
                    else if (flowCtx.Payload?.RawCapture?.CaptureDetails != null)
                    {
                        var details = flowCtx.Payload.RawCapture.CaptureDetails;
                        if (details.ProcessingTask != null)
                        {
                            try { details.ProcessingTask.Wait(5000); } catch { }
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

                    await context.ReplyAsync(new
                    {
                        status = "ok",
                        exit_code = 0,
                        stdout = outputText
                    }).ConfigureAwait(false);
                }
            }
        }

        private static async Task HandleOpenFileAsync(IpcRequestContext context, Form mainForm, Action<string> onOpenFile)
        {
            var rawFiles = new List<string>();
            if (context.Envelope.Files != null && context.Envelope.Files.Count > 0)
            {
                rawFiles.AddRange(context.Envelope.Files);
            }
            if (rawFiles.Count == 0 && context.Envelope.Parsed?.Parameters != null)
            {
                if (context.Envelope.Parsed.Parameters.TryGetValue("path", out var p) && !string.IsNullOrWhiteSpace(p))
                {
                    rawFiles.Add(p);
                }
                else if (context.Envelope.Parsed.Parameters.TryGetValue("file", out var f) && !string.IsNullOrWhiteSpace(f))
                {
                    rawFiles.Add(f);
                }
            }
            if (rawFiles.Count == 0 && !string.IsNullOrWhiteSpace(context.Envelope.RawInput))
            {
                rawFiles.Add(context.Envelope.RawInput.Trim('"', ' '));
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
                if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
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
                    foreach (var tc in recipe.Triggers.Where(t => t.Enabled && string.Equals(t.TriggerType, TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase)))
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
                            var allowedExts = filter.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(e => e.Trim().StartsWith(".") ? e.Trim() : "." + e.Trim());
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

                        if (fnf || pipeline == null)
                        {
                            _ = pipeline?.ExecuteAsync(recipeToExecute, trigger, ctx => ctx.Properties["Filename"] = file);
                        }
                        else
                        {
                            await pipeline.ExecuteAsync(recipeToExecute, trigger, ctx => ctx.Properties["Filename"] = file).ConfigureAwait(false);
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
                mf.BeginInvoke(new Action(() => mf.ShowSetting(plugin, tab)));
            }
            else
            {
                mainForm?.BeginInvoke(new Action(() =>
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
                mf.BeginInvoke(new Action(() => mf.ShowAbout()));
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

            mainForm?.BeginInvoke(new Action(() =>
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

            mainForm?.BeginInvoke(new Action(() =>
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
            mainForm?.BeginInvoke(new Action(() =>
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
                if (pathAndAction.StartsWith("run/", StringComparison.OrdinalIgnoreCase))
                {
                    parameters["recipe"] = pathAndAction.Substring(4);
                    return "RUN_RECIPE";
                }
                if (string.Equals(pathAndAction, "run", StringComparison.OrdinalIgnoreCase))
                {
                    return "RUN_RECIPE";
                }

                if (string.Equals(pathAndAction, "settings", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pathAndAction, "preferences", StringComparison.OrdinalIgnoreCase))
                {
                    return "SETTINGS";
                }

                if (string.Equals(pathAndAction, "about", StringComparison.OrdinalIgnoreCase))
                {
                    return "ABOUT";
                }

                if (string.Equals(pathAndAction, "self-service", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pathAndAction, "selfservice", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pathAndAction, "diagnostics", StringComparison.OrdinalIgnoreCase))
                {
                    return "SELF_SERVICE";
                }

                if (string.Equals(pathAndAction, "editor", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pathAndAction, "recipe-editor", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pathAndAction, "recipeeditor", StringComparison.OrdinalIgnoreCase))
                {
                    return "RECIPE_EDITOR";
                }

                if (string.Equals(pathAndAction, "recipe-manager", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pathAndAction, "recipemanager", StringComparison.OrdinalIgnoreCase))
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
