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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Contracts = Greenshot.Base.Recipes.Contracts;

using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Sources;
using Greenshot.Capturing;
using log4net;
using Greenshot.Base.Threading;
#if !GREENSHOT_LIGHT
using Greenshot.Ai;
#endif

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Pipeline step responsible for pre-capture preparation (delay, tray reset),
    /// raw pixel acquisition from screen, window, file, or clipboard, and pixel DPI alignment.
    /// Evaluates configuration settings (e.g. mouse cursor, delay) dynamically at runtime.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Source, "Acquire Source", "Acquires the image: captures the screen, a region, a window, the clipboard, a file or the current editor. A capture handed to the flow (forwarded from another recipe, imported from the browser extension) is used instead of capturing.", "Acquisition")]
    [StepPayload(RawCapture = PayloadRequirement.Created, Surface = PayloadRequirement.Created)]
    [StepParameter("SourceType", ContractDataType.Enum, DefaultValue = "Region", SupportsExpressions = false, Description = "What to capture", AllowedValues = new[] { "Region", "Window", "ActiveWindow", "FullScreen", "LastRegion", "Clipboard", "File", "TextOcr", "CurrentEditor", "Extension" })]
    [StepParameter("Filename", ContractDataType.FilePath, Description = "File to load (SourceType File); default: variable Filename")]
    [StepParameter("CaptureMouseCursor", ContractDataType.Boolean, Description = "Include the mouse cursor (default: settings)")]
    [StepParameter("DelayMs", ContractDataType.Integer, Description = "Delay before capturing in milliseconds (default: settings)")]
    [StepParameter("ScreenCaptureMode", ContractDataType.Enum, Description = "Which screen(s) to capture for FullScreen (default: settings)", AllowedValues = new[] { "Auto", "FullScreen", "Fixed" })]
    [StepParameter("WindowTitle", ContractDataType.String, Description = "Capture the window with this title (SourceType Window)")]
    [StepParameter("WindowTitlePattern", ContractDataType.String, Description = "Capture the window whose title matches this regular expression (SourceType Window)")]
    [StepParameter("ProcessName", ContractDataType.String, Description = "Capture the window of this process (SourceType Window)")]
    [StepParameter("WindowHandle", ContractDataType.Object, Description = "Capture this window without asking or activating it (SourceType Window): the variable of a Window argument (e.g. ${Window}) or a window handle")]
    [StepParameter("MatchCase", ContractDataType.Boolean, Description = "Match WindowTitle / WindowTitlePattern case-sensitively")]
    [StepParameter("AlignDpi", ContractDataType.Boolean, DefaultValue = true, Description = "Set the image resolution to the screen DPI")]
    [StepInputVariable("PreSuppliedRegion", ContractDataType.Object, Description = "Region to capture without asking (set by the caller)")]
    [StepInputVariable("CaptureDelay", ContractDataType.Integer, Description = "Overrides DelayMs")]
    [StepInputVariable("CaptureMouseCursor", ContractDataType.Boolean, Description = "Overrides the CaptureMouseCursor parameter")]
    [StepInputVariable("ScreenCaptureMode", ContractDataType.Enum, Description = "Overrides the ScreenCaptureMode parameter")]
    [StepInputVariable("TargetWindow", ContractDataType.Object, Description = "Window to capture (set by the caller)")]
    [StepInputVariable("WindowTitle", ContractDataType.String, Description = "Window title, when the parameter is not set")]
    [StepInputVariable("WindowTitlePattern", ContractDataType.String, Description = "Window title pattern, when the parameter is not set")]
    [StepInputVariable("ProcessName", ContractDataType.String, Description = "Process name, when the parameter is not set")]
    [StepInputVariable("MatchCase", ContractDataType.Boolean, Description = "Match case, when the parameter is not set")]
    [StepInputVariable("Filename", ContractDataType.FilePath, Description = "File to load (SourceType File), when the parameter is not set")]
    [StepInputVariable("EditorForm", ContractDataType.Object, Description = "The editor to take the image from (SourceType CurrentEditor, set by the editor trigger)")]
    [StepOutputVariable("SelectedWindow", ContractDataType.Object, "The window of the last region (SourceType LastRegion)", Conditional = true)]
    public class SourceAcquisitionStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SourceAcquisitionStep));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        /// <summary>
        /// Loading a file (SourceType File) reads from the file system: the user has to allow file system access
        /// </summary>
        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            if (string.Equals(Config.GetParameter<string>("SourceType"), "File", StringComparison.OrdinalIgnoreCase))
            {
                string filename = Config.GetParameter<string>("Filename");
                yield return new RecipeGatedAction(RecipeGateType.FileSystemAccess,
                    string.IsNullOrWhiteSpace(filename) ? "Reads the image file named in the variable Filename" : $"Reads the image file {filename}");
            }
        }

        private readonly ICaptureWindowPreparer _windowPreparer;

        /// <param name="config">RecipeNodeConfig</param>
        /// <param name="windowPreparer">Opens the window of the interactive selection while the screen is captured, optional</param>
        public SourceAcquisitionStep(RecipeNodeConfig config, ICaptureWindowPreparer windowPreparer = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "SourceAcquisitionStep";
            _windowPreparer = windowPreparer;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            // 0. Check if payload is already pre-supplied (e.g. browser extension capture or programmatic injection)
            if (context.Payload != null)
            {
                Log.Info($"Source {Name} using pre-supplied capture payload.");
                context.IsPayloadPreSupplied = true;
                if (Config.GetParameter("AlignDpi", true))
                {
                    AlignDpi(context.Payload);
                }
                return;
            }

            // 1. Pre-capture preparation: tray icon reset & delay
            await PreparePreCaptureAsync(context, cancellationToken).ConfigureAwait(false);
            if (context.IsAborted || cancellationToken.IsCancellationRequested) return;

            // 2. Resolve mouse capture setting dynamically
            bool captureMouse = ResolveCaptureMouse(context);

            // 3. Resolve source type & DPI alignment setting
            CaptureSourceType sourceType = Config.GetParameter("SourceType", CaptureSourceType.Region);
            bool alignDpi = Config.GetParameter("AlignDpi", true);

            // 4. Handle pre-supplied region (e.g. command-line or programmatic region capture)
            if (context.Properties.TryGetValue("PreSuppliedRegion", out var regionObj) &&
                regionObj is NativeRect preRect && !preRect.IsEmpty)
            {
                var composite = CreateScreenWithCursorSource(captureMouse, "PreSuppliedRegionSource");
                var payload = await composite.AcquireAsync(context, cancellationToken).ConfigureAwait(false);

                if (payload?.RawCapture != null )
                {
#if !GREENSHOT_LIGHT
                    RedactForAiTool(context, payload);
#endif
                    // Offset to bitmap coordinates for cropping
                    NativeRect screenOffsetRect = preRect.Offset(-payload.RawCapture.Location.X, -payload.RawCapture.Location.Y);
                    payload.RawCapture.Crop(screenOffsetRect);
                }
                if (alignDpi && payload != null)
                {
                    AlignDpi(payload);
                }
                context.Payload = payload;
                return;
            }

            // A given window (WindowHandle): captured directly, with its exact contents, without activating it
            if (sourceType == CaptureSourceType.Window && Config.GetParameter<object>("WindowHandle") is { } windowHandle)
            {
#if GREENSHOT_LIGHT
                // The window of an AI tool's Window argument: Greenshot Light has no AI tools
                context.Fail($"Capturing a given window ({windowHandle}) is not available in Greenshot Light.");
#else
                await AcquireWindowAsync(context, windowHandle, alignDpi, cancellationToken).ConfigureAwait(false);
#endif
                return;
            }

            // Check if window targeting parameters are specified in config
            bool hasTargetWindowConfig = !string.IsNullOrEmpty(Config.GetParameter<string>("WindowTitle")) ||
                                         !string.IsNullOrEmpty(Config.GetParameter<string>("WindowTitlePattern")) ||
                                         !string.IsNullOrEmpty(Config.GetParameter<string>("ProcessName"));

            // 5. Instantiate source based on SourceType
            ICaptureSource source = sourceType switch
            {
                CaptureSourceType.Window when hasTargetWindowConfig =>
                    captureMouse
                        ? new CompositeCaptureSource("TargetWindowWithCursor", new ICaptureSource[] { new ActiveWindowCaptureSource(Config), new CursorCaptureSource() })
                        : new ActiveWindowCaptureSource(Config),

                CaptureSourceType.Region or CaptureSourceType.Window or CaptureSourceType.TextOcr =>
                    CreateScreenWithCursorSource(captureMouse, "InteractiveBaseSource", ScreenCaptureMode.FullScreen),

                CaptureSourceType.FullScreen =>
                    CreateScreenWithCursorSource(captureMouse, "FullScreenSource", ResolveScreenCaptureMode(context)),

                CaptureSourceType.ActiveWindow =>
                    captureMouse
                        ? new CompositeCaptureSource("ActiveWindowWithCursor", new ICaptureSource[] { new ActiveWindowCaptureSource(Config), new CursorCaptureSource() })
                        : new ActiveWindowCaptureSource(Config),

                CaptureSourceType.LastRegion =>
                    captureMouse
                        ? new CompositeCaptureSource("LastRegionWithCursor", new ICaptureSource[] { new LastRegionCaptureSource(), new CursorCaptureSource() })
                        : new LastRegionCaptureSource(),

                CaptureSourceType.Clipboard =>
                    new ClipboardCaptureSource(),

                CaptureSourceType.File =>
                    new FileCaptureSource(Config),

                CaptureSourceType.CurrentEditor =>
                    new CurrentEditorCaptureSource(),

                CaptureSourceType.Extension =>
                    null,

                _ => null
            };

            if (source == null)
            {
                if (sourceType == CaptureSourceType.Extension)
                {
                    context.Fail("Extension capture source requires a pre-supplied image payload.");
                }
                else
                {
                    context.Fail($"Unsupported capture source type: {sourceType}");
                }
                return;
            }

            // The window of the selection is built while the screen is captured
            if (_windowPreparer != null && sourceType is CaptureSourceType.Region or CaptureSourceType.Window or CaptureSourceType.TextOcr &&
                !hasTargetWindowConfig && HasInteractiveSelection(context))
            {
                _windowPreparer.PrepareWindow(InteractiveSelectionStep.GetSnapWindowsAsync(cancellationToken));
            }

            var acquired = await source.AcquireAsync(context, cancellationToken).ConfigureAwait(false);
            if (acquired != null)
            {
#if !GREENSHOT_LIGHT
                // Clipboard, file and editor contents are the user's, everything else is taken from the screen
                if (sourceType != CaptureSourceType.Clipboard && sourceType != CaptureSourceType.File && sourceType != CaptureSourceType.CurrentEditor)
                {
                    RedactForAiTool(context, acquired);
                }
#endif
                // Align DPI for raw captured pixels (screen, window, active window, region, last region)
                if (alignDpi && sourceType != CaptureSourceType.File)
                {
                    AlignDpi(acquired);
                }
                context.Payload = acquired;
            }
            else if (!context.IsAborted)
            {
                context.Abort("Acquisition failed or produced no payload.");
            }
        }

