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

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;
using SkiaSharp;
using Capture = Greenshot.Linux.Core.Capturing.Capture;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.Source, "Acquire Source", "Acquires the image: captures the screen, a region, a window, the clipboard, a file or the current editor. A capture handed to the flow (forwarded from another recipe, imported from the browser extension) is used instead of capturing.", "Acquisition")]
    [StepPayload(RawCapture = PayloadRequirement.Created, Surface = PayloadRequirement.Created)]
    [StepParameter("SourceType", ContractDataType.Enum, DefaultValue = "Region", SupportsExpressions = false, Description = "What to capture", AllowedValues = new[] { "Region", "Window", "ActiveWindow", "FullScreen", "LastRegion", "Clipboard", "File", "TextOcr", "CurrentEditor", "Extension" })]
    [StepParameter("Filename", ContractDataType.FilePath, Description = "File to load (SourceType File); default: variable Filename")]
    [StepParameter("CaptureMouseCursor", ContractDataType.Boolean, Description = "Include the mouse cursor (default: settings)")]
    [StepParameter("DelayMs", ContractDataType.Integer, Description = "Delay before capturing in milliseconds (default: settings)")]
    [StepParameter("ScreenCaptureMode", ContractDataType.Enum, Description = "Which screen(s) to capture for FullScreen (default: settings)", AllowedValues = new[] { "Auto", "FullScreen", "Fixed" })]
    [StepParameter("WindowTitlePattern", ContractDataType.String, Description = "Capture the window whose title matches this regular expression (SourceType Window)")]
    [StepParameter("ProcessName", ContractDataType.String, Description = "Capture the window of this process (SourceType Window)")]
    [StepParameter("WindowHandle", ContractDataType.Window, Description = "Capture this window without asking or activating it (SourceType Window): the variable of a Window argument (e.g. ${Window}) or a window handle")]
    [StepParameter("MatchCase", ContractDataType.Boolean, Description = "Match WindowTitlePattern and ProcessName case-sensitively")]
    [StepParameter("AlignDpi", ContractDataType.Boolean, DefaultValue = true, Description = "Set the image resolution to the screen DPI")]
    [StepInputVariable("PreSuppliedRegion", ContractDataType.Object, Description = "Region to capture without asking (set by the caller)")]
    [StepInputVariable("CaptureDelay", ContractDataType.Integer, Description = "Overrides DelayMs")]
    [StepInputVariable("CaptureMouseCursor", ContractDataType.Boolean, Description = "Overrides the CaptureMouseCursor parameter")]
    [StepInputVariable("ScreenCaptureMode", ContractDataType.Enum, Description = "Overrides the ScreenCaptureMode parameter")]
    [StepInputVariable("TargetWindow", ContractDataType.Object, Description = "Window to capture (set by the caller)")]
    [StepInputVariable("WindowTitlePattern", ContractDataType.String, Description = "Window title pattern, when the parameter is not set")]
    [StepInputVariable("ProcessName", ContractDataType.String, Description = "Process name, when the parameter is not set")]
    [StepInputVariable("MatchCase", ContractDataType.Boolean, Description = "Match case, when the parameter is not set")]
    [StepInputVariable("Filename", ContractDataType.FilePath, Description = "File to load (SourceType File), when the parameter is not set")]
    [StepInputVariable("EditorForm", ContractDataType.Object, Description = "The editor to take the image from (SourceType CurrentEditor, set by the editor trigger)")]
    [StepOutputVariable("SelectedWindow", ContractDataType.Object, "The window of the last region (SourceType LastRegion)", Conditional = true)]
    public class SourceAcquisitionStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SourceAcquisitionStep));

        // The default delay of Windows lets a menu close before the capture; a hotkey opens no menu
        private const int DefaultCaptureDelay = 100;

        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public SourceAcquisitionStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "SourceAcquisitionStep";
        }

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            if (string.Equals(Config.GetParameter<string>("SourceType"), "File", StringComparison.OrdinalIgnoreCase))
            {
                string filename = Config.GetParameter<string>("Filename");
                yield return new RecipeGatedAction(RecipeGateType.FileSystemAccess,
                    string.IsNullOrWhiteSpace(filename) ? "Reads the image file named in the variable Filename" : $"Reads the image file {filename}");
            }
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            // A capture handed to the flow (forwarded from another recipe, the editor, the command line) is used instead of capturing
            if (context.Payload != null)
            {
                Log.Info($"Source {Name} using pre-supplied capture payload.");
                context.IsPayloadPreSupplied = true;
                return;
            }

            var host = RecipeServices.Host;
            if (host == null)
            {
                context.Fail("Capturing needs Greenshot, it isn't there (tests, command line).");
                return;
            }

            CaptureSourceType sourceType = Config.GetParameter("SourceType", CaptureSourceType.Region);
            if (sourceType == CaptureSourceType.Window && Config.GetParameter<object>("WindowHandle") is { } windowHandle)
            {
                // Wayland gives applications no handles of other windows
                context.Fail($"Capturing a given window ({windowHandle}) is not possible on Linux.");
                return;
            }

            bool fromScreen = sourceType is not (CaptureSourceType.Clipboard or CaptureSourceType.File or CaptureSourceType.CurrentEditor or CaptureSourceType.Extension);
            if (fromScreen)
            {
                await DelayAsync(context, cancellationToken).ConfigureAwait(false);
            }

            bool captureMouse = ResolveCaptureMouse(context);
            string titlePattern = Config.GetParameter<string>("WindowTitlePattern") ?? (context.Properties.TryGetValue("WindowTitlePattern", out var p) ? p as string : null);
            bool matchCase = Config.GetParameter("MatchCase", context.Properties.TryGetValue("MatchCase", out var m) && m is true);

            ICapturePayload payload;
            switch (sourceType)
            {
                case CaptureSourceType.Window when !string.IsNullOrEmpty(titlePattern):
                case CaptureSourceType.ActiveWindow:
                    payload = await host.CaptureWindowAsync(titlePattern, matchCase, captureMouse, cancellationToken).ConfigureAwait(false);
                    break;
                case CaptureSourceType.Region:
                case CaptureSourceType.Window:
                case CaptureSourceType.TextOcr:
                    payload = await host.CaptureScreenAsync(true, captureMouse, cancellationToken).ConfigureAwait(false);
                    break;
                case CaptureSourceType.FullScreen:
                    payload = await host.CaptureScreenAsync(false, captureMouse, cancellationToken).ConfigureAwait(false);
                    break;
                case CaptureSourceType.LastRegion:
                    payload = await host.CaptureLastRegionAsync(captureMouse, cancellationToken).ConfigureAwait(false);
                    break;
                case CaptureSourceType.Clipboard:
                    payload = await host.CaptureClipboardAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case CaptureSourceType.File:
                    string filename = Config.GetParameter<string>("Filename") ?? (context.Properties.TryGetValue("Filename", out var f) ? f as string : null);
                    if (string.IsNullOrWhiteSpace(filename))
                    {
                        context.Fail("SourceType File requires a file path: set the parameter Filename or the variable Filename.");
                        return;
                    }

                    payload = await host.OpenFileAsync(filename, cancellationToken).ConfigureAwait(false);
                    if (payload == null)
                    {
                        context.Fail($"The file '{filename}' can't be read.");
                        return;
                    }

                    payload.Metadata["Filename"] = filename;
                    // The parts of the file for the patterns of later steps: ${dirname}/${basename}.png, like on Windows
                    if (payload.Details is { } fileDetails)
                    {
                        fileDetails.Title = Path.GetFileNameWithoutExtension(filename);
                        fileDetails.MetaData["file"] = filename;
                        fileDetails.MetaData["source"] = "file";
                        fileDetails.MetaData["dirname"] = Path.GetDirectoryName(Path.GetFullPath(filename)) ?? string.Empty;
                        fileDetails.MetaData["filename"] = Path.GetFileNameWithoutExtension(filename);
                        fileDetails.MetaData["basename"] = Path.GetFileNameWithoutExtension(filename);
                        fileDetails.MetaData["extension"] = Path.GetExtension(filename);
                    }

                    break;
                case CaptureSourceType.CurrentEditor:
                    payload = FromEditor(context);
                    break;
                case CaptureSourceType.Extension:
                    context.Fail("Extension capture source requires a pre-supplied image payload.");
                    return;
                default:
                    context.Fail($"Unsupported capture source type: {sourceType}");
                    return;
            }

            if (payload?.RawCapture == null)
            {
                payload?.Dispose();
                if (!context.IsAborted)
                {
                    context.Abort("Acquisition failed or produced no payload.");
                }

                return;
            }

            // A region given by the caller (command line, AI tools): no selection
            if (fromScreen && context.Properties.TryGetValue("PreSuppliedRegion", out var regionObject) && regionObject is SKRectI region && !region.IsEmpty)
            {
                var image = payload.RawCapture.Image;
                var area = SKRectI.Intersect(region, SKRectI.Create(image.Width, image.Height));
                if (!area.IsEmpty)
                {
                    var cropped = new SKBitmap(area.Width, area.Height, image.ColorType, image.AlphaType);
                    image.ExtractSubset(cropped, area);
                    payload.RawCapture = new Capture(cropped.Copy(), payload.RawCapture.Details);
                    cropped.Dispose();
                }
            }

            payload.Metadata["source"] = fromScreen ? "Screen" : sourceType.ToString();
            context.Payload = payload;
        }

        private static ICapturePayload FromEditor(CaptureFlowContext context)
        {
            if (!context.Properties.TryGetValue("EditorForm", out var editorObject) || editorObject is not IImageEditor editor)
            {
                context.Fail("SourceType CurrentEditor needs the editor (it runs from the Recipes menu of the editor).");
                return null;
            }

            var surface = new EditorSurface(editor.Surface.Image.Copy()) { CounterStart = editor.Surface.CounterStart };
            surface.LoadElements(editor.Surface.SerializeElements());
            return new CapturePayload(new Capture(editor.Surface.Image.Copy(), editor.CaptureDetails)) { Surface = surface };
        }

        private bool ResolveCaptureMouse(CaptureFlowContext context)
        {
            if (context.Properties.TryGetValue("CaptureMouseCursor", out var contextValue) && contextValue is bool contextBool)
            {
                return contextBool;
            }

            if (Config.Parameters.TryGetValue("CaptureMouseCursor", out var stepValue) && stepValue != null)
            {
                if (stepValue is bool stepBool) return stepBool;
                if (bool.TryParse(stepValue.ToString(), out bool parsed)) return parsed;
            }

            return RecipeServices.Settings?.CaptureMousepointer ?? true;
        }

        private async Task DelayAsync(CaptureFlowContext context, CancellationToken cancellationToken)
        {
            int delay = -1;
            if (context.Properties.TryGetValue("CaptureDelay", out var contextDelay) && contextDelay is int cd)
            {
                delay = cd;
            }
            else if (Config.Parameters.TryGetValue("DelayMs", out var stepDelay) && stepDelay != null)
            {
                if (stepDelay is int sd) delay = sd;
                else if (int.TryParse(stepDelay.ToString(), out int parsed)) delay = parsed;
            }

            if (delay < 0)
            {
                delay = RecipeServices.Settings?.CaptureDelay ?? 0;
                if (delay == DefaultCaptureDelay && string.Equals(context.Trigger?.TriggerType, Triggers.TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
                {
                    delay = 0;
                }
            }

            if (delay > 0)
            {
                context.LogStep($"Waiting pre-capture delay: {delay}ms");
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
