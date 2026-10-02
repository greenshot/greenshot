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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Base.Triggers;
using Greenshot.Recipes;
using Greenshot.UI;
using log4net;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// AI tools writing recipes (greenshot-mcp.exe). The caller (<see cref="IpcSecurityDispatcher"/>) already checked the source,
    /// the connection (<see cref="AiToolCaller"/>) and the user's consent for the AI tool.
    /// <list type="bullet">
    /// <item>RECIPE_CATALOG: what a recipe can use on this PC: step types with their contracts, destinations, processors, triggers and the existing recipes</item>
    /// <item>VALIDATE_RECIPE: checks a recipe, nothing is saved or shown</item>
    /// <item>PROPOSE_RECIPE: shows a new recipe (or a change of an existing one) in the approval window, it is only saved when the user approves it</item>
    /// </list>
    /// </summary>
    public static class AiRecipeIpcHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AiRecipeIpcHandler));

        /// <summary>
        /// The largest recipe JSON an AI tool can send
        /// </summary>
        public const int MaxRecipeLength = 256 * 1024;

        public const string RecipeParameter = "recipe";
        public const string RequestParameter = "request";
        public const string ExplanationParameter = "explanation";
        public const string ReplacesParameter = "replaces";

        /// <summary>
        /// One proposal at a time: the approval window is modal
        /// </summary>
        private static readonly SemaphoreSlim ProposalLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Proposals the user rejected (AI tool + recipe content), not shown again until Greenshot restarts
        /// </summary>
        private static readonly HashSet<string> RejectedProposals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Shows the proposal and returns the user's decision; replaceable for tests
        /// </summary>
        internal static Func<RecipeApprovalRequest, CancellationToken, Task<RecipeApprovalWindow.ApprovalResult>> ApprovalPrompt { get; set; } = ShowApprovalAsync;

        /// <summary>
        /// The directory for the recipes AI tools write; replaceable for tests
        /// </summary>
        internal static Func<string> AiRecipeDirectoryProvider { get; set; } = GetDefaultAiRecipeDirectory;

        /// <summary>
        /// RECIPE_CATALOG: what recipes can use, or with the parameter "recipe" (an id) the JSON of that recipe, to change it
        /// </summary>
        public static Task HandleRecipeCatalogAsync(IpcRequestContext context, CancellationToken cancellationToken = default)
        {
            string recipeId = GetParameter(context, RecipeParameter)?.Trim();
            if (string.IsNullOrEmpty(recipeId))
            {
                return context.ReplyAsync(BuildCatalog(), cancellationToken);
            }
            var recipe = RecipeManager.Instance.GetRecipeById(recipeId);
            if (recipe == null)
            {
                return ReplyErrorAsync(context, $"There is no recipe with the id '{recipeId}'.", cancellationToken);
            }
            var copy = recipe.Clone();
            copy.FilePath = null;
            return context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                recipe = recipe.Id,
                built_in = recipe.IsBuiltIn,
                replaces_built_in = recipe.IsBuiltIn && recipe.IsOverridden,
                recipe_json = RecipeSerializer.Serialize(copy)
            }, cancellationToken);
        }

        internal static object BuildCatalog()
        {
            var steps = StepRegistry.Instance.Contracts
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.StepType))
                .OrderBy(c => c.StepType, StringComparer.OrdinalIgnoreCase)
                .Select(DescribeContract)
                .ToList();

            var destinations = SafeList(() => DestinationHelper.GetAllDestinations())
                .Where(d => d != null && !string.IsNullOrWhiteSpace(d.Designation))
                .Select(d => new
                {
                    designation = d.Designation,
                    name = d.Descriptor?.DisplayName ?? d.Designation,
                    uploads = d is IRequiresRecipeAuthorization authorization && authorization.GetGatedActions().Any(a => a.GateType == RecipeGateType.NetworkAccess)
                })
                .OrderBy(d => d.designation, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var processors = SafeList(() => SimpleServiceProvider.Current.GetAllInstances<IProcessor>())
                .Where(p => p != null)
                .Select(p => new { id = p.GetType().Name, designation = p.Designation, description = p.Description })
                .ToList();

            var recipeManager = RecipeManager.Instance;
            var recipes = recipeManager.GetAllRecipes().Select(r => new
            {
                id = r.Id,
                name = r.Name,
                description = r.Description,
                built_in = r.IsBuiltIn,
                replaces_built_in = r.IsBuiltIn && r.IsOverridden,
                enabled = r.IsEnabled,
                ai_created = !string.IsNullOrEmpty(r.FilePath) && (RecipeTrustStore.GetTrustRecord(r.FilePath)?.IsAiCreated ?? false),
                triggers = r.Triggers?.Where(t => t != null).Select(t => t.TriggerType).ToList() ?? new List<string>()
            }).ToList();

            return new
            {
                status = "ok",
                exit_code = 0,
                notes = new[]
                {
                    "Write a recipe as JSON following the recipe schema (get_recipe_schema), using only the step types, destinations and processors listed here.",
                    "Check it with validate_recipe, then propose_recipe shows it to the user. Nothing is saved or run unless the user approves it.",
                    "A new recipe needs an id that no recipe uses; to change a recipe (also a built-in one) use update_recipe with its id.",
                    "The user switches each trigger on or off; for a recipe from an AI tool they start switched off. Uploads, files outside the output folder and external commands need the user's permission.",
                    "Destinations with uploads=true send the capture to the internet."
                },
                trigger_types = TriggerTypes,
                steps,
                destinations,
                processors,
                recipes
            };
        }

        private static readonly object[] TriggerTypes =
        {
            new { type = TriggerConfig.TypeHotkey, parameters = "Hotkey (e.g. \"Ctrl+Shift+F9\")", note = "starts when the user presses the hotkey" },
            new { type = TriggerConfig.TypeContextMenu, parameters = "MenuItemText, Group, Order", note = "an entry in the Greenshot menu" },
            new { type = TriggerConfig.TypeEditor, parameters = "MenuItemText, Group, Order", note = "an entry in the editor's recipe menu, works on the open image" },
            new { type = TriggerConfig.TypeClipboard, parameters = "OnImageCopied, FormatFilter", note = "runs on its own when an image is copied" },
            new { type = TriggerConfig.TypeManual, parameters = "", note = "only started from the recipe list" },
            new { type = TriggerConfig.TypeCommandline, parameters = "Command, Description, FireAndForget, Stdout, Arguments, AllowBrowserInvocation", note = "greenshot.com run <Command>" },
            new { type = TriggerConfig.TypeOpenFile, parameters = "Filter, FireAndForget", note = "runs for files opened with Greenshot" },
            new { type = TriggerConfig.TypeExtension, parameters = "Browser, FireAndForget", note = "handles captures from the browser extension" },
            new { type = TriggerConfig.TypeAiTool, parameters = "ToolName, Title, Description, ReadOnly, Destructive, Arguments, MaxImageSize", note = "offers the recipe as a tool to AI tools" }
        };

        private static object DescribeContract(StepContract contract)
        {
            return new
            {
                step_type = contract.StepType,
                name = contract.DisplayName,
                description = contract.Description,
                category = contract.Category,
                accepts_other_parameters = contract.AcceptsUndeclaredParameters,
                parameters = contract.Parameters?.Select(p => new
                {
                    name = p.Name,
                    type = p.DataType.ToString(),
                    required = p.Required,
                    default_value = p.DefaultValue?.ToString(),
                    allowed_values = SafeList(() => p.AllowedValues),
                    supports_expressions = p.SupportsExpressions,
                    description = p.Description
                }).ToList(),
                inputs = contract.InputVariables?.Select(v => new { name = v.Name, type = v.DataType.ToString(), required = v.Required, description = v.Description }).ToList(),
                outputs = contract.OutputVariables?.Select(v => new { name = v.Name, type = v.DataType.ToString(), conditional = v.Conditional, description = v.Description }).ToList(),
                payload = contract.PayloadContract == null ? null : new
                {
                    raw_capture = contract.PayloadContract.RawCapture.ToString(),
                    surface = contract.PayloadContract.Surface.ToString(),
                    extracted_text = contract.PayloadContract.ExtractedText.ToString(),
                    visual_mutation = contract.PayloadContract.VisualMutation.ToString()
                }
            };
        }

        private static List<T> SafeList<T>(Func<IEnumerable<T>> source)
        {
            try
            {
                return source()?.ToList() ?? new List<T>();
            }
            catch (Exception ex)
            {
                Log.Debug("Could not collect a part of the recipe catalog.", ex);
                return new List<T>();
            }
        }

        /// <summary>
        /// A recipe from an AI tool, parsed and validated
        /// </summary>
        internal sealed class ParsedProposal
        {
            public CaptureRecipe Recipe { get; set; }
            public RecipeValidationResult Validation { get; set; }

            /// <summary>
            /// The recipe as Greenshot writes it: this is shown, hashed and saved (not the AI's formatting)
            /// </summary>
            public string Content { get; set; }

            public string Error { get; set; }
        }

        /// <summary>
        /// Parses and validates the recipe JSON of an AI tool: exactly one recipe
        /// </summary>
        internal static ParsedProposal Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ParsedProposal { Error = "The recipe JSON is missing." };
            }
            if (json.Length > MaxRecipeLength)
            {
                return new ParsedProposal { Error = $"The recipe is too large (more than {MaxRecipeLength / 1024} KB)." };
            }

            List<CaptureRecipe> recipes;
            try
            {
                recipes = RecipeSerializer.DeserializeList(json, validate: false);
            }
            catch (Exception ex)
            {
                return new ParsedProposal { Error = $"The recipe is not valid JSON for a recipe: {ex.Message}" };
            }
            if (recipes.Count != 1 || recipes[0] == null)
            {
                return new ParsedProposal { Error = "Send exactly one recipe (a JSON object, not a list)." };
            }

            var recipe = recipes[0];
            // Where it comes from is decided by Greenshot, not by the JSON
            recipe.FilePath = null;
            recipe.IsBuiltIn = false;
            recipe.IsOverridden = false;
            var validation = RecipeValidator.Validate(recipe);
            return new ParsedProposal
            {
                Recipe = recipe,
                Validation = validation,
                Content = RecipeSerializer.Serialize(recipe)
            };
        }

        /// <summary>
        /// VALIDATE_RECIPE: errors, warnings and what the user will be asked to allow; nothing is saved or shown
        /// </summary>
        public static Task HandleValidateRecipeAsync(IpcRequestContext context, CancellationToken cancellationToken = default)
        {
            var parsed = Parse(GetParameter(context, RecipeParameter));
            if (parsed.Error != null)
            {
                return ReplyErrorAsync(context, parsed.Error, cancellationToken);
            }

            var validation = parsed.Validation;
            return context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                valid = validation.IsValid,
                errors = validation.Errors,
                warnings = validation.Warnings,
                needs_permission = validation.GatedActions.Select(a => new { kind = a.GateType.ToString(), what = RecipeDescriber.DescribeGatedAction(a) }).ToList(),
                what_it_does = validation.IsValid ? RecipeDescriber.DescribeSteps(parsed.Recipe).Select(l => l.Text).ToList() : new List<string>(),
                triggers = RecipeDescriber.DescribeTriggers(parsed.Recipe).Select(t => t.Label).ToList(),
                existing_recipe = RecipeManager.Instance.GetRecipeById(parsed.Recipe.Id) != null,
                stdout = validation.IsValid ? "The recipe is valid." : $"The recipe has {validation.Errors.Count} error(s)."
            }, cancellationToken);
        }

        /// <summary>
        /// PROPOSE_RECIPE: shows the recipe in the approval window, marked as written by the AI tool. Saved only when the user approves it,
        /// with only the triggers and permissions the user switched on.
        /// </summary>
        public static async Task HandleProposeRecipeAsync(IpcRequestContext context, CancellationToken cancellationToken = default)
        {
            var client = context.AiClient;
            if (client == null)
            {
                await ReplyErrorAsync(context, AiToolAccess.NotAllowedMessage, cancellationToken).ConfigureAwait(false);
                return;
            }

            var parsed = Parse(GetParameter(context, RecipeParameter));
            if (parsed.Error != null)
            {
                await ReplyErrorAsync(context, parsed.Error, cancellationToken).ConfigureAwait(false);
                return;
            }
            if (!parsed.Validation.IsValid)
            {
                await ReplyErrorAsync(context, "The recipe is not valid, fix it first (validate_recipe):\n" + string.Join("\n", parsed.Validation.Errors), cancellationToken).ConfigureAwait(false);
                return;
            }

            var recipe = parsed.Recipe;
            string replaces = GetParameter(context, ReplacesParameter)?.Trim();
            var recipeManager = RecipeManager.Instance;
            var target = ResolveTarget(recipeManager, recipe, replaces);
            if (target.Error != null)
            {
                await ReplyErrorAsync(context, target.Error, cancellationToken).ConfigureAwait(false);
                return;
            }
            // The id may have been set to the replaced recipe's id
            parsed.Content = RecipeSerializer.Serialize(recipe);

            byte[] bytes = new UTF8Encoding(false).GetBytes(parsed.Content);
            string contentHash = RecipeTrustStore.ComputeSha256(bytes);
            string rejectionKey = $"{client.ExePath}|{contentHash}";
            bool rejectedBefore;
            lock (RejectedProposals)
            {
                rejectedBefore = RejectedProposals.Contains(rejectionKey);
            }
            if (rejectedBefore)
            {
                await ReplyErrorAsync(context, "The user already rejected this recipe. Ask the user what to change before proposing a recipe again.", cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!await ProposalLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                await ReplyErrorAsync(context, "Another recipe proposal is waiting for the user, try again after it was answered.", cancellationToken).ConfigureAwait(false);
                return;
            }
            try
            {
                var request = new RecipeApprovalRequest
                {
                    Recipe = recipe,
                    FilePath = target.FilePath,
                    Content = parsed.Content,
                    ContentHash = contentHash,
                    Validation = parsed.Validation,
                    PreviousRecord = File.Exists(target.FilePath) ? RecipeTrustStore.GetTrustRecord(target.FilePath) : null,
                    ProposedByName = client.DisplayName ?? Path.GetFileName(client.ExePath),
                    ProposedByPath = client.ExePath,
                    ProposedBySigner = client.Signer,
                    AiRequest = Truncate(GetParameter(context, RequestParameter), 2000),
                    AiExplanation = Truncate(GetParameter(context, ExplanationParameter), 4000),
                    ReplacedRecipe = target.Replaced,
                    ReplacesBuiltIn = target.ReplacesBuiltIn,
                    PreviousContent = target.Replaced != null ? RecipeSerializer.Serialize(target.Replaced) : null,
                    StartSwitchedOff = true
                };

                var decision = await ApprovalPrompt(request, cancellationToken).ConfigureAwait(false);
                if (decision?.Approval == null)
                {
                    lock (RejectedProposals)
                    {
                        RejectedProposals.Add(rejectionKey);
                    }
                    Log.InfoFormat("The user rejected the recipe '{0}' proposed by {1}.", recipe.Id, client);
                    await context.ReplyAsync(new
                    {
                        status = "ok",
                        exit_code = 0,
                        decision = "rejected",
                        recipe = recipe.Id,
                        stdout = "The user rejected the recipe, nothing was saved."
                    }, cancellationToken).ConfigureAwait(false);
                    return;
                }

                var approval = decision.Approval;
                approval.RecipeId = recipe.Id;
                approval.ReplacesBuiltIn = target.ReplacesBuiltIn;
                string error = await SaveAsync(recipeManager, target.FilePath, bytes, parsed.Content, contentHash, approval, client, recipe, cancellationToken).ConfigureAwait(false);
                if (error != null)
                {
                    await ReplyErrorAsync(context, error, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (decision.OpenInEditor)
                {
                    await UiDispatcher.Current.InvokeAsync(() =>
                    {
                        SimpleServiceProvider.Current.GetInstance<IRecipeEditorService>(isOptional: true)?.OpenEditor(recipe.Id);
                    }, cancellationToken).ConfigureAwait(false);
                }

                var triggers = RecipeDescriber.DescribeTriggers(recipe);
                await context.ReplyAsync(new
                {
                    status = "ok",
                    exit_code = 0,
                    decision = "approved",
                    recipe = recipe.Id,
                    file = target.FilePath,
                    triggers_on = triggers.Where(t => approval.IsTriggerApproved(t.Key)).Select(t => t.Label).ToList(),
                    triggers_off = triggers.Where(t => !approval.IsTriggerApproved(t.Key)).Select(t => t.Label).ToList(),
                    stdout = "The user approved the recipe, it is saved."
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ProposalLock.Release();
            }
        }

        /// <summary>
        /// Where a proposal goes and what it replaces
        /// </summary>
        internal sealed class ProposalTarget
        {
            public string FilePath { get; set; }
            public CaptureRecipe Replaced { get; set; }
            public bool ReplacesBuiltIn { get; set; }
            public string Error { get; set; }
        }

        /// <summary>
        /// A new recipe gets its own file in the AI recipe directory and needs an unused id. A change (replaces = id) keeps the
        /// recipe's file when that file holds only this recipe; a built-in recipe gets a file in the AI recipe directory.
        /// </summary>
        internal static ProposalTarget ResolveTarget(IRecipeManager recipeManager, CaptureRecipe recipe, string replaces)
        {
            if (string.IsNullOrWhiteSpace(replaces))
            {
                if (recipeManager.GetRecipeById(recipe.Id) != null)
                {
                    return new ProposalTarget { Error = $"A recipe with the id '{recipe.Id}' exists. Use another id, or update_recipe to change that recipe." };
                }
                return new ProposalTarget { FilePath = GetNewFilePath(recipe.Id) };
            }

            var existing = recipeManager.GetRecipeById(replaces);
            if (existing == null)
            {
                return new ProposalTarget { Error = $"There is no recipe with the id '{replaces}'." };
            }
            // The changed recipe keeps the id of the recipe it replaces
            recipe.Id = existing.Id;

            var builtIn = (recipeManager as RecipeManager)?.GetBuiltInRecipe(existing.Id);
            var target = new ProposalTarget { Replaced = existing, ReplacesBuiltIn = builtIn != null };
            if (string.IsNullOrEmpty(existing.FilePath))
            {
                target.FilePath = GetNewFilePath(existing.Id);
                return target;
            }

            int recipesInFile = recipeManager.GetAllRecipes().Count(r => string.Equals(r.FilePath, existing.FilePath, StringComparison.OrdinalIgnoreCase));
            if (recipesInFile > 1)
            {
                return new ProposalTarget { Error = $"The recipe '{existing.Id}' is in a file with other recipes ({existing.FilePath}), change it in the recipe editor." };
            }
            target.FilePath = existing.FilePath;
            return target;
        }

        private static string GetNewFilePath(string recipeId)
        {
            string directory = AiRecipeDirectoryProvider();
            var name = new StringBuilder();
            foreach (char c in recipeId ?? "recipe")
            {
                name.Append(Path.GetInvalidFileNameChars().Contains(c) || c == '.' ? '_' : c);
            }
            string baseName = name.Length == 0 ? "recipe" : name.ToString();
            string path = Path.Combine(directory, baseName + RecipeSerializer.RecipeFileExtension);
            for (int i = 2; File.Exists(path); i++)
            {
                path = Path.Combine(directory, $"{baseName}-{i}{RecipeSerializer.RecipeFileExtension}");
            }
            return path;
        }

        private static string GetDefaultAiRecipeDirectory()
        {
            string configDirectory = null;
            try
            {
                string configLocation = GreenshotEnvironment.ConfigLocation;
                configDirectory = string.IsNullOrEmpty(configLocation) ? null : Path.GetDirectoryName(configLocation);
            }
            catch (Exception ex)
            {
                Log.Debug("Could not get the directory of greenshot.ini.", ex);
            }
            if (string.IsNullOrEmpty(configDirectory))
            {
                configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Greenshot");
            }
            return Path.Combine(configDirectory, "Recipes", "AI");
        }

        /// <summary>
        /// Writes the approved bytes, records the approval for exactly them, and loads the recipe (on the UI thread, which registers the hotkeys)
        /// </summary>
        private static async Task<string> SaveAsync(RecipeManager recipeManager, string filePath, byte[] bytes, string content, string contentHash, RecipeApproval approval,
            AiToolClient client, CaptureRecipe recipe, CancellationToken cancellationToken)
        {
            // Recorded before writing, so the change on disk is known as Greenshot's own and doesn't ask again
            RecipeTrustStore.RecordApproval(filePath, contentHash, approval, content, RecipeTrustRecord.AiOriginPrefix + (client.DisplayName ?? client.ExePath), recipe.Name, recipe.Version);
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllBytes(filePath, bytes);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not save the recipe '{recipe.Id}' to '{filePath}'.", ex);
                return $"The user approved the recipe, but Greenshot could not save it: {ex.Message}";
            }

            Log.InfoFormat("Saved the recipe '{0}' proposed by {1} to '{2}'.", recipe.Id, client, filePath);

            // Loads what is on disk now: when the file changed since it was written, the hash doesn't match and nothing is loaded
            var result = await UiDispatcher.Current.InvokeAsync(() => recipeManager.LoadRecipeFromFile(filePath, interactiveApproval: false), cancellationToken).ConfigureAwait(false);
            if (!result.IsValid)
            {
                return $"The recipe was saved, but could not be loaded: {string.Join("; ", result.Errors)}";
            }
            return null;
        }

        private static Task<RecipeApprovalWindow.ApprovalResult> ShowApprovalAsync(RecipeApprovalRequest request, CancellationToken cancellationToken)
        {
            return UiDispatcher.Current.InvokeAsync(() => RecipeManager.Instance.RequestInteractiveApprovalWithOptions(request), cancellationToken);
        }

        /// <summary>
        /// Forgets the rejected proposals (tests)
        /// </summary>
        internal static void ResetRejectedProposals()
        {
            lock (RejectedProposals)
            {
                RejectedProposals.Clear();
            }
        }

        private static string GetParameter(IpcRequestContext context, string name)
        {
            var parameters = context.Envelope?.Parameters;
            if (parameters == null)
            {
                return null;
            }
            foreach (var pair in parameters)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }
            return null;
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }
            return text.Substring(0, maxLength) + "…";
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
