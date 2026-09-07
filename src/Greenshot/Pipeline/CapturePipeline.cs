/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Configuration;
using Greenshot.Editor.Drawing;
using Greenshot.Native;
using Greenshot.Pipeline.Steps;
using log4net;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// Core pipeline engine orchestrating capture flows from trigger through ordered modular steps.
    /// Eliminates rigid hardcoded stages in favor of dynamic step iteration.
    /// </summary>
    public class CapturePipeline : ICapturePipeline
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CapturePipeline));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        private readonly IInteractiveCaptureSelector _selector;
        private readonly IDestinationDispatcher _dispatcher;
        private readonly IStepRegistry _stepRegistry;

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
        }

        private void RegisterBuiltInStepFactories()
        {
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Source, config => new SourceAcquisitionStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.InteractiveSelection, config => new InteractiveSelectionStep(config, _selector));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Border, config => new EffectCaptureStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Effect, config => new EffectCaptureStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.ImmediateFeedback, config => new ImmediateFeedbackStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Processors, config => new ProcessorExecutionStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Destinations, config => new DestinationExportStep(config, _dispatcher));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Notification, config => new NotificationStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.TextEffect, config => new TextEffectStep(config));
            _stepRegistry.RegisterStepFactory("ObfuscateText", config => new TextEffectStep(config));
            _stepRegistry.RegisterStepFactory(WellKnownStepTypes.Conditional, config =>
            {
                var cond = config.GetParameter<IStepCondition>("Condition");
                var thenConfigs = config.GetParameter<List<RecipeStepConfig>>("ThenSteps");
                var elseConfigs = config.GetParameter<List<RecipeStepConfig>>("ElseSteps");

                var thenSteps = thenConfigs != null
                    ? thenConfigs.Select(c => _stepRegistry.CreateStep(c)).Where(s => s != null)
                    : Enumerable.Empty<ICaptureStep>();

                var elseSteps = elseConfigs != null
                    ? elseConfigs.Select(c => _stepRegistry.CreateStep(c)).Where(s => s != null)
                    : Enumerable.Empty<ICaptureStep>();

                return new ConditionalCaptureStep(config.Name, cond, thenSteps, elseSteps);
            });
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
                int stepCount = recipe.Steps?.Count ?? 0;
                Log.InfoFormat("Starting capture flow: '{0}' ({1} step(s))", recipe.Name, stepCount);
                context.LogStep($"Starting flow '{recipe.Name}' with {stepCount} configured step(s)");

                // WindowsGraphicsCapture beta tester hook
                if (CoreConfig.IsBetaTester)
                {
                    CaptureHandler.CaptureScreenRectangle = WindowsGraphicsCaptureInterop.CaptureRectangle;
                }

                if (recipe.Steps != null)
                {
                    foreach (var stepConfig in recipe.Steps)
                    {
                        if (!stepConfig.Enabled)
                        {
                            context.LogStep($"Skipping disabled step: {stepConfig.Name} [{stepConfig.StepType}]");
                            continue;
                        }

                        if (context.IsAborted || cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var step = _stepRegistry.CreateStep(stepConfig);
                        if (step == null)
                        {
                            Log.WarnFormat("Could not resolve executable step for type '{0}' ({1})", stepConfig.StepType, stepConfig.Name);
                            context.LogStep($"Warning: unresolved step type '{stepConfig.StepType}'");
                            continue;
                        }

                        context.LogStep($"Executing step: {step.Name}");
                        Log.InfoFormat("Executing pipeline step: '{0}' [{1}]", step.Name, stepConfig.StepType);
                        await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                        Log.InfoFormat("Finished pipeline step: '{0}' [{1}]", step.Name, stepConfig.StepType);
                    }
                }

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
