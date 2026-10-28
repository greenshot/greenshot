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



using System.Text;
using Greenshot.Linux.Core.Ipc;
using Greenshot.Linux.Recipes.Approval;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Ipc;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Triggers;

namespace Greenshot.Linux.Recipes.Ai
{
    /// <summary>
    /// The commands of greenshot-mcp.exe (AI tools). The caller (<see cref="IpcSecurityDispatcher"/>) already checked the source,
    /// the connection (<see cref="AiToolCaller"/>) and, except for LIST_AI_TOOLS, the user's consent.
    /// <list type="bullet">
    /// <item>LIST_WINDOWS: the windows, with ids instead of handles (<see cref="AiWindowRefs"/>)</item>
    /// <item>LIST_AI_TOOLS: the recipes with an AI tool trigger, these are the AI tool's tools</item>
    /// <item>RUN_AI_TOOL: runs such a recipe and replies with its result and image; everything an AI tool captures goes through a recipe</item>
    /// </list>
    /// </summary>
    public static class AiToolIpcHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AiToolIpcHandler));

        /// <summary>
        /// Default for the MaxImageSize parameter of an AI tool trigger: images are scaled so the longest side is at most this
        /// (larger images cost the AI more and are scaled down by the model anyway)
        /// </summary>
        public const int DefaultMaxImageSize = 1568;

        /// <summary>
        /// Tool names greenshot-mcp.exe offers itself
        /// </summary>
        private static readonly HashSet<string> ReservedToolNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "list_windows" };

        /// <summary>
        /// LIST_WINDOWS: the windows on the visible workspaces, top most first, with ids for Window arguments, and the displays.
        /// Windows of excluded applications are left out. Only Sway and Hyprland tell where windows are.
        /// </summary>
        public static async Task HandleListWindowsAsync(IpcRequestContext context, CancellationToken cancellationToken = default)
        {
            if (context.AiClient is not AiToolClient client)
            {
                await ReplyErrorAsync(context, AiToolAccess.NotAllowedMessage, cancellationToken).ConfigureAwait(false);
                return;
            }

            var compositor = RecipeServices.Compositor;
            var windows = new List<object>();
            int excludedCount = 0;
            if (compositor is { KnowsWindows: true })
            {
                foreach (var window in await compositor.GetVisibleWindowsAsync().ConfigureAwait(false))
                {
                    if (AiToolAccess.IsWindowExcluded(window))
                    {
                        excludedCount++;
                        continue;
                    }

                    if (window.Id == null || window.ProcessId == 0)
                    {
                        continue;
                    }

                    windows.Add(new
                    {
                        id = AiWindowRefs.Register(client, window),
                        title = window.Title,
                        process = AiToolAccess.GetApplicationName(window),
                        x = window.Bounds.X,
                        y = window.Bounds.Y,
                        width = window.Bounds.Width,
                        height = window.Bounds.Height,
                        minimized = false,
                        active = window.Focused
                    });
                }
            }

            var layout = compositor == null ? null : await compositor.GetLayoutAsync().ConfigureAwait(false);
            var displays = (layout?.Screens ?? []).Select((d, index) => new
            {
                index,
                name = d.Name,
                primary = d.IsPrimary,
                x = d.Bounds.X,
                y = d.Bounds.Y,
                width = d.Bounds.Width,
                height = d.Bounds.Height
            }).ToList();

            string note = compositor is { KnowsWindows: true }
                ? null
                : $"This desktop ({compositor?.Name ?? "unknown"}) doesn't tell other programs where windows are: use capture_screen or capture_region.";
            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                windows,
                displays,
                excluded_windows = excludedCount,
                id_lifetime_minutes = (int)AiWindowRefs.Lifetime.TotalMinutes,
                note,
                stdout = $"{windows.Count} windows, {displays.Count} displays." + (note == null ? string.Empty : " " + note)
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// An enabled AI tool trigger of an enabled recipe
        /// </summary>
        internal sealed class AiToolEntry
        {
            public CaptureRecipe Recipe { get; set; }
            public TriggerConfig Trigger { get; set; }
            public string ToolName { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public List<CommandlineArgument> Arguments { get; set; }
        }

        /// <summary>
        /// The tools: one per enabled AI tool trigger of the enabled recipes. A name used twice belongs to the first recipe.
        /// </summary>
        internal static List<AiToolEntry> GetAiTools()
        {
            var result = new List<AiToolEntry>();
            var names = new HashSet<string>(ReservedToolNames, StringComparer.OrdinalIgnoreCase);
            var recipeManager = RecipeServices.Manager ?? RecipeManager.Instance;
            if (recipeManager == null)
            {
                return result;
            }
            foreach (var recipe in recipeManager.GetAllRecipes().Where(r => r.IsEnabled && r.Triggers != null))
            {
                foreach (var trigger in recipe.Triggers.Where(t => t != null && t.IsActive && string.Equals(t.TriggerType, TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase)))
                {
                    string toolName = trigger.GetParameter<string>("ToolName");
                    if (!AiToolTrigger.IsValidToolName(toolName))
                    {
                        Log.Warn($"The AI tool trigger of recipe '{recipe.Id}' has no valid ToolName ('{toolName}'), it is not offered.");
                        continue;
                    }
                    if (!names.Add(toolName))
                    {
                        Log.Warn($"The AI tool '{toolName}' of recipe '{recipe.Id}' is not offered, another recipe (or greenshot-mcp) uses that name.");
                        continue;
                    }
                    result.Add(new AiToolEntry
                    {
                        Recipe = recipe,
                        Trigger = trigger,
                        ToolName = toolName,
                        Title = trigger.GetParameter<string>("Title") ?? recipe.Name,
                        Description = trigger.GetParameter<string>("Description") ?? recipe.Description ?? recipe.Name,
                        Arguments = trigger.GetParameter<List<CommandlineArgument>>("Arguments") ?? new List<CommandlineArgument>()
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// LIST_AI_TOOLS: the tools greenshot-mcp.exe offers besides list_windows. Names and descriptions only, no screen contents,
        /// so this needs no consent (the AI tool can show its tools before the user is asked).
        /// </summary>
        public static Task HandleListAiToolsAsync(IpcRequestContext context, CancellationToken cancellationToken = default)
        {
            var tools = GetAiTools().Select(tool => new
            {
                name = tool.ToolName,
                title = tool.Title,
                description = tool.Description,
                recipe = tool.Recipe.Id,
                read_only = tool.Trigger.GetParameter("ReadOnly", true),
                destructive = tool.Trigger.GetParameter("Destructive", false),
                arguments = tool.Arguments.Where(a => !string.IsNullOrWhiteSpace(a?.Name)).Select(a => new
                {
                    name = a.Name,
                    description = a.Description ?? string.Empty,
                    required = a.Required,
                    default_value = a.DefaultValue,
                    type = a.Type.ToString(),
                    allowed_values = a.AllowedValues
                })
            }).ToList();

            return context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                tools,
                stdout = $"{tools.Count} AI tools."
            }, cancellationToken);
        }

        /// <summary>
        /// What the flow produced, collected before the flow disposes its payload
        /// </summary>
        internal sealed class AiToolResult
        {
            public byte[] Png;
            public int Width;
            public int Height;
            public int OriginalWidth;
            public int OriginalHeight;
            public string Title;
            public string Source;
            public string Text;
            public IReadOnlyList<Greenshot.Linux.Core.Ocr.OcrLine> OcrLines;
        }

        /// <summary>
        /// RUN_AI_TOOL: runs the recipe of the tool (Recipe = tool name, Parameters = arguments) and replies with the result:
        /// stdout / stderr, the JSON-safe variables, the final image (PNG, base64) and the text found by OCR.
        /// </summary>
        public static async Task HandleRunAiToolAsync(IpcRequestContext context, CancellationToken cancellationToken = default)
        {
            string toolName = context.Envelope.Recipe?.Trim();
            var tool = GetAiTools().FirstOrDefault(t => string.Equals(t.ToolName, toolName, StringComparison.OrdinalIgnoreCase));
            if (tool == null)
            {
                await ReplyErrorAsync(context, $"There is no AI tool '{toolName}' (anymore), the tools are the recipes with an AI tool trigger.", cancellationToken).ConfigureAwait(false);
                return;
            }

            var supplied = new Dictionary<string, string>(context.Envelope.Parameters ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            var binding = CommandlineArgumentBinder.Bind(tool.Arguments, supplied, tool.ToolName, null, IpcSources.Mcp, context.AiClient);
            if (!binding.Success)
            {
                await ReplyErrorAsync(context, binding.Error, cancellationToken).ConfigureAwait(false);
                return;
            }

            var runner = RecipeServices.Runner;
            if (runner == null)
            {
                await ReplyErrorAsync(context, "The recipe engine is not available.", cancellationToken).ConfigureAwait(false);
                return;
            }

            int maxImageSize = Math.Max(0, tool.Trigger.GetParameter("MaxImageSize", DefaultMaxImageSize));
            var trigger = new AiToolTrigger($"ai_{tool.Recipe.Id}_{Guid.NewGuid():N}", tool.Title, tool.Recipe.Id, tool.ToolName, tool.Description, tool.Arguments);
            var recipeToExecute = TriggerRecipePreparer.Prepare(tool.Recipe, trigger);

            var stdout = new List<string>();
            var stderr = new List<string>();
            AiToolResult result = null;
            Action<CaptureFlowContext> configureContext = flowContext =>
            {
                foreach (var kv in binding.Variables)
                {
                    flowContext.Properties[kv.Key] = kv.Value;
                }
                flowContext.StdoutWriter = text =>
                {
                    lock (stdout)
                    {
                        stdout.Add(text);
                    }
                    return Task.CompletedTask;
                };
                flowContext.StderrWriter = text =>
                {
                    lock (stderr)
                    {
                        stderr.Add(text);
                    }
                    return Task.CompletedTask;
                };
                flowContext.FlowFinishedAsync = async finished => result = await CollectResultAsync(finished, maxImageSize, cancellationToken).ConfigureAwait(false);
            };

            var flowResult = await runner.Start(recipeToExecute, FlowTriggerContext.Empty(trigger), configureContext).Completion.ConfigureAwait(false);
            var flowContext = flowResult.Context;
            bool failed = flowContext == null || flowContext.IsAborted || flowContext.State == CaptureFlowState.Failed;
            int exitCode = flowContext != null && flowContext.ExitCode != 0 ? flowContext.ExitCode : (failed ? 1 : 0);

            if (result?.Png != null)
            {
                AiToolAccess.NotifyCapture((context.AiClient as AiToolClient)?.DisplayName, $"'{result.Title ?? tool.Title}' ({tool.Title})");
            }

            string stderrText = Join(stderr);
            if (failed && string.IsNullOrEmpty(stderrText))
            {
                stderrText = flowContext?.AbortReason ?? flowContext?.Error?.Message ?? flowResult.Reason ?? "The recipe failed.";
            }

            await context.ReplyAsync(new
            {
                status = failed ? "error" : "ok",
                exit_code = exitCode,
                tool = tool.ToolName,
                recipe = tool.Recipe.Id,
                stdout = Join(stdout),
                stderr = stderrText,
                variables = flowContext == null ? null : GetVariables(flowContext, result?.Text),
                image = result?.Png == null ? null : new
                {
                    mime_type = "image/png",
                    data = Convert.ToBase64String(result.Png),
                    width = result.Width,
                    height = result.Height,
                    original_width = result.OriginalWidth,
                    original_height = result.OriginalHeight
                },
                title = result?.Title,
                source = result?.Source,
                text = result?.Text,
                // OCR bounds are in pixels of the original (unscaled) image
                ocr_lines = result?.OcrLines?.Select(l => new
                {
                    text = l.Text,
                    x = l.Bounds.Left,
                    y = l.Bounds.Top,
                    width = l.Bounds.Width,
                    height = l.Bounds.Height
                })
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The JSON-safe variables, without the copies of the OCR text (OcrText, Text, CommandResult): it is in "text" once
        /// </summary>
        private static IDictionary<string, object> GetVariables(CaptureFlowContext flowContext, string text)
        {
            var variables = RecipeCommands.SnapshotJsonSafe(flowContext.Properties);
            if (!string.IsNullOrEmpty(text))
            {
                foreach (var key in variables.Where(v => v.Value is string value && value == text).Select(v => v.Key).ToList())
                {
                    variables.Remove(key);
                }
            }
            return variables;
        }

        private static string Join(List<string> chunks)
        {
            lock (chunks)
            {
                return chunks.Count == 0 ? null : string.Join("\n", chunks);
            }
        }

        /// <summary>
        /// The final image of the flow (with what the recipe drew on it), and the text found by OCR
        /// </summary>
        internal static async Task<AiToolResult> CollectResultAsync(CaptureFlowContext flowContext, int maxImageSize, CancellationToken cancellationToken)
        {
            var payload = flowContext.Payload;
            if (payload?.RawCapture == null || payload.Image == null)
            {
                return null;
            }

            var details = payload.Details;
            var result = new AiToolResult
            {
                Title = details?.Title,
                Source = payload.Metadata.TryGetValue("source", out var source) ? source?.ToString() : null,
                Text = payload.ExtractedText
            };
            if (payload.Metadata.TryGetValue(Steps.OcrStep.OcrLinesKey, out var lines) && lines is IReadOnlyList<Greenshot.Linux.Core.Ocr.OcrLine> { Count: > 0 } ocrLines)
            {
                result.OcrLines = ocrLines;
            }

            if (payload.Surface == null)
            {
                Encode(result, payload.Image, maxImageSize);
                return result;
            }

            // A surface (the recipe drew on the capture): its rendering, owned by the payload
            var exportSource = await payload.GetExportSourceAsync(RecipeServices.Ui ?? InlineUiDispatcher.Instance, cancellationToken).ConfigureAwait(false);
            Encode(result, exportSource.Image, maxImageSize);
            return result;
        }

        private static void Encode(AiToolResult result, SkiaSharp.SKBitmap image, int maxImageSize)
        {
            result.OriginalWidth = image.Width;
            result.OriginalHeight = image.Height;
            result.Png = AiToolCapture.EncodePng(image, maxImageSize, out result.Width, out result.Height);
        }

        private static Task ReplyErrorAsync(IpcRequestContext context, string message, CancellationToken cancellationToken)
        {
            return context.ReplyAsync(new
            {
                status = "error",
                exit_code = 1,
                stderr = message
            }, cancellationToken);
        }
    }
}
