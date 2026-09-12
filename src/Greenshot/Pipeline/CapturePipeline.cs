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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Dapplo.Windows.Kernel32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Editor.Drawing;
using Greenshot.Native;
using Greenshot.Pipeline.Steps;
using log4net;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// Core pipeline engine orchestrating DAG capture flows from trigger through Directed Acyclic Graph execution.
    /// Supports asynchronous execution, branch splitting (fork), join synchronization (merge), and dynamic expression resolution.
    /// </summary>
    public class CapturePipeline : ICapturePipeline
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CapturePipeline));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        private readonly IInteractiveCaptureSelector _selector;
        private readonly IDestinationDispatcher _dispatcher;
        private readonly IStepRegistry _stepRegistry;
        private readonly DagExecutionEngine _dagEngine;

        private static CapturePipeline _instance;
        public static CapturePipeline Instance => _instance ??= new CapturePipeline();

        static CapturePipeline()
        {
            // Wire surface instantiation so Greenshot.Base does not need a reference to Greenshot.Editor
            CapturePayload.DefaultSurfaceFactory = capture =>
            {
                bool outputMade = capture.CaptureDetails?.CaptureMode == CaptureMode.File ||
                                  capture.CaptureDetails?.CaptureMode == CaptureMode.Clipboard;
                return new Surface(capture)
                {
                    Modified = !outputMade
                };
            };

            // Wire custom window capture handler for WindowsGraphicsCapture beta tester mode
            WindowCaptureHelper.CustomWindowCaptureHandler = handle => WindowsGraphicsCaptureInterop.CaptureWindowToBitmap(handle);
        }

        public CapturePipeline(
            IInteractiveCaptureSelector selector = null,
            IDestinationDispatcher dispatcher = null,
            IStepRegistry stepRegistry = null)
        {
            _selector = selector ?? new InteractiveCaptureSelector();
            _dispatcher = dispatcher ?? new DestinationDispatcher();
            _stepRegistry = stepRegistry ?? StepRegistry.Instance;

            RegisterBuiltInStepFactories();

            _dagEngine = new DagExecutionEngine(config => _stepRegistry.CreateStep(config));
        }

        private void RegisterBuiltInStepFactories()
        {
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Source, config => new SourceAcquisitionStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.InteractiveSelection, config => new InteractiveSelectionStep(config, _selector));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Border, config => new EffectCaptureStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Effect, config => new EffectCaptureStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Drawable, config => new DrawableStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.SetVariable, config => new SetVariableStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.ImmediateFeedback, config => new ImmediateFeedbackStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Processors, config => new ProcessorExecutionStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Destinations, config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.SaveFile, config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory("SaveToFile", config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Clipboard, config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Editor, config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Printer, config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Email, config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.CustomDestination, config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Notification, config => new NotificationStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.TextEffect, config => new TextEffectStep(config));
            _stepRegistry.RegisterStepFactory("ObfuscateText", config => new TextEffectStep(config));
        }

        public async Task<CaptureFlowContext> ExecuteAsync(
            CaptureRecipe recipe,
            ITrigger trigger = null,
            Action<CaptureFlowContext> configureContext = null,
            CancellationToken cancellationToken = default)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));

            // Verify external recipe integrity before executing
            if (!string.IsNullOrEmpty(recipe.FilePath))
            {
                var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true);
                if (recipeManager != null)
                {
                    var verifiedRecipe = recipeManager.EnsureRecipeApprovedAndUpToDate(recipe);
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

            var context = new CaptureFlowContext(recipe, trigger, cancellationToken);
            configureContext?.Invoke(context);

            try
            {
                int nodeCount = recipe.Nodes?.Count ?? 0;
                Log.InfoFormat("Starting DAG capture flow: '{0}' ({1} node(s))", recipe.Name, nodeCount);
                context.LogStep($"Starting DAG flow '{recipe.Name}' with {nodeCount} configured node(s)");

                // WindowsGraphicsCapture beta tester hook
                if (CoreConfig.IsBetaTester)
                {
                    CaptureHandler.CaptureScreenRectangle = WindowsGraphicsCaptureInterop.CaptureRectangle;
                }

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
                // Clean up working set if enabled
                if (CoreConfig.MinimizeWorkingSetSize)
                {
                    PsApi.EmptyWorkingSet();
                }

                // Dispose context (cleans up raw capture and surface unless editor retained it)
                context.Dispose();
            }

            return context;
        }
    }
}
