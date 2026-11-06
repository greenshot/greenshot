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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using log4net;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Shows a file (selected in its folder) or a folder in the Windows Explorer. Without a Path it shows the last capture,
    /// or the folder of the screenshots when that file is gone. It only opens the Explorer, it never runs a file.
    /// </summary>
    [StepInfo(WellKnownStepTypes.ShowInExplorer, "Show in Explorer", "Shows a file or folder in the Windows Explorer, by default the last capture.", "Output")]
    [StepParameter(PathParameter, ContractDataType.FilePath, Description = "The file or folder to show (default: the last capture, else the folder of the screenshots)")]
    [StepOutputVariable(ShownPathVariable, ContractDataType.FilePath, "The file or folder which was shown", Conditional = true)]
    public class ShowInExplorerStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ShowInExplorerStep));

        public const string PathParameter = "Path";
        public const string ShownPathVariable = "ShownPath";

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public ShowInExplorerStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.ShowInExplorer;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) return;

            // The engine already evaluated the expressions in the parameter
            string configured = Config.GetParameter<string>(PathParameter);
            string path = string.IsNullOrWhiteSpace(configured) ? FindLastCaptureLocation() : FindExisting(configured.Trim());
            if (path == null)
            {
                context.Abort(string.IsNullOrWhiteSpace(configured)
                    ? "There is no last capture and no folder of the screenshots to show."
                    : $"'{configured}' doesn't exist, there is nothing to show.");
                return;
            }

            // The shell call needs the UI (STA) thread
            await context.Ui.InvokeAsync(() => ExplorerHelper.OpenInExplorer(path), cancellationToken).ConfigureAwait(false);
            context.Properties[ShownPathVariable] = path;
            context.LogStep($"[ShowInExplorer] {path}");
            Log.DebugFormat("Showed '{0}' in the Explorer.", path);
        }

        /// <summary>
        /// The file or folder, else the folder it is in; null when neither exists
        /// </summary>
        internal static string FindExisting(string path)
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath) || Directory.Exists(fullPath))
                {
                    return fullPath;
                }

                string directory = Path.GetDirectoryName(fullPath);
                return directory != null && Directory.Exists(directory) ? directory : null;
            }
            catch (Exception ex)
            {
                Log.Warn($"Can't use the path '{path}'", ex);
                return null;
            }
        }

        /// <summary>
        /// The last capture, else its folder (e.g. #1470: a drive which is gone), else the folder of the screenshots
        /// </summary>
        internal static string FindLastCaptureLocation()
        {
            var conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
            conf.ValidateAndCorrectOutputFileAsFullpath();
            string lastCapture = string.IsNullOrEmpty(conf.OutputFileAsFullpath) ? null : FindExisting(conf.OutputFileAsFullpath);
            if (lastCapture != null)
            {
                return lastCapture;
            }

            conf.ValidateAndCorrectOutputFilePath();
            try
            {
                string screenshots = FilenameHelper.FillVariables(conf.OutputFilePath, false);
                return Directory.Exists(screenshots) ? screenshots : null;
            }
            catch (Exception ex)
            {
                Log.Warn("Can't find the folder of the screenshots", ex);
                return null;
            }
        }
    }
}