#if !GREENSHOT_LIGHT
        /// <summary>
        /// Captures the window of the WindowHandle parameter. For AI tools windows of excluded processes are refused.
        /// </summary>
        private async Task AcquireWindowAsync(CaptureFlowContext context, object windowHandle, bool alignDpi, CancellationToken cancellationToken)
        {
            var window = AiToolCapture.ResolveWindow(windowHandle);
            if (window == null)
            {
                context.Fail($"The window to capture ({windowHandle}) doesn't exist (anymore).");
                return;
            }
            if (AiToolCapture.IsAiToolRun(context))
            {
                string processName = AiToolCapture.GetProcessName(window);
                if (AiToolAccess.IsProcessExcluded(processName))
                {
                    context.Fail($"Windows of '{processName}' are excluded from AI tools.");
                    return;
                }
            }

            var capture = await AiToolCapture.CaptureWindowAsync(window, cancellationToken).ConfigureAwait(false);
            if (capture?.Image == null)
            {
                capture?.Dispose();
                context.Fail($"Capturing the window '{window.Text}' failed.");
                return;
            }

            var payload = new CapturePayload(capture);
            // A capture of the window's area of the screen can contain what covers the window
            if (!AiToolCapture.IsWindowContentOnly(capture))
            {
                RedactForAiTool(context, payload);
            }
            if (alignDpi)
            {
                AlignDpi(payload);
            }
            context.Payload = payload;
        }

        /// <summary>
        /// AI tools never see windows of excluded processes (password managers by default): they are blacked out in screen captures.
        /// </summary>
        private static void RedactForAiTool(CaptureFlowContext context, ICapturePayload payload)
        {
            if (!AiToolCapture.IsAiToolRun(context) || payload?.RawCapture?.Image == null)
            {
                return;
            }
            int redacted = AiToolCapture.RedactExcludedWindows(payload.RawCapture.Image, payload.RawCapture.Location);
            if (redacted > 0)
            {
                context.LogStep($"Blacked out {redacted} window(s) of applications excluded from AI tools.");
            }
        }
