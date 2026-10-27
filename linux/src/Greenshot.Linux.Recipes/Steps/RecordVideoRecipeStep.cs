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

using System.Diagnostics;
using Greenshot.Linux.Core;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.RecordVideo, "Record Video", "Records a video of a monitor, window or region (Windows Graphics Capture).", "Video")]
    [StepParameter("SourceType", ContractDataType.Enum, DefaultValue = "ActiveWindow", Description = "What to record", AllowedValues = new[] { "ActiveWindow", "Window", "Region", "Monitor" })]
    [StepParameter("MonitorIndex", ContractDataType.Integer, Description = "Monitor to record (Monitor)")]
    [StepParameter("WindowTitlePattern", ContractDataType.String, Description = "Window title regular expression (Window)")]
    [StepParameter("ProcessName", ContractDataType.String, Description = "Process of the window (Window)")]
    [StepParameter("MatchCase", ContractDataType.Boolean, Description = "Match the window title case-sensitively")]
    [StepParameter("RegionX", ContractDataType.Integer, Description = "Region (Region)")]
    [StepParameter("RegionY", ContractDataType.Integer, Description = "Region (Region)")]
    [StepParameter("RegionWidth", ContractDataType.Integer, Description = "Region (Region)")]
    [StepParameter("RegionHeight", ContractDataType.Integer, Description = "Region (Region)")]
    [StepParameter("DelayMs", ContractDataType.Integer, Description = "Delay before recording in milliseconds")]
    [StepParameter("DurationSeconds", ContractDataType.Integer, Description = "Length of the recording")]
    [StepParameter("UntilWindowCloses", ContractDataType.Boolean, Description = "Record until the window closes (Window)")]
    [StepParameter("Preset", ContractDataType.String, Description = "Quality preset")]
    [StepParameter("Format", ContractDataType.String, Description = "Video format")]
    [StepParameter("FrameRate", ContractDataType.Integer, Description = "Frames per second")]
    [StepParameter("Bitrate", ContractDataType.Integer, Description = "Bitrate")]
    [StepParameter("TargetWidth", ContractDataType.Integer, Description = "Output width")]
    [StepParameter("TargetHeight", ContractDataType.Integer, Description = "Output height")]
    [StepParameter("ScaleFactor", ContractDataType.Decimal, Description = "Output scale")]
    [StepParameter("ColorMode", ContractDataType.String, Description = "Color mode")]
    [StepParameter("CaptureMouseCursor", ContractDataType.Boolean, DefaultValue = true, Description = "Record the mouse cursor")]
    [StepParameter("ShowCaptureBorder", ContractDataType.Boolean, DefaultValue = false, Description = "Show the Windows capture border")]
    [StepParameter("WindowResizeBehavior", ContractDataType.String, Description = "What to do when the window is resized")]
    [StepParameter("AudioSource", ContractDataType.String, DefaultValue = "None", Description = "Audio to record")]
    [StepParameter("PreventSleepWhileRecording", ContractDataType.Boolean, DefaultValue = true, Description = "Keep the PC awake while recording")]
    [StepParameter("AutoPauseOnSessionLock", ContractDataType.Boolean, DefaultValue = true, Description = "Pause while the PC is locked")]
    [StepParameter("OutputDirectory", ContractDataType.DirectoryPath, Description = "Directory to write to")]
    [StepParameter("FilenamePattern", ContractDataType.String, Description = "File name in OutputDirectory, {yyyyMMdd_HHmmss} is the start time (default Greenshot_{yyyyMMdd_HHmmss}.mp4)")]
    [StepInputVariable("SourceType", ContractDataType.Enum, Description = "What to record, when the parameter is not set")]
    [StepInputVariable("WindowHandle", ContractDataType.Object, Description = "Window to record (set by the caller)")]
    [StepInputVariable("CaptureRegion", ContractDataType.Object, Description = "Region to record (set by the caller)")]
    [StepOutputVariable("VideoFilePath", ContractDataType.FilePath, "Path of the recorded video")]
    [StepOutputVariable("VideoRecordingResult", ContractDataType.Object, "Details of the recording")]
    public class RecordVideoRecipeStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecordVideoRecipeStep));

        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public RecordVideoRecipeStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "RecordVideoRecipeStep";
        }

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            yield return new RecipeGatedAction(RecipeGateType.FileSystemAccess, $"Writes a video to {Config.GetParameter<string>("OutputDirectory") ?? "the Videos folder"}");
        }

        /// <summary>
        /// Windows records with Windows Graphics Capture; on wlroots compositors (sway, Hyprland) wf-recorder records the
        /// screen or a region. A window is recorded as its area (Wayland has no window recording for other applications).
        /// </summary>
        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (!ProcessRunner.IsOnPath("wf-recorder"))
            {
                context.Fail("Recording a video needs wf-recorder (sway, Hyprland and other wlroots compositors).");
                return;
            }

            int duration = Config.GetParameter("DurationSeconds", 10);
            int delay = Config.GetParameter("DelayMs", 0);
            string directory = Config.GetParameter<string>("OutputDirectory");
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                if (string.IsNullOrEmpty(directory)) directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos");
            }

            var start = DateTime.Now;
            string pattern = Config.GetParameter<string>("FilenamePattern") ?? "Greenshot_{yyyyMMdd_HHmmss}.mp4";
            string filename = System.Text.RegularExpressions.Regex.Replace(pattern, @"\{([^}]+)\}", m => start.ToString(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, filename);

            var arguments = new List<string> { "-f", path };
            string sourceType = Config.GetParameter<string>("SourceType") ?? (context.Properties.TryGetValue("SourceType", out var s) ? s as string : null) ?? "ActiveWindow";
            if (string.Equals(sourceType, "Region", StringComparison.OrdinalIgnoreCase))
            {
                int x = Config.GetParameter("RegionX", 0), y = Config.GetParameter("RegionY", 0), w = Config.GetParameter("RegionWidth", 0), h = Config.GetParameter("RegionHeight", 0);
                if (w > 0 && h > 0) arguments.AddRange(["-g", $"{x},{y} {w}x{h}"]);
            }

            int frameRate = Config.GetParameter("FrameRate", 0);
            if (frameRate > 0) arguments.AddRange(["-r", frameRate.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            if (!string.Equals(Config.GetParameter<string>("AudioSource"), "None", StringComparison.OrdinalIgnoreCase) && Config.GetParameter<string>("AudioSource") != null)
            {
                arguments.Add("-a");
            }

            if (delay > 0)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            context.LogStep($"Recording {duration}s to {path}");
            var startInfo = new ProcessStartInfo("wf-recorder") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardInput = true };
            foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo)!;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, duration)), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // wf-recorder finishes the file on SIGINT
                if (!process.HasExited)
                {
                    using var kill = Process.Start("kill", ["-INT", process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
                    await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None).ConfigureAwait(false);
                }
            }

            if (!File.Exists(path))
            {
                context.Fail($"wf-recorder made no video: {await process.StandardError.ReadToEndAsync(CancellationToken.None).ConfigureAwait(false)}".Trim());
                return;
            }

            context.Properties["VideoFilePath"] = path;
            context.Properties["VideoRecordingResult"] = new Dictionary<string, object> { ["FilePath"] = path, ["Duration"] = duration };
            Log.InfoFormat("Recorded {0}", path);
        }
    }
}
