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
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Sources;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step responsible for pre-capture preparation (delay, tray reset),
    /// raw pixel acquisition from screen, window, file, or clipboard, and pixel DPI alignment.
    /// Evaluates configuration settings (e.g. mouse cursor, delay) dynamically at runtime.
    /// </summary>
    public class SourceAcquisitionStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SourceAcquisitionStep));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public string Name { get; }
        public RecipeStepConfig Config { get; }

        public SourceAcquisitionStep(RecipeStepConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "SourceAcquisitionStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            context.State = CaptureFlowState.Acquiring;

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
                payload?.RawCapture?.Crop(preRect);
                if (alignDpi && payload != null)
                {
                    AlignDpi(payload);
                }
                context.Payload = payload;
                return;
            }

            // Check if window targeting parameters are specified in config
            bool hasTargetWindowConfig = !string.IsNullOrEmpty(Config.GetParameter<string>("WindowTitle")) ||
                                         !string.IsNullOrEmpty(Config.GetParameter<string>("windowTitle")) ||
                                         !string.IsNullOrEmpty(Config.GetParameter<string>("WindowTitlePattern")) ||
                                         !string.IsNullOrEmpty(Config.GetParameter<string>("windowTitlePattern")) ||
                                         !string.IsNullOrEmpty(Config.GetParameter<string>("ProcessName")) ||
                                         !string.IsNullOrEmpty(Config.GetParameter<string>("processName"));

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
                    new FileCaptureSource(),

                _ => null
            };

            if (source == null)
            {
                context.Fail($"Unsupported capture source type: {sourceType}");
                return;
            }

            var acquired = await source.AcquireAsync(context, cancellationToken).ConfigureAwait(false);
            if (acquired != null)
            {
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

        private async Task PreparePreCaptureAsync(CaptureFlowContext context, CancellationToken ct)
        {
            // Dismiss lingering tray balloons
            bool isTerminalServer = !CoreConfig.DisableRDPOptimizing && (CoreConfig.OptimizeForRDP || SystemInformation.TerminalServerSession);
            if (!CoreConfig.HideTrayicon && !isTerminalServer)
            {
                var notifyIcon = SimpleServiceProvider.Current.GetInstance<NotifyIcon>(isOptional: true);
                if (notifyIcon != null)
                {
                    var uiContext = SimpleServiceProvider.Current.GetInstance<SynchronizationContext>(isOptional: true) ?? SynchronizationContext.Current;
                    if (uiContext != null && SynchronizationContext.Current != uiContext)
                    {
                        uiContext.Post(_ =>
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
                        }, null);
                    }
                    else
                    {
                        notifyIcon.Visible = false;
                        notifyIcon.Visible = true;
                    }
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
            }

            if (delay > 0)
            {
                context.LogStep($"Waiting pre-capture delay: {delay}ms");
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }
}