#endif

        private static void AlignDpi(ICapturePayload payload)
        {
            if (payload?.RawCapture == null) return;

            float dpiX = 96f;
            float dpiY = 96f;
            try
            {
                using (Graphics graphics = Graphics.FromHwnd(User32Api.GetDesktopWindow()))
                {
                    dpiX = graphics.DpiX;
                    dpiY = graphics.DpiY;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to retrieve screen DPI via desktop window, falling back to 96 DPI", ex);
            }

            if (payload.RawCapture.CaptureDetails != null)
            {
                payload.RawCapture.CaptureDetails.DpiX = dpiX;
                payload.RawCapture.CaptureDetails.DpiY = dpiY;
            }

            if (payload.RawCapture.Image is Bitmap bmp)
            {
                bmp.SetResolution(dpiX, dpiY);
            }
        }

        private static ICaptureSource CreateScreenWithCursorSource(bool captureMouse, string name, ScreenCaptureMode mode = ScreenCaptureMode.FullScreen)
        {
            if (captureMouse)
            {
                return new CompositeCaptureSource(name, new ICaptureSource[]
                {
                    new ScreenCaptureSource(mode),
                    new CursorCaptureSource()
                });
            }
            return new ScreenCaptureSource(mode);
        }

        private ScreenCaptureMode ResolveScreenCaptureMode(CaptureFlowContext context)
        {
            // Priority 1: Runtime context property (e.g. from context menu dropdown "All" or caller)
            if (context.Properties.TryGetValue("ScreenCaptureMode", out var scmObj) && scmObj is ScreenCaptureMode scm)
            {
                return scm;
            }

            // Priority 2: Recipe step configuration parameter
            var stepMode = Config.GetParameter<string>("ScreenCaptureMode");
            if (!string.IsNullOrEmpty(stepMode) && Enum.TryParse<ScreenCaptureMode>(stepMode, ignoreCase: true, out var parsedMode))
            {
                return parsedMode;
            }

            // Priority 3: User configuration
            return CoreConfig.ScreenCaptureMode;
        }

        private bool ResolveCaptureMouse(CaptureFlowContext context)
        {
            // Priority 1: Runtime context property (e.g. from trigger parameter or CaptureHelper caller)
            if (context.Properties.TryGetValue("CaptureMouseCursor", out var ctxVal) && ctxVal is bool ctxBool)
            {
                return ctxBool;
            }

            // Priority 2: Explicit parameter pre-defined on the recipe step
            if (Config.Parameters.TryGetValue("CaptureMouseCursor", out var stepVal) && stepVal != null)
            {
                if (stepVal is bool stepBool) return stepBool;
                if (bool.TryParse(stepVal.ToString(), out bool parsedBool)) return parsedBool;
            }

            // Priority 3: Dynamic evaluation of user configuration at runtime
            return CoreConfig.CaptureMousepointer;
        }

        /// <summary>
        /// The default of ICoreConfiguration.CaptureDelay
        /// </summary>
        private const int DefaultCaptureDelay = 100;

        private static bool HasInteractiveSelection(CaptureFlowContext context) =>
            context.Recipe?.Nodes?.Any(node => string.Equals(node?.StepType, WellKnownStepTypes.InteractiveSelection, StringComparison.OrdinalIgnoreCase)) == true;

        private static bool IsHotkeyTrigger(CaptureFlowContext context) =>
            string.Equals(context.Trigger?.TriggerType, Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase);

        private async Task PreparePreCaptureAsync(CaptureFlowContext context, CancellationToken ct)
        {
            // Dismiss lingering tray balloons
            bool isTerminalServer = !CoreConfig.DisableRDPOptimizing && (CoreConfig.OptimizeForRDP || SystemInformation.TerminalServerSession);
            if (!CoreConfig.HideTrayicon && !isTerminalServer)
            {
                var notifyIcon = SimpleServiceProvider.Current.GetInstance<NotifyIcon>(isOptional: true);
                if (notifyIcon != null)
                {
                    // The tray icon belongs to the UI thread; awaited, so the balloon is gone before the capture
                    await context.Ui.RunOnUiAsync(() =>
                    {
                        try
                        {
                            notifyIcon.Visible = false;
                            notifyIcon.Visible = true;
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("Failed to toggle notifyIcon visibility", ex);
                        }
                    }, ct).ConfigureAwait(false);
                }
            }

            // Resolve capture delay: Context -> Step config -> CoreConfig
            int delay = -1;
            if (context.Properties.TryGetValue("CaptureDelay", out var ctxDelay) && ctxDelay is int cd)
            {
                delay = cd;
            }
            else if (Config.Parameters.TryGetValue("DelayMs", out var stepDelay) && stepDelay != null)
            {
                if (stepDelay is int sd) delay = sd;
                else if (int.TryParse(stepDelay.ToString(), out int parsedDelay)) delay = parsedDelay;
            }

            if (delay < 0)
            {
                delay = CoreConfig.CaptureDelay;
                // The default delay lets a tray or context menu close before the capture; a hotkey opens no menu, so it only made
                // the capture slower. A delay the user configured is kept.
                if (delay == DefaultCaptureDelay && IsHotkeyTrigger(context))
                {
                    delay = 0;
                }
            }

            if (delay > 0)
            {
                context.LogStep($"Waiting pre-capture delay: {delay}ms");
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }
}
