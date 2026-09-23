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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Video;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Video;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Experimental DAG recipe step that records screen video using Windows.Graphics.Capture.
    /// Can record full-screen, a window, or a fixed region, and outputs the resulting MP4 file
    /// into the pipeline context for downstream export or automation steps.
    /// </summary>
    public class RecordVideoRecipeStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecordVideoRecipeStep));

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public RecordVideoRecipeStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "RecordVideo";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var videoService = new WindowsGraphicsCaptureVideoService();
            if (!videoService.IsSupported)
            {
                Log.Warn("Windows Graphics Capture is not supported on this operating system.");
                context.State = CaptureFlowState.Failed;
                return;
            }

            // Optional pre-capture delay
            int delayMs = Config.GetParameter("DelayMs", 0);
            if (delayMs <= 0) delayMs = Config.GetParameter("delayMs", 0);
            if (delayMs <= 0)
            {
                int delaySec = Config.GetParameter("DelaySeconds", 0);
                if (delaySec <= 0) delaySec = Config.GetParameter("delaySeconds", 0);
                if (delaySec > 0) delayMs = delaySec * 1000;
            }

            if (delayMs > 0)
            {
                Log.Info($"Delaying {delayMs}ms before video recording...");
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    context.State = CaptureFlowState.Cancelled;
                    return;
                }
            }

            var options = BuildOptions(context);

            Log.Info($"Executing RecordVideoRecipeStep: Target={options.Target}, Format={options.Format}, FPS={options.FrameRate}, Bitrate={options.Bitrate}");

            using var session = await videoService.StartRecordingAsync(options, cancellationToken);

            // Read duration or wait until window closes
            int durationSeconds = Config.GetParameter("DurationSeconds", 0);
            if (durationSeconds <= 0) durationSeconds = Config.GetParameter("durationSeconds", 0);

            bool untilWindowCloses = Config.GetParameter("UntilWindowCloses", 
                Config.GetParameter("untilWindowCloses", options.Target.TargetType == VideoCaptureTargetType.Window && durationSeconds <= 0));

            if (durationSeconds > 0)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await session.CancelAsync();
                    context.State = CaptureFlowState.Cancelled;
                    return;
                }
            }
            else if (untilWindowCloses && options.Target.TargetType == VideoCaptureTargetType.Window)
            {
                IntPtr hWnd = options.Target.WindowHandle;
                Log.Info($"Recording window 0x{hWnd:X8} until it closes...");
                try
                {
                    while (session.State == RecordingState.Recording || session.State == RecordingState.Paused)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            await session.CancelAsync();
                            context.State = CaptureFlowState.Cancelled;
                            return;
                        }

                        if (hWnd != IntPtr.Zero && !IsWindow(hWnd))
                        {
                            Log.Info($"Target window (0x{hWnd:X8}) has closed. Finalizing recording.");
                            break;
                        }

                        await Task.Delay(250, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    await session.CancelAsync();
                    context.State = CaptureFlowState.Cancelled;
                    return;
                }
            }

            var result = await session.StopAsync();

            context.Properties["VideoRecordingResult"] = result;
            context.Properties["VideoFilePath"] = result.FilePath;

            if (context.Payload == null)
            {
                context.Payload = new CapturePayload();
            }
            context.Payload.Metadata["VideoRecordingResult"] = result;
            context.Payload.Metadata["VideoFilePath"] = result.FilePath;

            Log.Info($"Video recording completed successfully: {result.FilePath} ({result.FileSizeBytes} bytes)");
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        private VideoCaptureOptions BuildOptions(CaptureFlowContext context)
        {
            var options = new VideoCaptureOptions();

            // 1. Resolve Target / Source Type
            string sourceType = Config.GetParameter<string>("SourceType", null)
                ?? Config.GetParameter<string>("sourceType", null)
                ?? Config.GetParameter<string>("Target", null)
                ?? Config.GetParameter<string>("target", null)
                ?? Config.GetParameter<string>("TargetType", null)
                ?? Config.GetParameter<string>("targetType", null)
                ?? (context.Properties.TryGetValue("SourceType", out var stObj) ? stObj?.ToString() : null)
                ?? "ActiveWindow";

            if (string.Equals(sourceType, "Window", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sourceType, "ActiveWindow", StringComparison.OrdinalIgnoreCase))
            {
                IntPtr hWnd = IntPtr.Zero;

                // Pre-supplied window handle from context or previous node
                if (context.Properties.TryGetValue("WindowHandle", out var wndObj) && wndObj is IntPtr h && h != IntPtr.Zero)
                {
                    hWnd = h;
                }
                else
                {
                    string title = Config.GetParameter<string>("WindowTitle", null) ?? Config.GetParameter<string>("windowTitle", null);
                    string titlePattern = Config.GetParameter<string>("WindowTitlePattern", null) ?? Config.GetParameter<string>("windowTitlePattern", null);
                    string processName = Config.GetParameter<string>("ProcessName", null) ?? Config.GetParameter<string>("processName", null);
                    bool matchCase = Config.GetParameter("MatchCase", false) || Config.GetParameter("matchCase", false);

                    if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(titlePattern) || !string.IsNullOrEmpty(processName))
                    {
                        var win = FindMatchingWindow(title, titlePattern, processName, matchCase);
                        if (win != null)
                        {
                            hWnd = win.Handle;
                            Log.Info($"Resolved targeted window: '{win.Text}' (0x{hWnd:X8})");
                        }
                        else
                        {
                            Log.Warn($"No window found matching title='{title}', pattern='{titlePattern}', process='{processName}'. Falling back to foreground window.");
                        }
                    }

                    if (hWnd == IntPtr.Zero)
                    {
                        hWnd = User32Api.GetForegroundWindow();
                    }
                }

                options.Target = VideoCaptureTarget.FromWindow(hWnd);
            }
            else if (string.Equals(sourceType, "Region", StringComparison.OrdinalIgnoreCase))
            {
                NativeRect region = NativeRect.Empty;
                if (context.Properties.TryGetValue("CaptureRegion", out var regObj) && regObj is NativeRect r && !r.IsEmpty)
                {
                    region = r;
                }
                else
                {
                    int x = Config.GetParameter("RegionX", Config.GetParameter("regionX", 0));
                    int y = Config.GetParameter("RegionY", Config.GetParameter("regionY", 0));
                    int w = Config.GetParameter("RegionWidth", Config.GetParameter("regionWidth", 800));
                    int h = Config.GetParameter("RegionHeight", Config.GetParameter("regionHeight", 600));
                    region = new NativeRect(x, y, w, h);
                }
                options.Target = VideoCaptureTarget.FromRegion(region);
            }
            else
            {
                // Fullscreen / Screen / Monitor
                int monitorIndex = Config.GetParameter("MonitorIndex", Config.GetParameter("monitorIndex", -1));
                var allMonitors = DisplayInfo.AllDisplayInfos;
                DisplayInfo monitor = null;

                if (monitorIndex >= 0 && monitorIndex < allMonitors.Length)
                {
                    monitor = allMonitors[monitorIndex];
                }
                else
                {
                    monitor = allMonitors.FirstOrDefault(d => d.IsPrimary) ?? allMonitors.First();
                }

                options.Target = VideoCaptureTarget.FromMonitor(monitor.MonitorHandle);
            }

            // 2. Preset (applied first so individual settings can override)
            string presetStr = Config.GetParameter<string>("Preset", null) ?? Config.GetParameter<string>("preset", null);
            if (!string.IsNullOrWhiteSpace(presetStr) && Enum.TryParse<VideoEncodingPreset>(presetStr, true, out var preset))
            {
                options.ApplyPreset(preset);
            }

            // 3. Format / Codec
            string formatStr = Config.GetParameter<string>("Format", null) ?? Config.GetParameter<string>("format", null);
            if (!string.IsNullOrWhiteSpace(formatStr) && Enum.TryParse<VideoFormat>(formatStr, true, out var format))
            {
                options.Format = format;
            }

            // 4. Frame Rate
            int fps = Config.GetParameter("FrameRate", 0);
            if (fps <= 0) fps = Config.GetParameter("frameRate", 0);
            if (fps <= 0) fps = Config.GetParameter("fps", 0);
            if (fps > 0) options.FrameRate = fps;

            // 5. Bitrate
            int bitrate = Config.GetParameter("Bitrate", 0);
            if (bitrate <= 0) bitrate = Config.GetParameter("bitrate", 0);
            if (bitrate > 0) options.Bitrate = bitrate;

            // 6. Target Size & Scaling
            int targetW = Config.GetParameter("TargetWidth", 0);
            if (targetW <= 0) targetW = Config.GetParameter("targetWidth", 0);
            int targetH = Config.GetParameter("TargetHeight", 0);
            if (targetH <= 0) targetH = Config.GetParameter("targetHeight", 0);
            if (targetW > 0 && targetH > 0)
            {
                options.TargetSize = new Size(targetW, targetH);
            }

            double scale = Config.GetParameter("ScaleFactor", 0.0);
            if (scale <= 0.0) scale = Config.GetParameter("scaleFactor", 0.0);
            if (scale > 0.0 && scale <= 2.0)
            {
                options.ScaleFactor = scale;
            }

            // 7. Color Mode
            string colorModeStr = Config.GetParameter<string>("ColorMode", null) ?? Config.GetParameter<string>("colorMode", null);
            if (!string.IsNullOrWhiteSpace(colorModeStr) && Enum.TryParse<VideoColorMode>(colorModeStr, true, out var colorMode))
            {
                options.ColorMode = colorMode;
            }

            // 8. Appearance: Cursor, Capture Border & Window Resize
            options.CaptureCursor = Config.GetParameter("CaptureMouseCursor", 
                Config.GetParameter("captureMouseCursor", 
                Config.GetParameter("CaptureCursor", 
                Config.GetParameter("captureCursor", true))));

            options.ShowCaptureBorder = Config.GetParameter("ShowCaptureBorder", 
                Config.GetParameter("showCaptureBorder", false));

            string resizeBehaviorStr = Config.GetParameter<string>("WindowResizeBehavior", null) 
                ?? Config.GetParameter<string>("windowResizeBehavior", null);
            if (!string.IsNullOrWhiteSpace(resizeBehaviorStr) && Enum.TryParse<WindowResizeBehavior>(resizeBehaviorStr, true, out var resizeBehavior))
            {
                options.WindowResizeBehavior = resizeBehavior;
            }

            // 9. Audio
            string audioSourceStr = Config.GetParameter<string>("AudioSource", null) 
                ?? Config.GetParameter<string>("audioSource", "None");
            if (Enum.TryParse<AudioCaptureSource>(audioSourceStr, true, out var audioSource))
            {
                options.AudioSource = audioSource;
            }

            // 10. Power & System Sleep
            options.PreventSleepWhileRecording = Config.GetParameter("PreventSleepWhileRecording", 
                Config.GetParameter("preventSleepWhileRecording", true));

            options.AutoPauseOnSessionLock = Config.GetParameter("AutoPauseOnSessionLock", 
                Config.GetParameter("autoPauseOnSessionLock", true));

            // 11. Output file path & directory
            string outputPath = Config.GetParameter<string>("OutputFilePath", null) 
                ?? Config.GetParameter<string>("outputFilePath", null);

            string outputDir = Config.GetParameter<string>("OutputDirectory", null) 
                ?? Config.GetParameter<string>("outputDirectory", null);

            string filenamePattern = Config.GetParameter<string>("FilenamePattern", null) 
                ?? Config.GetParameter<string>("filenamePattern", null);

            if (string.IsNullOrWhiteSpace(outputPath) && !string.IsNullOrWhiteSpace(outputDir))
            {
                string fn = !string.IsNullOrWhiteSpace(filenamePattern) ? filenamePattern : "Greenshot_{yyyyMMdd_HHmmss}.mp4";
                outputPath = Path.Combine(outputDir, fn);
            }

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Environment.ExpandEnvironmentVariables(outputPath);
                outputPath = outputPath.Replace("{yyyyMMdd_HHmmss}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                options.OutputFilePath = outputPath;
            }

            return options;
        }

        private static WindowDetails FindMatchingWindow(string title, string titlePattern, string processName, bool matchCase)
        {
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            Regex regex = null;
            if (!string.IsNullOrEmpty(titlePattern))
            {
                var regexOptions = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                regex = new Regex(titlePattern, regexOptions);
            }

            // 1. Check current active window first
            var activeWin = WindowDetails.GetActiveWindow();
            if (activeWin != null && Matches(activeWin, title, regex, processName, comparison))
            {
                return activeWin;
            }

            // 2. Search top-level application windows
            foreach (var win in WindowDetails.GetTopLevelWindows())
            {
                if (win == null || win.Handle == IntPtr.Zero || win.HasParent) continue;

                if (Matches(win, title, regex, processName, comparison))
                {
                    return win;
                }
            }

            return null;
        }

        private static bool Matches(WindowDetails win, string title, Regex regex, string processName, StringComparison comparison)
        {
            if (win == null || win.Handle == IntPtr.Zero) return false;

            if (!string.IsNullOrEmpty(processName))
            {
                string proc = null;
                try
                {
                    if (!string.IsNullOrEmpty(win.ProcessPath))
                    {
                        proc = Path.GetFileNameWithoutExtension(win.ProcessPath);
                    }
                }
                catch { }

                string expectedProc = Path.GetFileNameWithoutExtension(processName);
                if (proc == null || (!proc.Equals(expectedProc, comparison) && !proc.Equals(processName, comparison)))
                {
                    return false;
                }
            }

            if (regex != null)
            {
                if (string.IsNullOrEmpty(win.Text) || !regex.IsMatch(win.Text))
                {
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(title))
            {
                if (string.IsNullOrEmpty(win.Text) || win.Text.IndexOf(title, comparison) < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
