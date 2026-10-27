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

using System.Text.Json;
using Greenshot.Linux.Core.Ipc;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Expressions;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Steps;
using Greenshot.Linux.Recipes.Triggers;

namespace Greenshot.Linux.Recipes.Ipc
{
    /// <summary>
    /// The recipe commands of the command line (and of greenshot: links): list the recipes with a Commandline trigger,
    /// describe one, run one and wait for its output and exit code. The handlers of the IpcSecurityDispatcher of Windows.
    /// </summary>
    public static class RecipeCommands
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeCommands));

        private static readonly JsonSerializerOptions NodeOptions = new();

        private static IRecipeManager Manager => RecipeServices.Manager ?? RecipeManager.Instance;

        public static async Task ListRecipesAsync(IpcRequestContext context)
        {
            var list = new List<object>();
            foreach (var recipe in (Manager.GetAllRecipes()).Where(r => r.IsEnabled))
            {
                foreach (var tc in (recipe.Triggers ?? []).Where(t => t.IsActive && string.Equals(t.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase)))
                {
                    var args = tc.GetParameter<List<CommandlineArgument>>("Arguments") ?? new List<CommandlineArgument>();
                    list.Add(new
                    {
                        id = recipe.Id,
                        name = recipe.Name,
                        command = tc.GetParameter<string>("Command") ?? recipe.Id,
                        description = tc.GetParameter<string>("Description") ?? recipe.Description ?? string.Empty,
                        fire_and_forget = tc.GetParameter("FireAndForget", false),
                        stdout = tc.GetParameter<string>("Stdout"),
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

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                recipes = list,
                stdout = context.WantsTextOutput ? CliTextRenderer.RenderRecipeList(JsonSerializer.SerializeToNode(list, NodeOptions)) : null
            }).ConfigureAwait(false);
        }

        public static async Task DescribeRecipeAsync(IpcRequestContext context)
        {
            string target = (context.Envelope.Recipe ?? context.Envelope.RawInput)?.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                await ErrorAsync(context, "Missing required argument: recipe name or ID to describe.").ConfigureAwait(false);
                return;
            }

            var recipe = (Manager.GetAllRecipes()).FirstOrDefault(r =>
                string.Equals(r.Id, target, StringComparison.OrdinalIgnoreCase) || string.Equals(r.Name, target, StringComparison.OrdinalIgnoreCase)
                || (r.Triggers ?? []).Any(t => string.Equals(t.GetParameter<string>("Command"), target, StringComparison.OrdinalIgnoreCase)));
            if (recipe == null)
            {
                await ErrorAsync(context, $"Recipe '{target}' not found.").ConfigureAwait(false);
                return;
            }

            var contract = RecipeContract.Analyze(recipe);
            var info = new
            {
                id = recipe.Id,
                name = recipe.Name,
                description = recipe.Description ?? string.Empty,
                category = "General",
                triggers = recipe.Triggers?.Select(t => new
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
                recipe = info,
                stdout = context.WantsTextOutput ? CliTextRenderer.RenderRecipeDescription(JsonSerializer.SerializeToNode(info, NodeOptions)) : null
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs a recipe with an active Commandline trigger: its declared arguments, the output of its Stdout and Stderr
        /// steps streamed, then its exit code. --async returns right away, --query prints a value, --json one JSON document.
        /// </summary>
        public static async Task RunRecipeAsync(IpcRequestContext context)
        {
            string target = (context.Envelope.Recipe ?? context.Envelope.RawInput)?.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                await ErrorAsync(context, "Missing required argument: recipe command or recipe ID.").ConfigureAwait(false);
                return;
            }


            CaptureRecipe recipe = null;
            TriggerConfig triggerConfig = null;
            foreach (var candidate in Manager.GetAllRecipes().Where(r => r.IsEnabled))
            {
                triggerConfig = (candidate.Triggers ?? []).FirstOrDefault(tc => tc.IsActive
                    && string.Equals(tc.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(tc.GetParameter<string>("Command"), target, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(candidate.Id, target, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(candidate.Name, target, StringComparison.OrdinalIgnoreCase)));
                if (triggerConfig != null)
                {
                    recipe = candidate;
                    break;
                }
            }

            if (recipe == null)
            {
                await ErrorAsync(context, $"Recipe '{target}' not found or does not have an active CommandlineTrigger.").ConfigureAwait(false);
                return;
            }

            // Only from the terminal: a greenshot: link (a web page) may only start a recipe which allows it
            if (!string.Equals(context.Envelope.Source, IpcSources.Cli, StringComparison.OrdinalIgnoreCase)
                && !(triggerConfig.GetParameter("AllowBrowserInvocation", false) && triggerConfig.IsBrowserInvocationApproved))
            {
                Log.Warn($"[SECURITY] RUN_RECIPE rejected: recipe '{recipe.Id}' does not allow invocation from source '{context.Envelope.Source}'.");
                await ErrorAsync(context, $"[SECURITY] Recipe '{target}' does not allow invocation from a link. Set 'AllowBrowserInvocation' on its Commandline trigger to enable this.").ConfigureAwait(false);
                return;
            }

            bool fireAndForget = context.Envelope.Async || triggerConfig.GetParameter("FireAndForget", false);
            string commandName = triggerConfig.GetParameter<string>("Command") ?? recipe.Id;
            string stdoutExpression = triggerConfig.GetParameter<string>("Stdout");
            var declaredArgs = triggerConfig.GetParameter<List<CommandlineArgument>>("Arguments") ?? new List<CommandlineArgument>();
            var trigger = new CommandlineTrigger($"cli_{recipe.Id}_{Guid.NewGuid():N}", commandName, recipe.Id, commandName,
                triggerConfig.GetParameter<string>("Description") ?? recipe.Description, fireAndForget, stdoutExpression, declaredArgs);
            var recipeToRun = TriggerRecipePreparer.Prepare(recipe, trigger);

            var supplied = new Dictionary<string, string>(context.Envelope.Parameters ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            supplied.Remove("recipe");
            // Only declared arguments, converted to their declared type
            var binding = CommandlineArgumentBinder.Bind(declaredArgs, supplied, commandName, context.Envelope.Cwd, context.Envelope.Source, context.AiClient);
            if (!binding.Success)
            {
                Log.Warn($"RUN_RECIPE '{recipe.Id}' rejected: {binding.Error}");
                await context.ReplyAsync(new { status = "error", exit_code = CliCommandParser.UsageExitCode, stderr = binding.Error }).ConfigureAwait(false);
                return;
            }

            string query = string.IsNullOrWhiteSpace(context.Envelope.Query) ? null : context.Envelope.Query;
            bool json = context.Envelope.Json;
            // --query evaluates any expression (environment, configuration): the terminal only
            if (query != null && !string.Equals(context.Envelope.Source, IpcSources.Cli, StringComparison.OrdinalIgnoreCase))
            {
                await ErrorAsync(context, "[SECURITY] Queries are only allowed from the command line.").ConfigureAwait(false);
                return;
            }

            // Plain runs stream the Stdout / Stderr steps; --query prints only its value, --json collects everything in one document
            bool streamStdout = query == null && !json;
            bool streamStderr = !json;
            var collectedStdout = new List<string>();
            var collectedStderr = new List<string>();
            bool streamedStdout = false, streamedStderr = false;

            void Configure(CaptureFlowContext flow)
            {
                foreach (var variable in binding.Variables)
                {
                    flow.Properties[variable.Key] = variable.Value;
                }

                // Fire and forget: the caller is gone, nothing may write to the connection
                if (fireAndForget)
                {
                    return;
                }

                flow.StdoutWriter = async text =>
                {
                    lock (collectedStdout)
                    {
                        collectedStdout.Add(text);
                    }

                    if (streamStdout)
                    {
                        streamedStdout = true;
                        await context.ReplyAsync(new { stream = "stdout", text }).ConfigureAwait(false);
                    }
                };
                flow.StderrWriter = async text =>
                {
                    lock (collectedStderr)
                    {
                        collectedStderr.Add(text);
                    }

                    if (streamStderr)
                    {
                        streamedStderr = true;
                        await context.ReplyAsync(new { stream = "stderr", text }).ConfigureAwait(false);
                    }
                };
            }

            if (RecipeServices.Runner == null)
            {
                await ErrorAsync(context, "The recipe engine is not available.").ConfigureAwait(false);
                return;
            }

            if (fireAndForget)
            {
                RecipeServices.Runner.Start(recipeToRun, FlowTriggerContext.Empty(trigger), Configure);
                await context.ReplyAsync(new
                {
                    status = "ok",
                    exit_code = 0,
                    recipe = recipe.Id,
                    stdout = $"Recipe '{recipe.Name}' triggered in background."
                }).ConfigureAwait(false);
                return;
            }

            var result = await RecipeServices.Runner.Start(recipeToRun, FlowTriggerContext.Empty(trigger), Configure).Completion.ConfigureAwait(false);
            var flowContext = result.Context ?? ContextForResult(recipeToRun, trigger, result);
            bool failed = flowContext.IsAborted || flowContext.State == CaptureFlowState.Failed;
            // A Stderr step's exit code counts, also when it didn't abort
            int exitCode = flowContext.ExitCode != 0 ? flowContext.ExitCode : failed ? 1 : 0;

            string stdoutText = null;
            string stderrText = null;
            if (failed)
            {
                // A Stderr step already printed its message
                if (!streamedStderr)
                {
                    stderrText = collectedStderr.Count > 0 ? Join(collectedStderr) : flowContext.AbortReason ?? flowContext.Error?.Message ?? "Recipe execution failed or was aborted.";
                }

                if (json && collectedStdout.Count > 0)
                {
                    stdoutText = Join(collectedStdout);
                }
            }
            else
            {
                if (query != null)
                {
                    stdoutText = ExpressionEvaluator.Instance.Evaluate(query, flowContext)?.ToString();
                }
                else if (streamedStdout)
                {
                    stdoutText = null;
                }
                else if (collectedStdout.Count > 0)
                {
                    stdoutText = Join(collectedStdout);
                }
                else if (!string.IsNullOrWhiteSpace(stdoutExpression))
                {
                    stdoutText = ExpressionEvaluator.Instance.Evaluate(stdoutExpression, flowContext)?.ToString();
                }
                else
                {
                    stdoutText = FallbackOutput(flowContext, recipe);
                }

                if (!streamedStderr && collectedStderr.Count > 0)
                {
                    stderrText = Join(collectedStderr);
                }
            }

            if (json)
            {
                // Only values JSON can hold: the context also has images, windows and settings
                await context.ReplyAsync(new
                {
                    status = failed ? "error" : "ok",
                    exit_code = exitCode,
                    recipe = recipe.Id,
                    stdout = stdoutText,
                    stderr = stderrText,
                    payload = PayloadSummary(flowContext.Payload),
                    variables = SnapshotJsonSafe(flowContext.Properties)
                }).ConfigureAwait(false);
            }
            else
            {
                await context.ReplyAsync(new { status = failed ? "error" : "ok", exit_code = exitCode, stdout = stdoutText, stderr = stderrText }).ConfigureAwait(false);
            }
        }

        public static Task ErrorAsync(IpcRequestContext context, string message, int exitCode = 1) =>
            context.ReplyAsync(new { status = "error", exit_code = exitCode, stderr = message });

        private static string Join(List<string> chunks)
        {
            lock (chunks)
            {
                return string.Join("\n", chunks);
            }
        }

        /// <summary>
        /// The output of a recipe without Stdout step, Stdout expression or query: its text, the saved file or a message
        /// </summary>
        private static string FallbackOutput(CaptureFlowContext flow, CaptureRecipe recipe)
        {
            if (flow.Properties.TryGetValue("LastStdout", out var last) && last != null)
            {
                return last.ToString();
            }

            if (!string.IsNullOrWhiteSpace(flow.Payload?.ExtractedText))
            {
                return flow.Payload.ExtractedText;
            }

            if (flow.Properties.TryGetValue("Destination.Filename", out var file) && file != null)
            {
                return file.ToString();
            }

            if (flow.Payload?.Metadata != null && flow.Payload.Metadata.TryGetValue(OcrStep.OcrLinesKey, out var lines)
                && lines is IEnumerable<Greenshot.Linux.Core.Ocr.OcrLine> ocrLines && ocrLines.Any())
            {
                return string.Join(Environment.NewLine, ocrLines.Select(l => l.Text));
            }

            return $"Recipe '{recipe.Name}' completed successfully.";
        }

        private static CaptureFlowContext ContextForResult(CaptureRecipe recipe, ITrigger trigger, CaptureFlowResult result)
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

        private static object PayloadSummary(ICapturePayload payload)
        {
            if (payload == null)
            {
                return null;
            }

            var image = payload.Surface?.Image ?? payload.RawCapture?.Image;
            return new
            {
                width = image?.Width ?? 0,
                height = image?.Height ?? 0,
                format = image?.ColorType.ToString(),
                extracted_text = payload.ExtractedText,
                metadata = SnapshotJsonSafe(payload.Metadata)
            };
        }

        /// <summary>
        /// The values JSON can hold as they are: text, numbers, booleans, dates, enums and lists of text
        /// </summary>
        public static SortedDictionary<string, object> SnapshotJsonSafe(IDictionary<string, object> values)
        {
            var result = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (values == null)
            {
                return result;
            }

            foreach (var pair in values.ToList())
            {
                if (!string.IsNullOrEmpty(pair.Key) && TryGetJsonSafeValue(pair.Value, out var safe))
                {
                    result[pair.Key] = safe;
                }
            }

            return result;
        }

        private static bool TryGetJsonSafeValue(object value, out object safe)
        {
            safe = value switch
            {
                null => null,
                string or decimal => value,
                char c => c.ToString(),
                DateTime d => d.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                DateTimeOffset d => d.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                TimeSpan t => t.ToString("c", System.Globalization.CultureInfo.InvariantCulture),
                Guid g => g.ToString(),
                Enum e => e.ToString(),
                IEnumerable<string> strings => strings.ToList(),
                _ when value.GetType().IsPrimitive && value is not IntPtr and not UIntPtr => value,
                _ => (object)Unsafe.Instance,
            };
            if (ReferenceEquals(safe, Unsafe.Instance))
            {
                safe = null;
                return false;
            }

            return true;
        }

        private sealed class Unsafe
        {
            public static readonly Unsafe Instance = new();
        }
    }
}
