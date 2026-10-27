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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Triggers;
using Greenshot.Linux.Recipes.Steps;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// Core pipeline engine orchestrating DAG capture flows from trigger through Directed Acyclic Graph execution.
    /// Supports asynchronous execution, branch splitting (fork), join synchronization (merge), and dynamic expression resolution.
    /// </summary>
    public class CapturePipeline : ICapturePipeline
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CapturePipeline));

        private readonly IDestinationDispatcher _dispatcher;
        private readonly IStepRegistry _stepRegistry;
        private readonly DagExecutionEngine _dagEngine;

        // Thread-safe: the first access can come from the UI thread and an IPC or pipeline thread at the same time,
        // and a second instance would silently lose what was registered in the first one.
        private static readonly Lazy<CapturePipeline> LazyInstance = new Lazy<CapturePipeline>(() => new CapturePipeline(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static CapturePipeline Instance => LazyInstance.Value;

        /// <summary>
        /// The interactive selector used by this pipeline, the flow runner asks it whether a selection is open.
        /// </summary>

        public CapturePipeline(
            IDestinationDispatcher dispatcher = null,
            IStepRegistry stepRegistry = null)
        {
            _dispatcher = dispatcher ?? new DestinationDispatcher();
            _stepRegistry = stepRegistry ?? StepRegistry.Instance;

            RegisterBuiltInStepFactories();

            _dagEngine = new DagExecutionEngine(config => _stepRegistry.CreateStep(config), _stepRegistry.GetContract);
        }

        private void RegisterBuiltInStepFactories()
        {
            // Step types are registered with their contract (from the step class' attributes).
            // Classes that implement several step types get a contract per step type.
            _stepRegistry.Register<SourceAcquisitionStep>(config => new SourceAcquisitionStep(config));
            _stepRegistry.Register<InteractiveSelectionStep>(config => new InteractiveSelectionStep(config));
            _stepRegistry.Register<EffectCaptureStep>(config => new EffectCaptureStep(config));
            _stepRegistry.Register<AnnotationStep>(config => new AnnotationStep(config));
            AnnotationStep.EnsureBuiltInDrawablesRegistered();
            _stepRegistry.Register<SetVariableStep>(config => new SetVariableStep(config));
            _stepRegistry.Register<ConditionalStep>(config => new ConditionalStep(config));
            _stepRegistry.Register<ImmediateFeedbackStep>(config => new ImmediateFeedbackStep(config));
            _stepRegistry.Register<OcrStep>(config => new OcrStep(config));
            _stepRegistry.Register<DestinationExportStep>(config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.Register<DestinationExportStep>(WellKnownStepTypes.SaveFile, "Save to File", "Saves the capture to a file without asking.",
                config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.Register<DestinationExportStep>(WellKnownStepTypes.Clipboard, "Copy to Clipboard", "Copies the image and/or the extracted text to the clipboard.",
                config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.Register<DestinationExportStep>(WellKnownStepTypes.Editor, "Open in Editor", "Opens the capture in the Greenshot editor.",
                config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.Register<DestinationExportStep>(WellKnownStepTypes.Printer, "Printer", "Prints the capture.",
                config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.Register<DestinationExportStep>(WellKnownStepTypes.Email, "Send by Email", "Attaches the capture to a new email.",
                config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.Register<DestinationExportStep>(WellKnownStepTypes.CustomDestination, "Custom Destination", "Exports to the destination named by CustomDestinationId.",
                config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.Register<NotificationStep>(config => new NotificationStep(config));
            _stepRegistry.Register<TextEffectStep>(config => new TextEffectStep(config));
            _stepRegistry.Register<UserPromptStep>(config => new UserPromptStep(config));
            _stepRegistry.Register<DynamicDestinationStep>(config => new DynamicDestinationStep(config));
            _stepRegistry.Register<RecordVideoRecipeStep>(config => new RecordVideoRecipeStep(config));
            _stepRegistry.Register<StdoutStep>(config => new StdoutStep(config));
            _stepRegistry.Register<StderrStep>(config => new StderrStep(config));
            _stepRegistry.Register<SlotStep>(config => new SlotStep(config));

            // The steps of the plugins (external command, uploads, ...)
            _stepRegistry.RegisterProviders(RecipeServices.StepProviders);
        }

        public async Task<CaptureFlowContext> ExecuteAsync(
            CaptureRecipe recipe,
            ITrigger trigger = null,
            Action<CaptureFlowContext> configureContext = null,
            CancellationToken cancellationToken = default)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));

            if (!recipe.IsEnabled)
            {
                Log.WarnFormat("Execution aborted for recipe '{0}' because it is deactivated.", recipe.Name);
                var abortedContext = new CaptureFlowContext(recipe, trigger, cancellationToken);
                abortedContext.Abort($"Recipe '{recipe.Name}' is deactivated.");
                return abortedContext;
            }

            var recipeManager = RecipeServices.Manager;

            // Verify external recipe integrity before executing
            if (!string.IsNullOrEmpty(recipe.FilePath))
            {
                if (recipeManager != null)
                {
                    var verifiedRecipe = await recipeManager.EnsureRecipeApprovedAndUpToDateAsync(recipe, cancellationToken).ConfigureAwait(false);
                    if (verifiedRecipe == null)
                    {
                        Log.WarnFormat("Execution aborted for recipe '{0}' because approval was denied or file verification failed.", recipe.Name);
                        var abortedContext = new CaptureFlowContext(recipe, trigger, cancellationToken);
                        abortedContext.Abort("Recipe execution aborted: approval denied or file verification failed.");
                        return abortedContext;
                    }
                    recipe = verifiedRecipe;
                }
            }

            // The recipe runs with the switched on extensions in its slots (border, drop shadow, caption, ...)
            if (recipeManager != null)
            {
                var effective = recipeManager.GetEffectiveRecipe(recipe);
                if (effective != null && !ReferenceEquals(effective, recipe))
                {
                    Log.InfoFormat("Recipe '{0}' runs with the extension(s) {1}.", recipe.Name, string.Join(", ", effective.AppliedExtensions.Select(e => e.Name ?? e.Id)));
                    recipe = effective;
                }
            }

            var context = new CaptureFlowContext(recipe, trigger, cancellationToken);
            configureContext?.Invoke(context);

            try
            {
                int nodeCount = recipe.Nodes?.Count ?? 0;
                Log.InfoFormat("Starting DAG capture flow: '{0}' ({1} node(s))", recipe.Name, nodeCount);
                context.LogStep($"Starting DAG flow '{recipe.Name}' with {nodeCount} configured node(s)");

                await _dagEngine.ExecuteAsync(recipe, context, cancellationToken).ConfigureAwait(false);

                if (!context.IsAborted)
                {
                    context.State = CaptureFlowState.Completed;
                    context.LogStep("Capture flow completed successfully.");
                    Log.InfoFormat("Capture flow completed successfully: '{0}'", recipe.Name);
                }

            }
            catch (OperationCanceledException)
            {
                context.Abort("Flow was cancelled.");
            }
            catch (Exception ex)
            {
                Log.Error("Capture flow failed with unhandled exception", ex);
                context.Fail("Capture flow failed", ex);
            }
            finally
            {
                // A failed flow didn't get to its Notification step: what it exported is shown with the failure, while the export
                // sources still exist (the kept capture of the toast comes from them). CaptureFlowRunner then shows no other message.
                if (context.State == CaptureFlowState.Failed)
                {
                    try
                    {
                        string flowError = context.Error?.Message ?? context.AbortReason;
                        if (await ExportNotification.ShowAsync(context, flowError, CancellationToken.None).ConfigureAwait(false))
                        {
                            context.Properties[ExportNotification.FailureShownProperty] = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("The notification of the failed flow failed", ex);
                    }
                }

                // The caller's last look at the result, while the payload still exists
                if (context.FlowFinishedAsync != null)
                {
                    try
                    {
                        await context.FlowFinishedAsync(context).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("The flow finished callback failed", ex);
                    }
                }

                // Dispose context (cleans up raw capture and surface unless editor retained it)
                context.Dispose();
            }

            return context;
        }
    }
}
